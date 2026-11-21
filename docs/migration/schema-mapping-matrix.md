# Ma Trận Chuyển Đổi Dữ Liệu (Schema Mapping Matrix) — T23

Tài liệu này xác định quy tắc ánh xạ kỹ thuật chi tiết từ cơ sở dữ liệu legacy (`HRM_Database.sql`) sang cơ sở dữ liệu thế hệ mới HRM.Next (EF Core / SQL Server), tuân thủ đặc tả MVP [02_DAC_TA_MVP.md](../../HRM_KeHoach_DoAn_8Tuan/02_DAC_TA_MVP.md) và nhiệm vụ [T23.md](../../HRM_KeHoach_DoAn_8Tuan/tasks/T23.md).

---

## 1. Nguyên Tắc Thiết Kế Cốt Lõi

1. **Khóa chính định danh tất định (Deterministic UUIDv5)**:
   - Các ID dạng số nguyên `INT IDENTITY` ở legacy được chuyển đổi sang `Guid` (UUIDv5) bằng thuật toán băm SHA1 kết hợp định danh Namespace thực thể và mã số legacy (`"Dept_{id}"`, `"Emp_{id}"`, v.v.).
   - Đảm bảo tính **Idempotent 100%**: chạy lại script nhiều lần hoặc phục hồi sau khi bị ngắt quãng giữa chừng đều sinh cùng một khóa chính, không bao giờ sinh bản ghi trùng lặp.

2. **Xử lý An toàn Mật khẩu & Tài khoản (Account Hash Handling)**:
   - **Tuyệt đối không chuyển đổi hash mật khẩu cũ (BCrypt/MD5/SHA)** vào `AspNetUsers.PasswordHash` của ASP.NET Identity (tránh rủi ro giải mã hoặc không tương thích thuật toán).
   - **Tuyệt đối không đặt mật khẩu mặc định** (như `123456` hay `Password@123`).
   - Mọi tài khoản import được thiết lập:
     - `PasswordHash = NULL`
     - `IsActive = FALSE` (Chưa kích hoạt)
   - Đồng thời tự động phát sinh bản ghi `[personnel].[ActivationOutboxMessages]` trạng thái `Pending` để nhân viên tự thiết lập mật khẩu qua luồng kích hoạt an toàn.

3. **Bảo Toàn Lịch Sử Lương & Snapshot Bất Biến (Payroll Immutability)**:
   - Kỳ lương cũ đã chốt được chuyển thẳng sang trạng thái `Locked` kèm `PayrollCalculationBatch` (phiên bản 1) và `PayrollCalculationSnapshot`.
   - **Không tính toán lại lịch sử (Zero Recalculation)**; bảo toàn nguyên vẹn số tiền thực nhận (`LuongThucNhan`), lương cơ bản, phụ cấp, giảm trừ cũ.

4. **Chuẩn Hóa Múi Giờ (Timezone Standardization)**:
   - Toàn bộ thời gian chấm công và nhật ký hệ thống ở legacy ghi theo giờ địa phương Việt Nam (`Asia/Ho_Chi_Minh`, UTC+07:00) được quy đổi sang `DateTimeOffset` UTC (`+00:00`) chuẩn ISO-8601 khi lưu trữ trong DB mới.
   - Ngày nghiệp vụ (`BusinessDate`) giữ nguyên định dạng `DateOnly` (`YYYY-MM-DD`).

---

## 2. Bảng Ánh Xạ Chi Tiết Các Thực Thể

### 2.1. Phòng Ban (`dbo.PhongBan` -> `personnel.Departments`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `MaPhongBan` | INT PK | `Id` | Guid PK | UUIDv5(`DepartmentNamespace`, `"Dept_" + MaPhongBan`) |
| `TenPhongBan` | NVARCHAR(100) | `Name` | nvarchar(100) | `TenPhongBan.Trim()` |
| `NgayTao` | DATETIME | `CreatedAtUtc` | datetimeoffset(7) | Quy đổi từ UTC+07:00 sang UTC; nếu NULL lấy thời điểm import |
| `MoTaChucNang` | NVARCHAR(500) | - | - | Lưu trong metadata hoặc bỏ qua ở bảng cốt lõi MVP |
| `TrangThai` | NVARCHAR(20) | - | - | Chỉ import phòng ban có trạng thái hợp lệ |

---

### 2.2. Chức Vụ (`dbo.ChucVu` -> `personnel.Positions`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `MaChucVu` | INT PK | `Id` | Guid PK | UUIDv5(`PositionNamespace`, `"Pos_" + MaChucVu`) |
| `TenChucVu` | NVARCHAR(100) | `Name` | nvarchar(100) | `TenChucVu.Trim()` |
| - | - | `CreatedAtUtc` | datetimeoffset(7) | Thời điểm import UTC |

---

### 2.3. Nhân Viên (`dbo.NhanVien` -> `personnel.EmployeeProfiles`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `MaNhanVien` | INT PK | `Id` | Guid PK | UUIDv5(`EmployeeNamespace`, `"Emp_" + MaNhanVien`) |
| `MaNV` | VARCHAR | `EmployeeCode` | nvarchar(20) UK | Giữ nguyên mã legacy nếu có, hoặc sinh chuẩn `NV{NămVàoLàm}{MaNhanVien:D4}` |
| `HoTen` | NVARCHAR(100) | `FullName` | nvarchar(100) | `HoTen.Trim()` |
| `NgaySinh` | DATE | `DateOfBirth` | date | `DateOnly.FromDateTime(NgaySinh)` |
| `GioiTinh` | NVARCHAR(10) | `Gender` | nvarchar(20) | Giữ nguyên (`Nam`, `Nữ`, `Khác`) |
| `CCCD` | VARCHAR(12) | `CitizenId` | nvarchar(20) | Giữ nguyên; kiểm tra trùng lặp ở pha dry-run |
| `DiaChi` | NVARCHAR(300) | `Address` | nvarchar(256) | Cắt tối đa 256 ký tự nếu vượt |
| `SoDienThoai` | VARCHAR(15) | `Phone` | nvarchar(20) | Chuẩn hóa định dạng số điện thoại |
| `Email` | VARCHAR(100) | `Email` | nvarchar(256) | Chuyển chữ thường, kiểm tra unique |
| `TinhTrangHonNhan` | NVARCHAR(20) | `MaritalStatus` | nvarchar(50) | Giữ nguyên |
| `MaPhongBan` | INT FK | `DepartmentId` | Guid FK | Ánh xạ qua `ledger.Departments[MaPhongBan]` |
| `MaChucVu` | INT FK | `PositionId` | Guid FK | Ánh xạ qua `ledger.Positions[MaChucVu]` |
| `NgayVaoLam` | DATE | `HireDate` | date | `DateOnly.FromDateTime(NgayVaoLam)` |
| `NgayNghiViec` | DATE | `TerminationDate` | date | `DateOnly.FromDateTime(NgayNghiViec)` nếu có |
| `TrangThai` | NVARCHAR(30) | `Status` | nvarchar(50) | Ánh xạ enum: `'Đang làm việc' -> 'DangLamViec'`, `'Nghỉ việc' -> 'DaNghiViec'`, `'Thử việc' -> 'ThuViec'`, `'Nghỉ phép dài hạn' -> 'NghiPhepDaiHan'` |
| `MucLuong` | DECIMAL(18,2) | (Bảng riêng) | - | Chuyển thành bản ghi khởi tạo trong `payroll.SalaryHistory` |
| `NgayTao` | DATETIME | `CreatedAtUtc` | datetimeoffset(7) | Quy đổi UTC+07:00 sang UTC |
| - | - | `UserId` | Guid FK | Liên kết tài khoản `AspNetUsers.Id` của nhân viên |

---

### 2.4. Tài Khoản Người Dùng (`dbo.TaiKhoan` -> `dbo.AspNetUsers` & `dbo.AspNetUserRoles`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `MaTaiKhoan` | INT PK | `Id` | Guid PK | UUIDv5(`UserNamespace`, `"User_" + MaNhanVien`) |
| `TenDangNhap` | VARCHAR(50) | `UserName` | nvarchar(256) | `TenDangNhap.Trim()`, `NormalizedUserName = ToUpper()` |
| `Email` (từ NV) | VARCHAR(100) | `Email` | nvarchar(256) | `Email.Trim()`, `NormalizedEmail = ToUpper()` |
| `MatKhauHash` | VARCHAR(256) | `PasswordHash` | nvarchar(max) | **NULL**. Không chuyển hash cũ, không đặt mật khẩu mặc định |
| `TrangThai` | NVARCHAR(20) | `IsActive` | bit | **FALSE**. Mọi tài khoản import ở trạng thái chưa kích hoạt |
| - | - | `EmailConfirmed` | bit | `TRUE` |
| - | - | `LockoutEnabled` | bit | `TRUE` |
| `VaiTro` | NVARCHAR(30) | `AspNetUserRoles` | FK Role | Ánh xạ vai trò:<br>- `'Admin'` -> `'SystemAdmin'`<br>- `'Phòng Nhân sự'`, `'HR'`, `'Quản lý'` -> `'HR'`<br>- `'KeToan'` -> `'Payroll'`<br>- `'Nhân viên'` -> `'Employee'` |
| - | - | `personnel.ActivationOutboxMessages` | Bảng Outbox | Sinh bản ghi Outbox kích hoạt (`Purpose = 'activation'`, `Status = 'Pending'`) |

---

### 2.5. Chấm Công (`dbo.ChamCong` -> `attendance.AttendanceSummaries` & `attendance.AttendanceEvents`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `MaChamCong` | INT PK | `Id` | Guid PK | UUIDv5(`AttendanceNamespace`, `"Att_" + MaChamCong`) |
| `MaNhanVien` | INT FK | `EmployeeId` | Guid FK | Ánh xạ qua `ledger.Employees[MaNhanVien]` |
| `NgayChamCong` | DATE | `BusinessDate` | date | `DateOnly.FromDateTime(NgayChamCong)` |
| `GioVao` | TIME | `CheckInUtc` | datetimeoffset(7) | `NgayChamCong + GioVao` quy đổi từ UTC+07:00 sang UTC |
| `GioRa` | TIME | `CheckOutUtc` | datetimeoffset(7) | `NgayChamCong + GioRa` quy đổi từ UTC+07:00 sang UTC (hoặc NULL nếu chưa checkout) |
| `TongGioLam` | DECIMAL(5,2) | `EligibleMinutes` | decimal(20,10) | `(TongGioLam ?? 8.0) * 60m` |
| `TrangThai` | NVARCHAR(30) | `LateMinutes`, `EarlyLeaveMinutes` | decimal(20,10) | Nếu chứa 'Đi muộn' -> 15m; 'Về sớm' -> 15m; còn lại 0m |
| - | - | `Phase` | nvarchar(20) | `CheckOutUtc != null ? "Closed" : "Open"` |
| - | - | `CalendarVersionId` | Guid FK | Liên kết WorkCalendarVersion chuẩn đang áp dụng |
| - | - | `AttendanceEvents` | Bảng Event | Sinh event CheckIn và CheckOut bất biến làm bằng chứng audit |

---

### 2.6. Nghỉ Phép (`dbo.DonNghiPhep`, `dbo.SoNgayPhep` -> `personnel.LeaveRecords`, `LeaveBalances`, `LeaveLedgerEntries`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `MaDonPhep` | INT PK | `Id` | Guid PK | UUIDv5(`LeaveNamespace`, `"LeaveReq_" + MaDonPhep`) |
| `MaNhanVien` | INT FK | `EmployeeId` | Guid FK | Ánh xạ qua `ledger.Employees[MaNhanVien]` |
| `NgayBatDau` | DATE | `StartDate` | date | `DateOnly.FromDateTime(NgayBatDau)` |
| `NgayKetThuc` | DATE | `EndDate` | date | `DateOnly.FromDateTime(NgayKetThuc)` |
| `SoNgayNghi` | DECIMAL(3,1) | `WorkingDays` | decimal(18,2) | `SoNgayNghi` |
| `LyDo` | NVARCHAR(500) | `Reason` | nvarchar(500) | `LyDo.Trim()` |
| `TrangThai` | NVARCHAR(20) | `Status` | nvarchar(20) | Ánh xạ:<br>- `'Đã duyệt'` -> `'Approved'`<br>- `'Từ chối'` -> `'Rejected'`<br>- `'Đã hủy'` -> `'Cancelled'`<br>- `'Chờ duyệt'` -> `'Pending'` |
| `NguoiPheDuyet`| INT FK | `ApproverId` | Guid FK | Ánh xạ nhân viên duyệt |
| `NgayPheDuyet` | DATETIME | `DecidedAtUtc` | datetimeoffset(7) | Quy đổi sang UTC |
| `TongSoNgayPhep`| INT | `LeaveBalances.AllocatedDays` | decimal(18,2) | Cấp quỹ phép năm (`AllocatedDays = TongSoNgayPhep`) |
| `SoNgayDaSuDung`| INT | `LeaveBalances.UsedDays` | decimal(18,2) | Ngày phép đã dùng (`UsedDays = SoNgayDaSuDung`) |
| - | - | `LeaveLedgerEntries`| Bảng Ledger | Sinh bản ghi `Action = 'Grant'` ghi nhận cấp phép ban đầu |

---

### 2.7. Bảng Lương (`dbo.BangLuong` -> `payroll.PayrollPeriods`, `CalculationBatches`, `CalculationSnapshots`, `Payslips`)

| Cột Legacy | Kiểu Legacy | Cột HRM.Next | Kiểu HRM.Next | Quy tắc chuyển đổi |
|---|---|---|---|---|
| `Thang`, `Nam` | INT | `PayrollPeriods.PeriodCode` | nvarchar(20) | `"{Nam:D4}-{Thang:D2}"` (Ví dụ: `2026-04`) |
| `TrangThai` | NVARCHAR(20) | `PayrollPeriods.Status` | nvarchar(20) | `'Locked'` (Bất biến, không thể sửa sau khi import) |
| - | - | `CalculationBatches` | Bảng Batch | Batch Version 1, ghi nhận thời điểm khóa kỳ và InputHash |
| `MaBangLuong` | INT PK | `CalculationSnapshots.Id` | Guid PK | UUIDv5(`PayrollSnapshotNamespace`, `"Snap_" + MaBangLuong`) |
| `LuongThucNhan`| DECIMAL(18,2) | `CalculationSnapshots.Net` | decimal(20,2) | Lưu chính xác số tiền thực nhận cũ |
| - | - | `CalculationSnapshots.OutputJson` | nvarchar(max) | Lưu đầy đủ cấu trúc: Lương cơ bản, phụ cấp, OT, thưởng, khấu trừ, thực nhận |
| `MaBangLuong` | INT PK | `Payslips.Id` | Guid PK | UUIDv5(`PayslipNamespace`, `"Slip_" + MaBangLuong`) |
| `LuongThucNhan`| DECIMAL(18,2) | `Payslips.Amount` | decimal(18,2) | `Amount = LuongThucNhan` |
| - | - | `Payslips.Status` | nvarchar(20) | `'Locked'` |
