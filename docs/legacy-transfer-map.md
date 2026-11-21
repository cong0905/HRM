# Bản đồ Chuyển giao Nghiệp vụ và Khoanh vùng Lỗi Legacy (Legacy Transfer Map)

> **Dự án:** Hệ thống Quản lý Nhân sự (HRM System) — Đồ án Tốt nghiệp 8 tuần  
> **Nhiệm vụ:** T03 — Khoanh vùng lỗi legacy và lập bản đồ chuyển nghiệp vụ  
> **Tài liệu tham chiếu:** [tasks/T03.md](file:///d:/dotnet%20project/HRM_KeHoach_DoAn_8Tuan/tasks/T03.md), [02_DAC_TA_MVP.md](file:///d:/dotnet%20project/HRM_KeHoach_DoAn_8Tuan/02_DAC_TA_MVP.md), [06_DOI_CHIEU_NGUON.md](file:///d:/dotnet%20project/HRM_KeHoach_DoAn_8Tuan/06_DOI_CHIEU_NGUON.md), [docs/business-rules.md](file:///d:/dotnet%20project/docs/business-rules.md), Biên bản [T00-signoff.md](file:///d:/dotnet%20project/docs/decisions/T00-signoff.md), [T01-signoff.md](file:///d:/dotnet%20project/docs/decisions/T01-signoff.md), [T02-signoff.md](file:///d:/dotnet%20project/docs/decisions/T02-signoff.md)  
> **Ngày lập:** 11/11/2026  
> **Người thực hiện:** AI Assistant (Antigravity) | **Kiểm duyệt:** Chủ dự án / Codex  

---

## 1. Ranh giới Kỹ thuật và Tuyên bố An toàn Dữ liệu

### 1.1. Nguyên tắc Cốt lõi của Bản đồ Chuyển giao
1. **Không mang lỗi đã biết sang backend mới:** Toàn bộ các khiếm khuyết trong mã nguồn WinForms và BLL legacy (`db/src/HRM.BLL/Services`) được khoanh vùng và phân rã thành các quy tắc thiết kế bắt buộc cho kiến trúc mới.
2. **Không sửa tràn lan hai hệ thống:** Mã nguồn legacy tại `db/src` được bảo toàn làm tài liệu đối chiếu (reference baseline); các chỉnh sửa chỉ được thực hiện trên backend mới trong phạm vi từng task được phân công (T05–T15).
3. **Phân biệt rõ ràng các trạng thái kỹ thuật:**
   - **`LEGACY_CHƯA_SỬA`**: Lỗi và giới hạn đang tồn tại trong mã nguồn legacy, đã được chỉ ra tại `06_DOI_CHIEU_NGUON.md` hoặc kiểm chứng bằng test. Việc ghi nhận tài liệu **TUYỆT ĐỐI KHÔNG ĐỒNG NGHĨA VỚI VIỆC LỖI ĐÃ ĐƯỢC KHẮC PHỤC**.
   - **`SCHEMA_BASELINE_EF_ĐÃ_KIỂM_CHỨNG (T02)`**: Đã sửa và kiểm chứng ở tầng schema/migration EF baseline legacy (T02), **KHÔNG PHẢI backend nghiệp vụ mới đã triển khai**.
   - **`CHƯA_KIỂM_CHỨNG (UNVERIFIED)`**: Đường nâng cấp từ file SQL legacy thuần túy (`HRM_Database.sql`) chưa kiểm chứng, chuyển sang đầu việc riêng `T02-LEGACY-SQL` (Tuần 6); toàn bộ backend use cases mới chưa triển khai.

---

### 1.2. Cảnh báo Nguy cơ Nghiêm trọng đối với Dữ liệu Thật (Production Safety Warning)

> [!CAUTION]
> **HỆ THỐNG LEGACY CHỨA CÁC LUỒNG XỬ LÝ NGUY HIỂM — CẤM SỬ DỤNG VỚI DỮ LIỆU THẬT:**
>
> 1. **Luồng Tính lại Lương (`BangLuongService.TinhVaLuuBangLuongThangAsync`):**
>    - Phương thức này thực thi lệnh xóa trắng toàn bộ dữ liệu bảng lương của tháng/năm (`await _bangLuongRepo.XoaTheoThangNamAsync(thang, nam)`) **TRƯỚC KHI** thực hiện tính toán mới.
>    - Chưa có transaction bao toàn bộ bước xóa và tính lại. Lỗi sau khi xóa có thể làm mất hoặc thiếu dữ liệu kỳ lương; việc phục hồi phụ thuộc bản sao lưu và quy trình recovery, không thể khẳng định mọi lỗi đều gây mất dữ liệu vĩnh viễn.
> 2. **Luồng Đọc Gây Ghi Đè Lịch sử (`BangLuongService.GetBangLuongAsync`):**
>    - Thao tác xem danh sách lương (Query) tự động kích hoạt đồng bộ mức lương và điểm hiệu suất hiện tại vào các bản ghi quá khứ (`DongBoLuongCoBanThangAsync`), làm biến động các kỳ lương đã thanh toán.
> 3. **Luồng Đặt lại Mật khẩu (`AuthService.ResetPasswordWithTokenAsync`):**
>    - Không kiểm tra mục đích token (`token.Purpose`), cho phép dùng token kích hoạt/xác thực email để ghi đè mật khẩu; mã OTP sinh ngẫu nhiên bằng `Random.Shared` không an toàn.
> 4. **Điều kiện sử dụng thực tế:**
>    - Nếu bất kỳ đơn vị nào có ý định sử dụng trực tiếp mã nguồn legacy trên dữ liệu thực tế, hệ thống **bắt buộc phải được re-scope và thực hiện gói hotfix khẩn cấp** trước khi cấp quyền truy cập cho người dùng.

---

## 2. Bảng Phân bổ Trách nhiệm và Kiểm thử cho Danh mục Phát hiện F01–F14

Dưới đây là bảng đối soát chi tiết toàn bộ các phát hiện kỹ thuật từ F01 đến F14 theo đặc tả đối chiếu mã nguồn:

| Mã | Tên Phát hiện | File & Dòng Nguồn Legacy | Hậu quả Kỹ thuật | Task Chịu Trách nhiệm | Bộ Kiểm thử Bắt buộc (Tests Cần có) | Trạng thái Hiện tại |
| :---: | :--- | :--- | :--- | :---: | :--- | :---: |
| **`F01`** | Migration Hwid thiếu metadata và không tạo cột | [`20260502105319_AddChamCongHwid.cs:1`](file:///d:/dotnet%20project/db/src/HRM.DAL/Migrations/20260502105319_AddChamCongHwid.cs#L1)<br>[`ChamCongConfiguration.cs:18`](file:///d:/dotnet%20project/db/src/HRM.DAL/Configurations/ChamCongConfiguration.cs#L18)<br>*(Đối chiếu T02 audit: Snapshot cũ từng dẫn nhầm file `20260505060254_AddChamCongHwid.cs` không tồn tại; migration đúng tạo cột là `20260502105319`; class thừa `20260504161102_AddHwidToChamCong.cs` thiếu metadata nhưng không tạo Hwid; script tài liệu `HRM_Database.sql` thiếu cột)* | EF Migrations bỏ qua migration thiếu attribute; schema không đồng bộ giữa code và DB | **`T02`** (Baseline)<br>**`T01`** (Regression) | `EfMigrator_OfflineScript_SchemaMustContainHwidColumnOnChamCong`<br>`AllMigrationClasses_MustHaveValidMigrationAttribute` | **SCHEMA_BASELINE_EF_ĐÃ_KIỂM_CHỨNG (T02)**<br>*(Chỉ phạm vi EF baseline; SQL legacy thuần túy UNVERIFIED; backend mới chưa triển khai)* |
| **`F02`** | Đọc bảng lương ghi DB; sửa lương ghi đè lịch sử | [`BangLuongService.cs:36,70`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/BangLuongService.cs#L36)<br>[`NhanVienService.cs:121`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/NhanVienService.cs#L121) | Xem bảng lương hoặc đổi lương nhân viên làm sai lệch số liệu các kỳ quá khứ đã khóa | **`T13`** (SalaryHistory)<br>**`T14`** (Immutability) | `DongBoBangLuongTheoNhanVien_WhenSalaryUpdated_MustNotOverwritePastPayrollRecords`<br>`Test_PayrollPeriod_Locked_IsImmutable` | **LEGACY_CHƯA_SỬA** (Đã có test tái hiện lỗi ở T01 & T03) |
| **`F03`** | Tính lại lương xóa kỳ cũ trước, thiếu transaction | [`BangLuongService.cs:169-195`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/BangLuongService.cs#L169-L195)<br>[`Repository.cs:33-44`](file:///d:/dotnet%20project/db/src/HRM.DAL/Repositories/Repository.cs#L33-L44) | Thất bại giữa chừng làm mất toàn bộ bảng lương của tháng; mất tính toàn vẹn dữ liệu | **`T05`** (Unit of Work)<br>**`T14`** (Atomic Run) | `CalculatePayroll_WhenExceptionOccurs_MustRollbackAndRetainPreviousVersion`<br>`PayrollRun_Versioned_AtomicCommit` | **LEGACY_CHƯA_SỬA** (Được khoanh vùng để thiết kế mới ở T14) |
| **`F04`** | Nhân viên chưa có điểm KPI bị tự động phạt trừ 10% | [`HieuSuatService.cs:481`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/HieuSuatService.cs#L481)<br>[`BangLuongService.cs:285`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/BangLuongService.cs#L285) | Người không được đánh giá bị trừ tiền oan; công thức thiếu minh bạch | **`T13`** (Pure Formula) | `TinhBonusHieuSuat_WhenDiemTongIsNull_MustNotPenalizeNegativeTenPercent`<br>`PayrollCalculation_WhenKpiDisabled_UsesNeutralMultiplier` | **LEGACY_CHƯA_SỬA** (Chốt quyết định OPEN-03 tắt liên kết ở T00) |
| **`F05`** | Duyệt phép không kiểm lại quỹ và ghi 2 bước rời rạc | [`DonNghiPhepService.cs:121-146`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/DonNghiPhepService.cs#L121-L146) | Phê duyệt vượt hạn mức làm số dư quỹ phép bị âm; race condition khi 2 người duyệt | **`T11`** (Leave Tx)<br>**`T12`** (Cross-module) | `ApproveLeave_WhenDaysRequestedExceedsRemainingBalance_MustThrowInvalidOperationException`<br>`ApproveLeave_ConcurrentRequests_RowVersionConflict` | **LEGACY_CHƯA_SỬA** (Đã có test tái hiện lỗi ở T01) |
| **`F06`** | Reset mật khẩu không kiểm Purpose; OTP ngẫu nhiên yếu | [`AuthService.cs:57-130`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/AuthService.cs#L57-L130)<br>[`PasswordResetToken.cs:1`](file:///d:/dotnet%20project/db/src/HRM.Domain/Entities/PasswordResetToken.cs#L1) | Token kích hoạt hoặc verify email bị lợi dụng để chiếm đoạt tài khoản; brute force OTP | **`T06`** (Identity & Tokens) | `ResetPasswordWithToken_WhenTokenPurposeIsVerifyEmail_MustBeRejectedAndNotMutatePassword`<br>`PasswordReset_OtpRateLimitAndExpiration` | **LEGACY_CHƯA_SỬA** (Đã có test tái hiện lỗi ở T01) |
| **`F07`** | NullEmailSender báo hoàn tất giả lập dù không gửi | [`NullEmailSender.cs:9`](file:///d:/dotnet%20project/db/src/HRM.Common/Helpers/NullEmailSender.cs#L9)<br>[`frmQuenMatKhau.cs:45`](file:///d:/dotnet%20project/db/src/HRM.GUI/Forms/Auth/frmQuenMatKhau.cs#L45)<br>[`AuthService.cs:80`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/AuthService.cs#L80) | UI thông báo gửi thành công nhưng người dùng không nhận được mail; lệch thời hạn 1h vs 15p | **`T06`** (Auth Flow)<br>**`T22`** (Email Outbox) | `EmailSender_InDevelopmentMode_LogsExplicitWarning`<br>`PasswordReset_ExpiryConsistency_TokenMatchesEmailText` | **LEGACY_CHƯA_SỬA** (Thiết kế Outbox Pattern ở T22) |
| **`F08`** | Thiếu Unique Index username; FK Token dùng shadow property | [`ChinhSachHeThongConfig.cs:14`](file:///d:/dotnet%20project/db/src/HRM.DAL/Configurations/ChinhSachHeThongConfiguration.cs#L14)<br>[`20260517153408_AddPassword...`](file:///d:/dotnet%20project/db/src/HRM.DAL/Migrations/20260517153408_AddPasswordResetTokens.cs#L15) | Trùng lặp tài khoản đăng nhập; quan hệ khóa ngoại không tường minh gây lỗi EF | **`T02`** (Schema Fix)<br>**`T06`** (Identity Model) | `ReconcileSchema_UniqueIndexOnTenDangNhap_MustExist`<br>`ReconcileSchema_ForeignKeyMaTaiKhoan_MustReferenceTaiKhoan` | **SCHEMA_BASELINE_EF_ĐÃ_KIỂM_CHỨNG (T02)**<br>*(Đã sửa trong migration T02; backend Identity mới chưa triển khai, thuộc T06)* |
| **`F09`** | Phân quyền chủ yếu ở GUI; Service chưa là ranh giới an toàn | [`frmMain.cs:122`](file:///d:/dotnet%20project/db/src/HRM.GUI/Forms/Main/frmMain.cs#L122)<br>[`ChamCongService.cs:1`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/ChamCongService.cs#L1)<br>[`BangLuongService.cs:40`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/BangLuongService.cs#L40) | Truyền cờ `bool isAdmin` hoặc client ID từ phía gọi; nguy cơ IDOR và bypass quyền | **`T07`** (Function & Record Auth) | `Endpoint_UnauthorizedAccess_Returns401`<br>`Endpoint_ForbiddenRole_Returns403`<br>`Endpoint_AccessOtherUserData_ReturnsForbiddenOrNotFound` | **LEGACY_CHƯA_SỬA** (Thiết kế Server-Side Authorization ở T07) |
| **`F10`** | Cập nhật nhân viên làm mất Địa chỉ và Tình trạng hôn nhân | [`frmSuaNhanVien.cs:86`](file:///d:/dotnet%20project/db/src/HRM.GUI/Forms/Main/NhanVien/frmSuaNhanVien.cs#L86)<br>[`NhanVienService.cs:98`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/NhanVienService.cs#L98) | Sửa số điện thoại/họ tên làm rơi mất địa chỉ và tình trạng hôn nhân do DTO dùng chung | **`T08`** (Profile DTO)<br>**`T17`** (Web NhanSu) | `UpdateAsync_WhenOptionalFieldsOmittedInDto_DestructivelyOverwritesExistingDataWithNull`<br>`DtoSemantics_CannotDistinguish_BetweenOmittedField_AndIntentionalNullDeletion` | **LEGACY_CHƯA_SỬA** (Đã tạo bộ Characterization test ở T03) |
| **`F11`** | Rủi ro mutation trên entity detached trong snapshot audit | [`Program.cs`](../db/src/HRM.GUI/Program.cs)<br>[`Repository.cs`](../db/src/HRM.DAL/Repositories/Repository.cs)<br>[`TinTuyenDungService.cs`](../db/src/HRM.BLL/Services/TinTuyenDungService.cs) | Chỉ sửa entity detached rồi SaveChanges sẽ không lưu; không kết luận mọi update hiện tại đều lỗi | **`T05`** (Tracking/UoW)<br>**`P01`** (Recruitment) | `DetachedQuery_WhenOnlySaveChangesCalled_DoesNotPersistMutation`<br>`DetachedQuery_WhenRepositoryUpdateCalled_PersistsMutation` | **ĐÃ ĐỐI CHIẾU HIỆN TRẠNG**: Program hiện không bật NoTracking toàn cục; Repository.UpdateAsync gọi Update + SaveChanges. Chưa triển khai backend mới. |
| **`F12`** | Giờ máy trạm, IP nội bộ và HWID không an toàn | [`ChamCongService.cs:25,58`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/ChamCongService.cs#L25)<br>[`ChamCongService.cs:39,69`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/ChamCongService.cs#L39) | Nhân viên có thể chỉnh giờ hệ điều hành hoặc giả mạo IP; check-out đè HWID check-in | **`T09`** (Server Time)<br>**`T10`** (Online Checkin)<br>**`P02`** (Advanced) | `Attendance_MustUseServerUtcTime_NeverClientTimestamp`<br>`AttendanceEvent_CheckOut_RetainsOriginalCheckInMetadata` | **LEGACY_CHƯA_SỬA** (Chốt giờ server UTC ở T00/T09) |
| **`F13`** | Mật khẩu ngày sinh (DOB), đổi mật khẩu không đồng nhất | [`NhanVienService.cs:79`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/NhanVienService.cs#L79)<br>[`frmDoiMatKhau.cs:90`](file:///d:/dotnet%20project/db/src/HRM.GUI/Forms/Main/frmDoiMatKhau.cs#L90)<br>[`TaiKhoanService.cs:1`](file:///d:/dotnet%20project/db/src/HRM.BLL/Services/TaiKhoanService.cs#L1) | Mật khẩu mặc định yếu, đoán được; luồng đổi mật khẩu bỏ qua kiểm tra mật khẩu cũ | **`T06`** (Identity)<br>**`T08`** (Account Life) | `AccountCreation_NeverUsesDefaultDobPassword`<br>`ChangePassword_InvalidCurrentPassword_RejectsRequest` | **LEGACY_CHƯA_SỬA** (Chốt lời mời kích hoạt OPEN-07 ở T00) |
| **`F14`** | Thư viện bắc cầu System.Text.Json 8.0.0 cảnh báo bảo mật | [`HRM.BLL.csproj:1`](file:///d:/dotnet%20project/db/src/HRM.BLL/HRM.BLL.csproj#L1)<br>[`HRM.DAL.csproj:1`](file:///d:/dotnet%20project/db/src/HRM.DAL/HRM.DAL.csproj#L1)<br>[`HRM.GUI.csproj:1`](file:///d:/dotnet%20project/db/src/HRM.GUI/HRM.GUI.csproj#L1) | Cảnh báo lỗ hổng DoS theo GitHub Advisory (GHSA-hh2w-p6rv-4g7w, GHSA-8g4q-xg66-9fp4) | **`T04`** (Runtime & Deps) | `DependencyScan_NoHighSeverityAdvisories`<br>`SolutionBuild_CleanWithoutObsoleteFrameworkWarnings` | **LEGACY_CHƯA_SỬA** (Sẽ xử lý dứt điểm khi nâng .NET ở T04) |

---

## 3. Bản đồ Chuyển giao Chi tiết theo Sáu Nhóm Nghiệp vụ

### Nhóm 1: DTO Nhân viên & Vòng đời Hồ sơ (Employee Profile & Lifecycle)

- **Mã nguồn Legacy liên quan:**
  - `db/src/HRM.BLL/Services/NhanVienService.cs`
  - `db/src/HRM.Common/DTOs/NhanVienDTO.cs` (chỉ có `NhanVienDTO` và `NhanVienCreateDTO`, không có UpdateDTO)
  - `db/src/HRM.GUI/Forms/Main/NhanVien/frmSuaNhanVien.cs`
  - `db/src/HRM.GUI/Forms/Main/NhanVien/frmThemNhanVien.cs`
- **Use Case Backend Mới dự kiến:**
  - `POST /api/v1/employees` -> `CreateEmployeeCommand`
  - `GET /api/v1/employees` -> `GetEmployeesPagedQuery` (có filter, pagination, whitelist sort)
  - `GET /api/v1/employees/{id}` -> `GetEmployeeDetailQuery`
  - `PATCH /api/v1/employees/{id}` -> `UpdateEmployeeProfileCommand` (partial-update semantics an toàn)
  - `POST /api/v1/employees/{id}/offboard` -> `OffboardEmployeeCommand` (khóa tài khoản, thu hồi phiên)
- **Quy tắc Giữ lại (KEEP):**
  - Giữ lại các trường thông tin cơ bản: Họ tên, Ngày sinh, Giới tính, CCCD, Địa chỉ, Số điện thoại, Email, Tình trạng hôn nhân, Ngày vào làm.
  - Giữ lại cấu trúc liên kết Phòng ban (`MaPhongBan`) và Chức vụ (`MaChucVu`).
  - Giữ lại định dạng mã nhân viên tự sinh theo quyết định `OPEN-06`: `NV{yyyy}{NNNN}` (duy nhất, bất biến, sinh bằng sequence/lock an toàn).
- **Quy tắc Bỏ hoàn toàn (DROP):**
  - **BỎ dùng chung `NhanVienCreateDTO` cho thao tác cập nhật.**
  - **BỎ hành vi gán đè `null` khi trường không xuất hiện trên request payload (loại bỏ triệt để lỗi F10).**
  - **BỎ cơ chế tự sinh mật khẩu mặc định từ ngày sinh (DOB)** `dto.NgaySinh.ToString("dd/MM/yyyy")` (loại bỏ F13).
  - **BỎ lệnh gọi `DongBoBangLuongTheoNhanVienAsync` bên trong hàm cập nhật nhân viên** (loại bỏ F02). Việc thay đổi mức lương phải đi qua endpoint riêng `POST /api/v1/employees/{id}/salary-history` có quyền Payroll chuyên biệt.
- **Task phụ trách:** **`T08`** (Hồ sơ nhân viên và vòng đời tài khoản), **`T17`** (Web nhân sự).
- **Bộ test cần có:**
  - `F10_PartialUpdateCharacterizationTests` (đã tạo tại T03 để khoanh vùng hành vi cũ).
  - Integration test: `PATCH /employees/{id}` chỉ cập nhật `SoDienThoai`, xác nhận `DiaChi` và `TinhTrangHonNhan` trong DB được giữ nguyên vẹn 100%.
  - Unit test: Xác nhận DTO mới phân biệt được `omitted` (không gửi) vs `null` (chủ ý xóa).
  - Security test: Nhân viên không thể tự sửa mức lương hoặc trạng thái làm việc của chính mình (HTTP 403).

---

### Nhóm 2: Quản lý Tracking, Vòng đời DbContext và Concurrency (Tracking & DB Lifetime)

- **Mã nguồn Legacy liên quan:**
  - `db/src/HRM.DAL/Repositories/Repository.cs`
  - `db/src/HRM.DAL/Context/HrmDbContext.cs`
  - `db/src/HRM.GUI/Program.cs` (Đăng ký `AddScoped` nhưng dùng trực tiếp từ root `ServiceProvider`)
- **Use Case Backend Mới dự kiến:**
  - Quản lý vòng đời `HrmDbContext` theo phạm vi Scoped cho từng HTTP Request độc lập.
  - Quản lý giao dịch nguyên tử thông qua `IUnitOfWork` hoặc MediatR Pipeline Behavior.
  - Sử dụng Concurrency Token (`RowVersion` / `byte[]`) cho các thực thể dễ xung đột: `LeaveBalance`, `AttendanceDay`, `PayrollPeriod`.
- **Quy tắc Giữ lại (KEEP):**
  - Giữ lại mô hình EF Core Code-First với chuỗi Migration tường minh.
  - Giữ lại các chỉ mục tối ưu hóa truy vấn (`IX_ChamCong_MaNhanVien_NgayChamCong`, `IX_DonNghiPhep_MaNhanVien`).
- **Quy tắc Bỏ hoàn toàn (DROP):**
  - **BỎ việc gọi `_context.SaveChangesAsync()` tự động bên trong từng phương thức generic của Repository** (`Repository.AddAsync`, `UpdateAsync`, `DeleteAsync`). Việc lưu thay đổi phải do Use Case / Handler kiểm soát tập trung.
  - Không đưa entity đọc bằng `AsNoTracking()` sang luồng ghi chỉ gọi `SaveChanges` mà không attach/update. Checkout hiện tại không cấu hình NoTracking toàn cục; `Repository.UpdateAsync` đã gọi `DbSet.Update` rồi lưu. F11 trong audit cũ là đầu vào cần đối chiếu, không phải bằng chứng mọi đường cập nhật hiện tại đều hỏng.
  - **BỎ việc chia sẻ `DbContext` giữa nhiều luồng hoặc giữ instance lâu dài** trong các form WinForms (loại bỏ F15).
- **Task phụ trách:** **`T05`** (Dựng backend và hợp đồng API nền tảng), **`T25`** (Staging, pipeline và quản lý cấu hình).
- **Bộ test cần có:**
  - Đã có hai characterization test F11: entity detached + SaveChanges không lưu; cùng entity đó đi qua `NhanVienRepository.UpdateAsync` thì lưu. Chạy trên EF InMemory, không thay bằng chứng SQL Server cho transaction/concurrency.
  - Concurrency test: Gửi đồng thời 2 request sửa đổi cùng một bản ghi, xác nhận request thứ hai nhận lỗi `409 Conflict` (DbUpdateConcurrencyException).
  - Transaction rollback test: Giả lập lỗi ở bước cuối cùng của use case ghi nhiều bảng, xác nhận toàn bộ thao tác trước đó được rollback sạch sẽ.

---

### Nhóm 3: Tiền lương, Lịch sử Mức lương và Vòng đời Bảng lương (Payroll & Salary History)

- **Mã nguồn Legacy liên quan:**
  - `db/src/HRM.BLL/Services/BangLuongService.cs`
  - `db/src/HRM.BLL/Services/HieuSuatService.cs`
  - `db/src/HRM.Common/DTOs/BangLuongDTO.cs`
  - `db/src/HRM.Domain/Entities/BangLuong.cs`
- **Use Case Backend Mới dự kiến:**
  - `POST /api/v1/payroll-periods` -> `CreatePayrollPeriodCommand`
  - `POST /api/v1/payroll-periods/{id}/calculate` -> `CalculatePayrollDraftCommand`
  - `POST /api/v1/payroll-periods/{id}/approve` -> `ApprovePayrollCommand`
  - `POST /api/v1/payroll-periods/{id}/publish` -> `PublishPayrollCommand`
  - `GET /api/v1/payroll-periods/{id}/items` -> `GetPayrollItemsQuery`
  - `GET /api/v1/me/payslips` -> `GetMyPayslipsQuery`
  - `GET /api/v1/employees/{id}/salary-history` -> `GetSalaryHistoryQuery`
  - `POST /api/v1/employees/{id}/salary-history` -> `AddSalaryHistorySegmentCommand`
- **Quy tắc Giữ lại (KEEP):**
  - Giữ lại công thức chia lương ngày chuẩn: `Lương cơ bản / Số ngày công chuẩn theo lịch tháng`.
  - Giữ lại hệ số làm thêm giờ (OT) demo: Ngày thường `1.5`, Nghỉ cuối tuần `2.0`, Ngày nghỉ cấu hình `3.0` (theo quyết định `OPEN-08`).
  - Giữ lại đơn vị tiền tệ `decimal` và làm tròn số học `AwayFromZero` đến hàng đồng (theo `OPEN-09`).
- **Quy tắc Bỏ hoàn toàn (DROP):**
  - **BỎ HOÀN TOÀN luồng xóa kỳ cũ trước khi tính mới (`XoaTheoThangNamAsync`)** (loại bỏ triệt để nguy cơ mất dữ liệu F03). Backend mới áp dụng cơ chế `PayrollRun` có số phiên bản (Versioned / Snapshot), chạy tính lại sinh run mới, thất bại giữ nguyên bản cũ.
  - **BỎ logic tự động đồng bộ đè dữ liệu khi xem danh sách (`GetBangLuongAsync`)** (loại bỏ F02). Xem dữ liệu là thao tác Read-Only tuyệt đối không gây mutation.
  - **BỎ trừ phạt 10% khi chưa có điểm KPI (`HieuSuatService.TinhBonusHieuSuat`)** (loại bỏ F04). Theo quyết định `OPEN-03`, tắt hoàn toàn liên kết tự động giữa KPI và lương trong bản MVP.
  - **BỎ việc truyền cờ `isAdmin` từ client** để phân quyền xem lương (loại bỏ F09).
- **Task phụ trách:** **`T13`** (Công thức lương thuần, salary history và snapshot), **`T14`** (Vòng đời bảng lương nguyên tử và bất biến), **`T15`** (Phiếu lương, xuất CSV và đối soát).
- **Bộ test cần có:**
  - Test bất biến (Immutability): Bảng lương ở trạng thái `Published` và `Locked` cấm mọi thao tác ghi đè hoặc tính lại.
  - Test snapshot: Lịch sử lương thay đổi ở Tháng 5 không làm thay đổi phiếu lương Tháng 1.
  - Test pure function lương: Độc lập với DB, kiểm thử đầy đủ 10 kịch bản mẫu từ tài liệu `docs/business-rules.md`.

---

### Nhóm 4: Chấm công, Giờ máy chủ và Lịch làm việc (Attendance & Work Calendar)

- **Mã nguồn Legacy liên quan:**
  - `db/src/HRM.BLL/Services/ChamCongService.cs`
  - `db/src/HRM.Common/DTOs/ChamCongDTO.cs`
  - `db/src/HRM.Domain/Entities/ChamCong.cs`
- **Use Case Backend Mới dự kiến:**
  - `POST /api/v1/attendance/check-in` -> `RecordCheckInCommand` (Idempotency-Key)
  - `POST /api/v1/attendance/check-out` -> `RecordCheckOutCommand` (Idempotency-Key)
  - `GET /api/v1/me/attendance` -> `GetMyAttendanceHistoryQuery`
  - `GET /api/v1/attendance` -> `GetCompanyAttendanceQuery` (Quyền HR)
  - `POST /api/v1/attendance/{id}/corrections` -> `RequestAttendanceCorrectionCommand` (Lý do + Audit)
  - `POST /api/v1/overtime-entries` -> `SubmitOvertimeEntryCommand`
  - `POST /api/v1/overtime-entries/{id}/approve` -> `ApproveOvertimeEntryCommand`
- **Quy tắc Giữ lại (KEEP):**
  - Giữ lại ca làm việc demo: Thứ Hai – Thứ Sáu, `08:30 – 17:30`, nghỉ trưa `12:00 – 13:00` (theo `OPEN-04`).
  - Giữ lại phân loại trạng thái chấm công: "Bình thường", "Đi muộn", "Về sớm".
  - Giữ lại cột `Hwid` và các metadata phục vụ ghi nhận thiết bị từ migration T02.
- **Quy tắc Bỏ hoàn toàn (DROP):**
  - **BỎ việc lấy giờ chấm công từ máy trạm client (`DateTime.Now`, `DateTime.Today`)** (loại bỏ F12). Server tự sinh timestamp UTC và chuyển đổi theo múi giờ `Asia/Ho_Chi_Minh`.
  - **BỎ kiểm tra IP máy trạm bằng whitelist nội bộ cục bộ** trên client desktop.
  - **BỎ việc tự động tính giờ làm thêm (OT) từ hiệu số Check-out trừ Check-in**. Phút OT hợp lệ bắt buộc phải qua xác nhận phê duyệt độc lập của HR (`OPEN-08`).
  - **BỎ ghi đè HWID check-in khi thực hiện check-out**.
- **Task phụ trách:** **`T09`** (Lịch làm việc, ngày nghiệp vụ và giờ server), **`T10`** (Chấm công online và điều chỉnh có kiểm soát), **`P02`** (Chấm công nâng cao — Sau đồ án).
- **Bộ test cần có:**
  - Test giờ server: Giả lập client gửi timestamp sai lệch, server bắt buộc ghi nhận theo giờ hệ thống của server.
  - Test Idempotency: Gửi lặp request Check-in với cùng key chỉ sinh 1 bản ghi chấm công duy nhất.
  - Test chặn chấm công khi đã có đơn nghỉ phép `Approved` trong ngày.
  - Test chặn điều chỉnh chấm công đối với các kỳ lương đã khóa (`Published`/`Locked`).

---

### Nhóm 5: Nghỉ phép, Quản lý Quỹ phép và Giao dịch Duyệt (Leave Management)

- **Mã nguồn Legacy liên quan:**
  - `db/src/HRM.BLL/Services/DonNghiPhepService.cs`
  - `db/src/HRM.Common/DTOs/DonNghiPhepDTO.cs`
  - `db/src/HRM.Domain/Entities/DonNghiPhep.cs`, `SoNgayPhep.cs`
- **Use Case Backend Mới dự kiến:**
  - `POST /api/v1/leave-requests` -> `SubmitLeaveRequestCommand` (Kiểm tra trùng lịch, kiểm tra quỹ dự kiến)
  - `POST /api/v1/leave-requests/{id}/approve` -> `ApproveLeaveRequestCommand` (Transaction + Concurrency check)
  - `POST /api/v1/leave-requests/{id}/reject` -> `RejectLeaveRequestCommand` (Lý do từ chối bắt buộc)
  - `POST /api/v1/leave-requests/{id}/cancel` -> `CancelLeaveRequestCommand` (Chỉ chủ đơn, chỉ khi Pending)
  - `GET /api/v1/leave-balances` -> `GetLeaveBalancesQuery`
- **Quy tắc Giữ lại (KEEP):**
  - Giữ lại hạn mức quỹ phép demo 12 ngày/năm (theo `OPEN-05`).
  - Giữ lại các trạng thái vòng đời đơn: `Pending` (Chờ duyệt), `Approved` (Đã duyệt), `Rejected` (Từ chối), `Cancelled` (Đã hủy).
  - Giữ lại ràng buộc kiểm tra không trùng lặp khoảng thời gian giữa các đơn nghỉ phép.
- **Quy tắc Bỏ hoàn toàn (DROP):**
  - **BỎ hoàn toàn việc phê duyệt đơn mà không kiểm tra lại số dư quỹ phép** (loại bỏ triệt để lỗi F05).
  - **BỎ việc tách rời thao tác đổi trạng thái đơn và thao tác trừ quỹ phép thành 2 lệnh SaveChanges không transaction**.
  - **BỎ tính số ngày nghỉ bằng chênh lệch lịch thuần túy `(den - tu).TotalDays + 1`** (loại bỏ F16). Phép chỉ trừ các ngày làm việc thực tế theo lịch công ty `WorkCalendar`.
  - **BỎ cho phép người nộp đơn tự duyệt đơn của chính mình**.
- **Task phụ trách:** **`T11`** (Nghỉ phép với giao dịch và chống âm quỹ), **`T12`** (Bộ kiểm thử bất biến liên module).
- **Bộ test cần có:**
  - `F05_LeaveBalanceOverdrawnTests` (đã có từ T01).
  - Concurrency test: Hai quản trị viên duyệt đồng thời 2 đơn phép khác nhau của cùng 1 nhân viên khi quỹ chỉ còn đủ cho 1 đơn; hệ thống bắt buộc từ chối đơn thứ hai.
  - Test hủy đơn: Nhân viên không được phép tự hủy đơn đã ở trạng thái `Approved` (phải qua quy trình reversal có quyền HR).

---

### Nhóm 6: Tuyển dụng, Đào tạo và Module Tạm hoãn (Recruitment & Deferred Modules)

- **Mã nguồn Legacy liên quan:**
  - `db/src/HRM.BLL/Services/TinTuyenDungService.cs`
  - `db/src/HRM.BLL/Services/PhongVanService.cs`
  - `db/src/HRM.BLL/Services/UngVienService.cs`
  - `db/src/HRM.GUI/Forms/Main/UngVien/ucUngVien.cs` (chứa logic tự tạo nhân viên khi vòng PV >= 3)
  - `db/src/HRM.BLL/Services/GeminiService.cs` (gửi thông tin nhân viên ra ngoài)
- **Use Case Backend Mới dự kiến:**
  - **HOÃN TOÀN BỘ CÁC USE CASE TUYỂN DỤNG, ĐÀO TẠO VÀ CHATBOT TRONG PHẠM VI 8 TUẦN (OUT OF MVP SCOPE).**
  - Đưa tuyển dụng hoàn chỉnh vào nhiệm vụ sau đồ án **`P01`**.
  - Đưa chatbot kiểm soát dữ liệu cá nhân vào nhiệm vụ sau đồ án **`P07`**.
- **Quy tắc Giữ lại (KEEP):**
  - Giữ lại cấu trúc bảng và khóa ngoại trong cơ sở dữ liệu để phục vụ đối chiếu dữ liệu lịch sử và schema baseline.
  - Giữ lại seed data tuyển dụng mẫu làm tài liệu tham khảo.
- **Quy tắc Bỏ hoàn toàn (DROP):**
  - **BỎ logic tự động chuyển ứng viên thành nhân viên nằm trong mã giao diện WinForms** (`ucUngVien.cs:251`) (loại bỏ F17).
  - **BỎ việc gửi PII nhân viên (tên, chức vụ, phòng ban, lương) từ client WinForms lên dịch vụ AI bên ngoài** (`GeminiService.cs`) (loại bỏ F18).
  - Không xây dựng Controller / API mới cho Tuyển dụng trong các nhiệm vụ từ T05 đến T15.
- **Task phụ trách:** **`P01`** (Tuyển dụng và đào tạo hoàn chỉnh), **`P07`** (Trợ lý AI có giới hạn dữ liệu).
- **Bộ test cần có:**
  - Kiểm tra tính toàn vẹn khóa ngoại (Referential Integrity) giữa các bảng tuyển dụng và phòng ban để không gây lỗi khi chạy migration.

---

## 4. Ma trận Ánh xạ Tổng hợp Service Legacy -> Backend API Use Case

| STT | Service Legacy | Scope MVP 8 Tuần | Use Case Backend Mới Tương ứng | Endpoint Đề xuất | Task Xử lý |
| :---: | :--- | :---: | :--- | :--- | :---: |
| 1 | `AuthService` | **IN** | Xử lý đăng nhập Identity, cấp Token, thu hồi phiên | `POST /api/v1/auth/login`<br>`POST /api/v1/auth/refresh`<br>`POST /api/v1/auth/revoke` | `T06`, `T07` |
| 2 | `TaiKhoanService` | **IN** | Kích hoạt tài khoản bằng lời mời, đổi mật khẩu bản thân | `POST /api/v1/accounts/invite`<br>`POST /api/v1/me/change-password` | `T06`, `T08` |
| 3 | `NhanVienService` | **IN** | Quản lý hồ sơ nhân viên, cập nhật thông tin cá nhân, thôi việc | `GET/POST /api/v1/employees`<br>`PATCH /api/v1/employees/{id}`<br>`POST /api/v1/employees/{id}/offboard` | `T08`, `T17` |
| 4 | `PhongBanService` | **IN** | Danh mục phòng ban, cơ cấu tổ chức | `GET/POST /api/v1/departments`<br>`PATCH /api/v1/departments/{id}` | `T08`, `T17` |
| 5 | `ChamCongService` | **IN** | Chấm công server-time, tra cứu công, đề xuất điều chỉnh | `POST /api/v1/attendance/check-in,out`<br>`POST /api/v1/attendance/{id}/corrections` | `T09`, `T10` |
| 6 | `DonNghiPhepService` | **IN** | Nộp đơn phép, phê duyệt/từ chối/hủy, quản lý số dư quỹ | `POST /api/v1/leave-requests`<br>`POST /api/v1/leave-requests/{id}/approve,reject,cancel` | `T11`, `T12` |
| 7 | `BangLuongService` | **IN** | Vòng đời kỳ lương (Tính nháp, Phê duyệt, Công bố), Phiếu lương | `POST /api/v1/payroll-periods`<br>`POST /api/v1/payroll-periods/{id}/calculate,approve,publish` | `T13`, `T14`, `T15` |
| 8 | `HieuSuatService` | **THU HẸP** | Tắt liên kết tính tiền tự động trong MVP; chỉ lưu điểm đánh giá | `GET/POST /api/v1/performance-evaluations` | `T13` (Tắt), `P03` |
| 9 | `DashboardService` | **IN** | Thống kê tổng quan dành cho HR và Quản lý theo quyền | `GET /api/v1/dashboard/summary` | `T19` |
| 10 | `TinTuyenDungService`| **OUT** | Quản lý tin tuyển dụng (Hoãn sau đồ án) | Không tạo API trong MVP | `P01` |
| 11 | `PhongVanService` | **OUT** | Quản lý phỏng vấn và đánh giá ứng viên (Hoãn sau đồ án) | Không tạo API trong MVP | `P01` |
| 12 | `GeminiService` | **OUT** | Trợ lý hỏi đáp chính sách (Hoãn sau đồ án) | Không tạo API trong MVP | `P07` |

---

## 5. Kết luận và Ranh giới Kỹ thuật Bàn giao

1. **Về Mã nguồn Sản phẩm:** Toàn bộ mã nguồn legacy trong `db/src` được giữ nguyên vẹn 100%, không bị sửa đổi, refactor hay can thiệp trong nhiệm vụ T03.
2. **Về Cơ sở Dữ liệu:** Không có bất kỳ kết nối hay thao tác thay đổi nào được thực hiện trên cơ sở dữ liệu `HRM_System` cũ hoặc các database kiểm thử.
3. **Về Kế hoạch Thực hiện:**
   - Các lỗi được khoanh vùng ở đây sẽ được giải quyết dứt điểm theo đúng từng task chuyên trách từ T04 đến T15.
   - Tuyệt đối không giả lập rằng các lỗi đã được sửa chỉ bằng việc ban hành tài liệu này.
   - Mọi tiến trình phát triển tiếp theo phải bám sát bản đồ chuyển giao và bộ kiểm thử đã định nghĩa.
