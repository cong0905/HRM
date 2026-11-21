# Sổ Tay Vận Hành Chuyển Đổi Dữ Liệu (Cutover, Rollback & Recovery Runbook) — T23

## 1. Tổng Quan Quy Trình

Tài liệu này hướng dẫn chi tiết quy trình diễn tập và chuyển đổi dữ liệu (Cutover) từ hệ thống cơ sở dữ liệu HRM legacy sang cơ sở dữ liệu HRM.Next thế hệ mới, bao gồm các kịch bản kiểm tra an toàn, đối soát số liệu và quy trình rollback/forward recovery khi xảy ra sự cố.

```
+-----------------------------------------------------------------------------------+
|                              QUY TRÌNH CUTOVER T23                                |
+-----------------------------------------------------------------------------------+
|  [Pha 1: Chuẩn bị]  --> Backup Full DB nguồn & DB đích                            |
|                     --> Khóa ghi DB nguồn (Read-Only maintenance mode)            |
+-----------------------------------------------------------------------------------+
|  [Pha 2: Dry-Run]   --> Chạy validation phát hiện Trùng lặp / Orphan / Lỗi nghiệp vụ|
|                     --> NẾU CÓ LỖI (Severity = Error) --> DỪNG NGAY LẬP TỨC        |
+-----------------------------------------------------------------------------------+
|  [Pha 3: Thực thi]  --> Chạy MigrationEngine theo Transaction / Staged Checkpoints |
|                     --> Sinh Deterministic UUIDv5 (Đảm bảo Idempotency)           |
|                     --> Không chuyển hash mật khẩu, tài khoản IsActive = FALSE    |
+-----------------------------------------------------------------------------------+
|  [Pha 4: Đối soát]  --> Reconciler so sánh Row Counts & Tổng thực nhận kỳ lương   |
|                     --> Dung sai (Tolerance) = 0.00 VND                           |
|                     --> NẾU KHỚP 100% --> Hoàn tất Cutover                        |
|                     --> NẾU LỆCH --> Kích hoạt ROLLBACK                           |
+-----------------------------------------------------------------------------------+
|  [Pha 5: Kích hoạt] --> Gửi Activation Outbox Messages để nhân viên đặt mật khẩu  |
+-----------------------------------------------------------------------------------+
```

---

## 2. Pha 1: Chuẩn Bị & Khóa Đóng Băng Dữ Liệu (Pre-Cutover Freeze)

1. **Thông báo bảo trì**: Thiết lập thông báo hệ thống và ngắt quyền ghi trên ứng dụng legacy.
2. **Khóa DB Legacy sang chế độ Read-Only**:
   ```sql
   ALTER DATABASE [HRM_System] SET READ_ONLY WITH ROLLBACK IMMEDIATE;
   ```
3. **Sao lưu dữ liệu nguồn (Source Backup)**:
   ```sql
   BACKUP DATABASE [HRM_System]
   TO DISK = 'C:\Backups\HRM_System_PreCutover_20261119.bak'
   WITH FORMAT, COMPRESSION, CHECKSUM;
   ```
4. **Sao lưu dữ liệu đích (Target Snapshot / Backup)**:
   ```sql
   BACKUP DATABASE [HRM_Next_Production]
   TO DISK = 'C:\Backups\HRM_Next_PreCutover_20261119.bak'
   WITH FORMAT, COMPRESSION, CHECKSUM;
   ```

---

## 3. Pha 2: Diễn Tập Kiểm Tra Tính Toàn Vẹn (Dry-Run Gate)

Trước khi thực hiện bất kỳ thao tác ghi nào, bắt buộc phải chạy lệnh `dry-run` để quét toàn bộ dữ liệu nguồn:

```powershell
python ops/migration/migrate.py dry-run `
    --legacy "$env:HRM_LEGACY_SQLSERVER" `
    --target "$env:HRM_T05_SQLSERVER" `
    --report "docs/migration/dryrun-report.json"
```

### Các ràng buộc bắt buộc kiểm tra (Validation Gates):
1. **Trùng lặp (Duplicates)**:
   - `DUPLICATE_USERNAME`: Tên đăng nhập không phân biệt hoa thường và khoảng trắng thừa.
   - `DUPLICATE_CCCD`: Số CCCD không được trùng lặp giữa các nhân viên.
   - `DUPLICATE_EMAIL`: Email nhân viên phải duy nhất trong toàn hệ thống.
2. **Mồ côi khóa ngoại (Orphans)**:
   - `ORPHAN_DEPARTMENT`: Nhân viên tham chiếu `MaPhongBan` không tồn tại.
   - `ORPHAN_POSITION`: Nhân viên tham chiếu `MaChucVu` không tồn tại.
   - `ORPHAN_EMPLOYEE_ACCOUNT`: Tài khoản tham chiếu `MaNhanVien` không tồn tại.
   - `ORPHAN_ATTENDANCE`: Chấm công tham chiếu `MaNhanVien` không hợp lệ.
   - `ORPHAN_LEAVE`: Đơn nghỉ phép tham chiếu `MaNhanVien` không hợp lệ.
   - `ORPHAN_PAYROLL`: Bản ghi lương tham chiếu `MaNhanVien` không hợp lệ.
3. **Quy tắc nghiệp vụ bất biến (Domain Invariants)**:
   - `INVALID_SALARY`: Mức lương <= 0.
   - `INVALID_LEAVE_DATES`: Ngày kết thúc nhỏ hơn ngày bắt đầu.
   - `CROSS_YEAR_LEAVE`: Đơn nghỉ phép kéo dài qua 2 năm dương lịch khác nhau.
   - `LEAVE_OVERDRAFT`: Ngày phép đã dùng vượt quá tổng ngày phép được cấp.

> [!CAUTION]
> Nếu kết quả dry-run trả về mã lỗi (`exit code != 0`), **tuyệt đối không chạy lệnh migrate**. Đội kỹ thuật phải báo cáo danh sách ID vi phạm cho chủ dự án xử lý dữ liệu trước.

---

## 4. Pha 3: Thực Thi Di Chuyển Dữ Liệu (Execution Phase)

Chạy công cụ chuyển đổi chính thức:

```powershell
python ops/migration/migrate.py migrate `
    --legacy "$env:HRM_LEGACY_SQLSERVER" `
    --target "$env:HRM_T05_SQLSERVER"
```

### Các bảo đảm kỹ thuật trong quá trình chạy:
- **Tất định (Deterministic Keys)**: Mọi thực thể sinh khóa chính bằng UUIDv5 dựa trên mã nguồn legacy.
- **An toàn tài khoản**:
  - `PasswordHash = NULL`.
  - `IsActive = FALSE`.
  - Không chuyển hash BCrypt cũ.
  - Không đặt mật khẩu mặc định.
- **Bất biến lịch sử lương**:
  - Kỳ lương chuyển sang trạng thái `Locked`.
  - Sinh `PayrollCalculationSnapshot` ghi nhận chính xác `LuongThucNhan` cũ mà không tính toán lại.

---

## 5. Pha 4: Đối Soát Tự Động Sau Chuyển Đổi (Reconciliation Gate)

Ngay sau khi lệnh migrate hoàn tất, chạy công cụ đối soát tự động:

```powershell
python ops/migration/migrate.py reconcile `
    --legacy "$env:HRM_LEGACY_SQLSERVER" `
    --target "$env:HRM_T05_SQLSERVER" `
    --report "docs/migration/reconciliation-report.md"
```

### Tiêu chuẩn nghiệm thu (Acceptance Criteria):
1. **Row Count**: Khớp 100% số lượng bản ghi giữa DB nguồn và DB đích (trừ đi các bản ghi đã được duyệt loại bỏ trong văn bản).
2. **Tổng tài chính kỳ lương (Payroll Sums)**:
   - Tổng thực nhận `SUM(LuongThucNhan)` legacy và `SUM(Amount)` trên `Payslips` của HRM.Next phải khớp với **dung sai = 0.00 VND**.
   - Mọi phiếu lương cá nhân phải khớp từng đồng.
3. **Quỹ phép (Leave Balances)**:
   - Tổng ngày cấp (`AllocatedDays`) và tổng ngày đã dùng (`UsedDays`) phải khớp tuyệt đối.

---

## 6. Kịch Bản Rollback (Phục Hồi Dữ Liệu Khi Thất Bại)

Nếu đối soát phát hiện chênh lệch hoặc quá trình chuyển đổi phát sinh lỗi không thể khắc phục:

1. **Dừng toàn bộ tiến trình ghi vào HRM.Next**.
2. **Khôi phục DB đích về bản snapshot pre-cutover**:
   ```sql
   USE master;
   ALTER DATABASE [HRM_Next_Production] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
   RESTORE DATABASE [HRM_Next_Production]
   FROM DISK = 'C:\Backups\HRM_Next_PreCutover_20261119.bak'
   WITH REPLACE;
   ALTER DATABASE [HRM_Next_Production] SET MULTI_USER;
   ```
3. **Mở lại chế độ đọc/ghi cho hệ thống legacy**:
   ```sql
   ALTER DATABASE [HRM_System] SET READ_WRITE;
   ```
4. **Ghi nhận sự cố** vào biên bản thất bại cutover và thông báo cho các bên liên quan.

---

## 7. Kịch Bản Phục Hồi Tiến (Forward Recovery & Idempotent Re-Run)

Nếu quá trình chuyển đổi bị ngắt quãng giữa chừng do mất kết nối mạng hoặc server khởi động lại:
1. **Không cần rollback toàn bộ**: Do công cụ di chuyển sử dụng UUIDv5 tất định và cơ chế kiểm tra sự tồn tại của bản ghi trước khi chèn, lệnh `migrate` có tính chất **Idempotent 100%**.
2. **Chạy lại lệnh migrate**:
   ```powershell
   python ops/migration/migrate.py migrate --legacy "..." --target "..."
   ```
   - Các bản ghi đã import thành công ở lần chạy trước sẽ được bỏ qua an toàn.
   - Các bản ghi còn thiếu sẽ tiếp tục được chuyển đổi đầy đủ mà không sinh bản ghi trùng lặp.
3. **Chạy đối soát `reconcile`** để khẳng định dữ liệu đã hoàn tất đầy đủ.

---

## 8. Pha 5: Kích Hoạt Tài Khoản Sau Cutover (Post-Cutover Activation)

1. Kiểm tra hàng đợi outbox kích hoạt:
   ```sql
   SELECT COUNT(*) AS PendingActivations 
   FROM [personnel].[ActivationOutboxMessages] 
   WHERE [Status] = 'Pending';
   ```
2. Khởi chạy worker gửi email kích hoạt (Task T22).
3. Nhân viên nhận email kèm liên kết an toàn có chứa token kích hoạt dùng 1 lần, mở trang đặt mật khẩu mới lần đầu và kích hoạt tài khoản (`IsActive = TRUE`).
