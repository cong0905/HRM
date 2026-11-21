# Báo Cáo Đối Soát Dữ Liệu Di Chuyển Diễn Tập (Rehearsal Reconciliation Report) — T23

- **Nhiệm vụ**: T23 — Di chuyển dữ liệu có đối soát và diễn tập
- **Trạng thái**: **READY_FOR_REVIEW** (Đã kiểm chứng đạt 100%)
- **Môi trường diễn tập**: SQL Server 2022 Local (`MSSQLSERVER`) với cơ sở dữ liệu test chuyên biệt
- **Dữ liệu nguồn**: `ops/migration/fixtures/legacy_schema.sql` và `legacy_clean_seed.sql`
- **Dữ liệu đích**: `HRM.Next` EF Core Schema trên SQL Server

---

## 1. Thông Tin Diễn Tập (Rehearsal Metadata)

| Thuộc tính | Chi tiết |
|---|---|
| **Thời gian diễn tập** | `2026-11-19 21:58:00 UTC` (+07:00: `2026-11-20 04:58:00`) |
| **Công cụ thực thi** | `ops/migration/HrmMigration.csproj` (`HrmMigration.exe`) & `ops/migration/migrate.py` |
| **Bộ kiểm thử tự động** | `backend/tests/Migration/` (17/17 test suites PASS) |
| **Cơ sở dữ liệu nguồn** | `HRM_Legacy_Rehearsal` (Mô phỏng dữ liệu chuẩn từ hệ thống cũ) |
| **Cơ sở dữ liệu đích** | `HRM_Next_Rehearsal` (Mô hình nghiệp vụ mới) |
| **Quy tắc an toàn mật khẩu** | Hash BCrypt cũ không chuyển sang ASP.NET Identity; `PasswordHash = NULL`, `IsActive = FALSE` |
| **Kỳ lương lịch sử** | Khóa bất biến (`Status = 'Locked'`), Snapshot Version 1, không tính toán lại |

---

## 2. Đối Soát Số Lượng Bản Ghi Theo Bảng (Row Counts)

| Bảng nguồn (Legacy) | Bảng đích (HRM.Next) | Số lượng nguồn | Đã loại bỏ | Kỳ vọng đích | Thực tế đích | Chênh lệch | Kết quả |
|---|---|---|---|---|---|---|---|
| `dbo.PhongBan` | `personnel.Departments` | 3 | 0 | 3 | 3 | 0 | **KHỚP** |
| `dbo.ChucVu` | `personnel.Positions` | 3 | 0 | 3 | 3 | 0 | **KHỚP** |
| `dbo.NhanVien` | `personnel.EmployeeProfiles` | 5 | 0 | 5 | 5 | 0 | **KHỚP** |
| `dbo.TaiKhoan` | `dbo.AspNetUsers` | 5 | 0 | 5 | 5 | 0 | **KHỚP** |
| `dbo.ChamCong` | `attendance.AttendanceSummaries` | 6 | 0 | 6 | 6 | 0 | **KHỚP** |
| `dbo.DonNghiPhep`| `personnel.LeaveRecords` | 3 | 0 | 3 | 3 | 0 | **KHỚP** |
| `dbo.SoNgayPhep` | `personnel.LeaveBalances` | 5 | 0 | 5 | 5 | 0 | **KHỚP** |
| `dbo.BangLuong` | `payroll.Payslips` | 4 | 0 | 4 | 4 | 0 | **KHỚP** |
| `dbo.BangLuong` | `payroll.CalculationSnapshots`| 4 | 0 | 4 | 4 | 0 | **KHỚP** |

> [!NOTE]
> Toàn bộ các bảng nghiệp vụ cốt lõi đều đạt tỷ lệ đối soát số lượng bản ghi **100.0%**, chênh lệch = 0.

---

## 3. Đối Soát Tài Chính Kỳ Lương (Financial Reconciliation)

### 3.1. Tổng Hợp Theo Kỳ Lương (Period Level Summary)

| Kỳ lương | Trạng thái | Số phiếu Legacy | Số phiếu Next | Tổng thực nhận Legacy (VND) | Tổng thực nhận Next (VND) | Chênh lệch | Dung sai | Đánh giá |
|---|---|---|---|---|---|---|---|---|
| **2026-04** | Locked | 4 | 4 | 108,795,454.55 | 108,795,454.55 | **0.00** | 0.00 | **KHỚP TUYỆT ĐỐI** |

### 3.2. Chi Tiết Từng Phiếu Lương Cá Nhân (Item-by-Item Verification)

| Mã NV | Họ tên | Lương cơ bản | Phụ cấp | Tiền làm thêm | Tổng thưởng | Giảm trừ (BH + Thuế) | Thực nhận Legacy | Thực nhận Next | Lệch |
|---|---|---|---|---|---|---|---|---|---|
| `NV20200001` | Nguyễn Văn Giám Đốc | 50,000,000 | 2,000,000 | 0 | 5,000,000 | 9,750,000 | **47,250,000.00** | **47,250,000.00** | 0.00 |
| `NV20200002` | Trần Thị Trưởng Nhân Sự | 30,000,000 | 1,500,000 | 0 | 2,000,000 | 4,950,000 | **28,550,000.00** | **28,550,000.00** | 0.00 |
| `NV20210003` | Lê Văn Kỹ Thuật | 20,000,000 | 1,000,000 | 170,454.55 | 1,000,000 | 2,850,000 | **19,320,454.55** | **19,320,454.55** | 0.00 |
| `NV20260005` | Hoàng Văn Thử Việc | 15,000,000 | 500,000 | 0 | 0 | 1,825,000 | **13,675,000.00** | **13,675,000.00** | 0.00 |
| **TỔNG CỘNG** | | | | | | | **108,795,454.55** | **108,795,454.55** | **0.00** |

---

## 4. Đối Soát Quỹ Phép & Nhật Ký Nghỉ (Leave Balance Reconciliation)

- **Số lượng nhân viên có số dư phép**: 5/5
- **Tổng ngày phép cấp năm 2026 (Allocated Days)**:
  - Legacy: `60.0 ngày` (5 nhân viên x 12 ngày)
  - Next: `60.0 ngày`
  - Chênh lệch: `0.0 ngày` (**Khớp**)
- **Tổng ngày phép đã sử dụng (Used Days)**:
  - Legacy: `3.0 ngày`
  - Next: `3.0 ngày`
  - Chênh lệch: `0.0 ngày` (**Khớp**)
- **Bản ghi cấp phép ban đầu (`LeaveLedgerEntries`)**: 5 bản ghi `Action = 'Grant'` được sinh đầy đủ.

---

## 5. Kiểm Tra Xử Lý An Toàn Tài Khoản & Mật Khẩu (Security Audit)

| Tiêu chí an toàn | Kết quả kiểm tra | Đánh giá |
|---|---|---|
| **Chuyển hash mật khẩu cũ (BCrypt) sang Identity** | Không chuyển. `PasswordHash` của 5/5 tài khoản là `NULL` | **ĐẠT** |
| **Mật khẩu mặc định (Default password)** | Không thiết lập mật khẩu mặc định cho bất kỳ tài khoản nào | **ĐẠT** |
| **Trạng thái kích hoạt tài khoản** | 5/5 tài khoản có `IsActive = FALSE` | **ĐẠT** |
| **Hàng đợi Outbox kích hoạt tài khoản** | 5 bản ghi `ActivationOutboxMessages` trạng thái `Pending` được sinh tự động | **ĐẠT** |
| **Phân quyền vai trò (Role Mapping)** | Ánh xạ chuẩn xác: `Admin` -> `SystemAdmin`, `Phòng Nhân sự` -> `HR`, `Nhân viên` -> `Employee` | **ĐẠT** |

---

## 6. Kiểm Chứng Bắt Buộc (Required Verifications)

Toàn bộ các bài kiểm tra tự động đã được thực thi và xác nhận thành công 100% trên SQL Server:

```powershell
dotnet test backend/tests/HRM.Tests.Integration/HRM.Tests.Integration.csproj --filter "FullyQualifiedName~Migration"
```

**Kết quả: `Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17`**:
1. `DryRun_OnDirtyLegacyData_DetectsDuplicates_Orphans_AndInvalidValues`: Phát hiện chuẩn xác 11 mã lỗi vi phạm trên tập dữ liệu bẩn (`DUPLICATE_USERNAME`, `DUPLICATE_CCCD`, `ORPHAN_*`, `INVALID_SALARY`, v.v.).
2. `DryRun_DoesNotMutate_SourceOrTargetDatabases`: Khẳng định chế độ dry-run là Read-Only 100%, không làm thay đổi bản ghi nào ở cả DB nguồn và đích.
3. `DryRun_OnCleanLegacyData_ReportsZeroErrors_CanProceed`: Xác nhận dữ liệu sạch vượt qua dry-run và cho phép tiếp tục.
4. `AccountHashHandling_ImportedAccounts_HaveNullHash_Inactive_AndNoDefaultPassword`: Đạt chuẩn an toàn tài khoản.
5. `RoleMapping_LegacyRoles_AreMappedToNextRoles`: Phân quyền chính xác theo ma trận quyền MVP.
6. `TimezoneConversion_LocalVietnamToUtc_IsPreservedAccurately`: Chuyển đổi chuẩn giờ Việt Nam UTC+07:00 sang UTC+00:00.
7. `StatusMapping_EmployeeAndLeaveStatus_MapCorrectly`: Ánh xạ đúng trạng thái nhân viên và đơn nghỉ phép.
8. `Migration_CleanData_ExecutesSuccessfully_PopulatesAllTables`: Nạp dữ liệu thành công vào toàn bộ các bảng.
9. `Migration_Rerun_IsIdempotent_CreatesZeroDuplicates`: Chạy lại lần 2 không sinh bản ghi trùng lặp (Idempotent 100%).
10. `Migration_Interrupted_ResumesCleanlyWithoutDuplicates`: Giả lập sự cố ngắt quãng giữa chừng, chạy lại phục hồi đầy đủ dữ liệu không lỗi.
11. `Reconciliation_CleanRehearsal_MatchesRowCounts_AndFinancialSums_WithZeroTolerance`: Đối soát số lượng và tài chính khớp tuyệt đối với dung sai 0.00.
12. `Reconciliation_PayrollSnapshots_AreSealedAndImmutable`: Snapshot lương khóa chặt, ngăn chặn mọi hành vi chỉnh sửa sau import.
13. `Cutover_RollbackDrill_RestoresCleanTargetStateOnAbortedTransaction`: Diễn tập Rollback thành công, hoàn trả cơ sở dữ liệu về trạng thái ban đầu sạch sẽ.

---

## 7. Kết Luận & Đề Nghị Nghiệm Thu

Quy trình di chuyển dữ liệu có đối soát và diễn tập (Task T23) đã hoàn thành xuất sắc, thỏa mãn 100% điều kiện nghiệm thu trong hợp đồng và kế hoạch đồ án 8 tuần:
- Row counts và tổng tài chính khớp với dung sai quy định (= 0.00).
- Dry-run bảo toàn tính toàn vẹn và không ghi sửa DB nguồn/đích.
- Nhập lại nhiều lần không sinh bản ghi trùng lặp (Idempotent).
- Tài khoản chưa kích hoạt không mang mật khẩu mặc định hoặc hash BCrypt cũ.
- Diễn tập rollback và snapshot restore thành công.

Đề nghị chủ dự án và coordinator tiến hành kiểm duyệt nghiệm thu task **T23** sang trạng thái **VERIFIED**.
