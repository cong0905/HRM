# Biên bản Nghiệm thu Nhiệm vụ T02 (T02 Sign-off Record)

> **Dự án:** Hệ thống Quản lý Nhân sự (HRM System) — Đồ án Tốt nghiệp 8 tuần  
> **Nhiệm vụ:** T02 — Sửa đường dựng schema và ràng buộc dữ liệu legacy  
> **Ngày phê duyệt:** 10/11/2026  
> **Người phê duyệt:** Chủ dự án (Sinh viên thực hiện đồ án)  
> **Đơn vị hỗ trợ:** AI Coding Assistant (Antigravity)  
> **Trạng thái nhiệm vụ:** `VERIFIED` (Trong phạm vi đã kiểm chứng)  
> **Baseline Commit:** `05254b306f1f882b118575f50c5c81b486f40c6c` (Tag: `baseline-05254b3`)  
> **Nhánh làm việc:** `task/T02`

---

## 1. Tuyên bố Nghiệm thu & Phạm vi Được Công Nhận

Chủ dự án chính thức nghiệm thu hoàn thành nhiệm vụ **T02** trong **phạm vi kỹ thuật đã được kiểm chứng đầy đủ bằng thực nghiệm**:

1. **Fresh install từ EF Core Migrations**: Tạo mới database hoàn toàn từ chuỗi migration EF Core hiện tại (bao gồm migration mới `20261110141626_FixLegacySchemaAndConstraints.cs`).
2. **Nâng cấp database có schema baseline EF với fixture mô phỏng history**: Nâng cấp database có cấu trúc tương thích baseline EF và lịch sử migration tương đương khảo sát thực tế, có kiểm tra chặn an toàn trước mọi can thiệp DDL.

### Hồ Sơ & Bằng Chứng Kèm Theo:

- **Báo cáo bằng chứng thực thi**: [docs/verification/t02-execution-evidence.md](file:///d:/dotnet%20project/docs/verification/t02-execution-evidence.md)
- **Log máy đọc chi tiết (Lượt chạy v7)**: [docs/verification/runs/t02_verification_log_HRM_Next_Test_T02_v7.json](file:///d:/dotnet%20project/docs/verification/runs/t02_verification_log_HRM_Next_Test_T02_v7.json)
- **Báo cáo khảo sát schema & metadata**: [docs/verification/T02-schema-audit.md](file:///d:/dotnet%20project/docs/verification/T02-schema-audit.md)
- **Class Migration chính**: [20261110141626_FixLegacySchemaAndConstraints.cs](file:///d:/dotnet%20project/db/src/HRM.DAL/Migrations/20261110141626_FixLegacySchemaAndConstraints.cs)
- **Script kiểm thử & đối soát tự động**: [scripts/verify_t02_execution.py](file:///d:/dotnet%20project/scripts/verify_t02_execution.py)
- **Bộ kiểm thử đơn vị công cụ**: [scripts/tests/test_verify_t02_execution.py](file:///d:/dotnet%20project/scripts/tests/test_verify_t02_execution.py)

---

## 2. Bằng Chứng Kiểm Chứng Kỹ Thuật Chi Tiết

### A. Kiểm Thử Đơn Vị (Unit Test)

- **33/33 Unit tests PASS** (100% Xanh):
  - **18 tests** kiểm thử bộ điều phối runner, TRX parser và validator manifest (`scripts/tests/test_run_and_verify_baseline.py` và `test_verify_baseline_trx.py`).
  - **15 tests** kiểm thử công cụ kiểm chứng T02 (`scripts/tests/test_verify_t02_execution.py`), bao gồm:
    - Chứng minh phát hiện index xuất hiện hoặc biến mất ngoài ý muốn khi rollback thất bại.
    - Chứng minh phát hiện sai khác trong định nghĩa Foreign Key.
    - Chứng minh đối soát dữ liệu giữ trường `NULL` qua `INCLUDE_NULL_VALUES`, so sánh tiền tệ chính xác qua `Decimal`.
    - Chứng minh dynamic markdown report không chứa bất kỳ chuỗi mô tả thành công sai nào (zero false positive) khi có kịch bản thất bại.
    - Chứng minh hiển thị `CHƯA KIỂM CHỨNG` khi thiếu dữ liệu đo đạc thực tế.

### B. Kết Quả Thực Thi SQL Server Thực Tế (Theo Log v7: `HRM_Next_Test_T02_v7_*`)

Toàn bộ kịch bản kiểm thử được chạy độc lập trên cụm database test mới `HRM_Next_Test_T02_v7_*` với kết quả ghi nhận từ `t02_verification_log_HRM_Next_Test_T02_v7.json`:

| Kịch Bản                   | Database Đích                              | Exit Code | Lỗi SQL Mong Đợi / Thực Tế                                   | Kiểm Tra Schema & History Sau Chạy                                                            | Kết Quả  |
| :------------------------- | :----------------------------------------- | :-------: | :----------------------------------------------------------- | :-------------------------------------------------------------------------------------------- | :------: |
| **1. Fresh Install**       | `HRM_Next_Test_T02_v7_Fresh`               |    `0`    | Không phát hiện lỗi                                          | Unique Index: Có; FK MaTaiKhoan: Có; Drop shadow: Đã drop; Cột Hwid: Có; History: 7 migration | **PASS** |
| **2. Upgrade Valid**       | `HRM_Next_Test_T02_v7_UpgradeValid`        |    `0`    | Không phát hiện lỗi                                          | Áp dụng migration: Exit 0; Đối soát dữ liệu: 0 diffs; History: 8 migration                    | **PASS** |
| **- Idempotency**          | `HRM_Next_Test_T02_v7_UpgradeValid`        |    `0`    | 'No migrations were applied'                                 | Chạy lần 2: 0 lệch dữ liệu; Schema checks: Khớp; Schema metadata: Khớp 100%                   | **PASS** |
| **3. Block ShadowOnly**    | `HRM_Next_Test_T02_v7_Block_ShadowOnly`    |    `1`    | Mong đợi: 51001<br>Thực tế: 51001 (Báo Token ID: 20)         | Bảo toàn sau thất bại: Đạt; Dữ liệu: 0 lệch; Schema metadata: Khớp; History: Khớp             | **PASS** |
| **4. Block DifferingIds**  | `HRM_Next_Test_T02_v7_Block_DifferingIds`  |    `1`    | Mong đợi: 51002<br>Thực tế: 51002 (Báo Token ID: 30)         | Bảo toàn sau thất bại: Đạt; Dữ liệu: 0 lệch; Schema metadata: Khớp; History: Khớp             | **PASS** |
| **5. Block OrphanToken**   | `HRM_Next_Test_T02_v7_Block_OrphanToken`   |    `1`    | Mong đợi: 51003<br>Thực tế: 51003 (Báo Token ID: 40)         | Bảo toàn sau thất bại: Đạt; Dữ liệu: 0 lệch; Schema metadata: Khớp; History: Khớp             | **PASS** |
| **6. Block DuplicateUser** | `HRM_Next_Test_T02_v7_Block_DuplicateUser` |    `1`    | Mong đợi: 51004<br>Thực tế: 51004 (Báo User: duplicate_user) | Bảo toàn sau thất bại: Đạt; Dữ liệu: 0 lệch; Schema metadata: Khớp; History: Khớp             | **PASS** |

### C. Đối Soát Dữ Liệu Tự Động Từng Bản Ghi Theo Khóa Chính (Upgrade Valid)

Đối soát chi tiết từng trường dữ liệu giữa trước và sau migration trên database `HRM_Next_Test_T02_v7_UpgradeValid`:

- **`dbo.PhongBan`** (PK `MaPhongBan`): **6 bản ghi, 0 lệch** (Bao gồm các trường `NgayTao` và `NgayCapNhat` đã được bảo toàn nguyên trạng, loại bỏ triệt để lỗi UpdateData ngoài phạm vi).
- **`dbo.NhanVien`** (PK `MaNhanVien`): **10 bản ghi, 0 lệch** (Kiểm tra đầy đủ 14 cột nghiệp vụ, bao gồm `NgayTao`, `NgayCapNhat`).
- **`dbo.TaiKhoan`** (PK `MaTaiKhoan`): **7 bản ghi, 0 lệch** (Cột `LanDangNhapCuoi` có giá trị `NULL` được đối chiếu chính xác; mật khẩu `MatKhauHash` được bảo vệ không ghi log).
- **`dbo.BangLuong`** (PK `MaBangLuong`): **10 bản ghi, 0 lệch** (Đối soát 19 cột nghiệp vụ lương chính xác theo kiểu `Decimal`).
- **`dbo.ChamCong`** (PK `MaChamCong`): **8 bản ghi, 0 lệch** (Bao gồm cột `Hwid` được giữ nguyên vẹn).
- **`dbo.PasswordResetTokens`** (PK `Id`): **1 bản ghi, 0 lệch** (Kiểm tra liên kết `MaTaiKhoan` chính xác; chuỗi bí mật `Token` được bảo vệ không ghi log).
- **Tổng cộng**: **42 bản ghi được kiểm tra trên toàn bộ 6 bảng, ghi nhận đúng 0 sai lệch** trong các cột được đối soát.

---

## 3. Ranh Giới Kỹ Thuật Chưa Kiểm Chứng (UNVERIFIED) & Đầu Việc Riêng

> [!IMPORTANT]
> **Đường chuyển đổi từ SQL script legacy thuần túy (`db/docs/HRM_Database.sql`) tiếp tục ở trạng thái UNVERIFIED.**

1. **Nguyên nhân kỹ thuật**:
   - File script SQL legacy `db/docs/HRM_Database.sql` được tạo độc lập từ thời điểm ban đầu, hoàn toàn thiếu các bảng và cột phát sinh sau này trong chuỗi EF Core (`PasswordResetTokens`, cột `Hwid` trên `ChamCong`, sai lệch kiểu dữ liệu `datetime` vs `datetime2`).
   - Do đó, không thể chạy trực tiếp chuỗi EF migration hiện tại lên một database khởi tạo thuần túy từ file `HRM_Database.sql` mà không có bước bù đắp schema trung gian.
2. **Quy định bàn giao**:
   - Fixture `fixture_simulated_legacy.sql` được dùng trong test chỉ là **dữ liệu mô phỏng lịch sử EF**, tuyệt đối **không được coi là kiểm thử thay thế** cho đường nâng cấp từ file SQL legacy thuần túy.
   - Đầu việc này được tách thành một hạng mục riêng trong backlog: **`T02-LEGACY-SQL`** (chuyển sang Tuần 6, phối hợp cùng nhiệm vụ diễn tập di chuyển dữ liệu `T23`), và **tuyệt đối không được coi là đã hoàn thành** trong phạm vi T02.

---

## 4. Ghi Chú Kỹ Thuật Bắt Buộc Về Phương Thức `Down()` Của Migration

> [!WARNING]
> **Phương thức `Down()` chỉ khôi phục cấu trúc schema, không khôi phục dữ liệu shadow đã xóa:**
>
> 1. Khi rollback migration `20261110141626_FixLegacySchemaAndConstraints.cs`, phương thức `Down()` tái tạo lại cấu trúc cột (`TaiKhoanMaTaiKhoan`), thiết lập lại Foreign Key và drop Unique Index trên `TenDangNhap`.
> 2. `Down()` **KHÔNG TỰ PHỤC HỒI các giá trị shadow data** đã bị xóa khi cột bị DROP ở phương thức `Up()`. Toàn bộ giá trị trong cột `TaiKhoanMaTaiKhoan` sau khi rollback bằng `Down()` sẽ nhận giá trị `NULL`.
> 3. Tuyệt đối **không được mô tả `Down()` là cơ chế rollback dữ liệu đầy đủ**. Trong môi trường sản phẩm, nếu cần hoàn nguyên cả dữ liệu shadow, bắt buộc phải restore từ bản backup database được tạo trước thời điểm chạy migration.

---

## 5. Quy Định Phạm Vi Đối Chiếu Đối Với Cơ Sở Dữ Liệu `HRM_System`

1. **Phạm vi đối chiếu thực tế đã thực hiện**:
   - Database sản phẩm `HRM_System` chỉ được khảo sát thông qua kết nối **chỉ đọc (read-only)** bằng `sqlcmd` với Windows Authentication.
   - Khảo sát đã ghi nhận: Metadata schema, 7 dòng trong `__EFMigrationsHistory`, và số lượng bản ghi tại thời điểm khảo sát (NhanVien: 10, TaiKhoan: 7, ChamCong: 8, BangLuong: 10, PasswordResetTokens: 1).
2. **Nguyên tắc báo cáo trung thực**:
   - **Tuyệt đối không mô tả `HRM_System` được bảo toàn mọi giá trị chỉ dựa vào số dòng**. Lý do: Trong suốt quá trình T02, `HRM_System` hoàn toàn không bị can thiệp ghi hay chạy bất kỳ migration nào; do đó chưa từng có quá trình thay đổi schema để đối soát trước/sau trên database này.
   - Toàn bộ kết quả đối soát tự động 42 bản ghi từng cột là kết quả đo đạc thực nghiệm trên database test riêng biệt `HRM_Next_Test_T02_v7_UpgradeValid`.
   - Cơ sở dữ liệu `HRM_System` được bảo toàn nguyên trạng bằng biện pháp **cô lập tuyệt đối và không can thiệp**.

---

## 6. Cam Kết Kỷ Luật & Trạng Thái Bàn Giao

- **Mã nguồn sản phẩm & Migration**: Giữ nguyên trạng thái đã kiểm chứng; không sửa thêm code sau khi hoàn tất kiểm thử.
- **Cơ sở dữ liệu sản phẩm**: Tuyệt đối không chạy migration lên `HRM_System`.
- **Git**: Không thực hiện `git commit` hay `git push`.
- **Nhiệm vụ tiếp theo**: Giữ nguyên kỷ luật, chưa bắt đầu nhiệm vụ `T03` khi chưa có chỉ lệnh bằng văn bản từ chủ dự án.

