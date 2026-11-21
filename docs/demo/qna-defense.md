# Bộ 10 Câu Hỏi & Trả Lời Phản Biện Bảo Vệ Đồ Án Tốt Nghiệp (Defense Q&A Guide)

Tài liệu này chuẩn bị câu trả lời chi tiết, có chiều sâu kỹ thuật, viện dẫn mã nguồn và số liệu kiểm thử thực tế cho **10 câu hỏi cốt lõi** mà Hội đồng chấm đồ án tốt nghiệp thường đặt ra cho đề tài HRM (theo định hướng tại [05_LO_TRINH_NGHIEM_THU.md](file:///d:/dotnet%20project/HRM_KeHoach_DoAn_8Tuan/05_LO_TRINH_NGHIEM_THU.md)).

---

## Câu 1: Vì sao hệ thống WinForms kế thừa (Legacy) không thể dùng trực tiếp làm backend cho Web và Mobile?

**Trả lời:**
Hệ thống WinForms cũ mắc phải 4 rào cản kiến trúc chí mạng khiến nó không thể tái sử dụng trực tiếp làm backend cho các nền tảng phân tán hiện đại:
1. **Liên kết chặt 2 tầng (Fat-Client / 2-Tier Architecture)**:
   - Ứng dụng WinForms cũ nhúng trực tiếp chuỗi kết nối CSDL và các câu lệnh SQL (ADO.NET / SqlDataReader) ngay trong các sự kiện giao diện (như `btnSave_Click`). Không hề có tầng API Service hay Business Logic độc lập để Web hoặc Mobile gọi tới.
2. **Nguy cơ An ninh & Phơi bày Mạng**:
   - Nếu Mobile hoặc Web giao tiếp trực tiếp với WinForms hoặc mở cổng SQL Server (Port 1433) ra Internet, ứng dụng sẽ để lộ thông tin xác thực CSDL (Database Credentials) qua việc dịch ngược (Decompile) mã nhị phân, đồng thời tạo nguy cơ tấn công từ chối dịch vụ (DDoS) và SQL Injection trực tiếp vào lõi dữ liệu.
3. **Mô hình Trạng thái Kết nối (Stateful Connections)**:
   - WinForms giữ kết nối TCP kéo dài (Long-lived Database Connection per User). Mô hình này hoàn toàn không thể mở rộng (Cannot Scale) khi có hàng trăm hoặc hàng ngàn thiết bị di động truy cập đồng thời qua môi trường mạng không ổn định (Stateless HTTP/REST).
4. **Thiếu vắng Cơ chế Phân quyền Tinh gọn (Fine-grained Scoping & Tokens)**:
   - WinForms quản lý phiên theo tiến trình máy tính để bàn (Desktop Process Identity), không có khái niệm JSON Web Token (JWT), Anti-CSRF Token hay cơ chế thu hồi phiên tức thời (`SecurityStamp`) bắt buộc cho môi trường Web/Mobile đa người dùng.

---

## Câu 2: Trong kiến trúc Clean Architecture, 4 tầng Domain, Application, Infrastructure và API khác biệt nhau ở những trách nhiệm nào?

**Trả lời:**
Kiến trúc tuân thủ nghiêm ngặt nguyên lý đảo ngược phụ thuộc (Dependency Inversion Principle - DIP), với ranh giới trách nhiệm rõ rệt:

| Tầng kiến trúc | Bản chất công nghệ | Trách nhiệm cốt lõi | Quy tắc phụ thuộc |
|---|---|---|---|
| **HRM.Domain** | C# thuần túy (.NET 10), Zero Dependencies | Chứa các thực thể miền (Entities), giá trị đối tượng (Value Objects) và bảo vệ các **bất biến nghiệp vụ tuyệt đối** (Business Invariants) như: Quỹ phép không được âm, Bảng lương đã khóa là bất biến. | Hoàn toàn độc lập, không phụ thuộc vào bất kỳ tầng nào hay thư viện ngoài nào. |
| **HRM.Application** | Thư viện C#, dùng MediatR / FluentValidation | Đóng gói toàn bộ các kịch bản nghiệp vụ (Use Cases, CQRS Commands/Queries), điều phối luồng dữ liệu, kiểm tra hợp lệ dữ liệu đầu vào và phát sinh sự kiện miền. | Chỉ phụ thuộc vào `HRM.Domain`. Định nghĩa các giao diện trừu tượng (Ports như `IUnitOfWork`, `IEmailSender`). |
| **HRM.Infrastructure** | EF Core, SQL Server Client, SMTP | Hiện thực hóa các giao diện kỹ thuật (Adapters): Cấu hình ánh xạ bảng CSDL, Transactional Outbox, gửi email qua SMTP, thuật toán mã hóa mật khẩu, ghi nhật ký an ninh. | Phụ thuộc vào `HRM.Application` và `HRM.Domain`. Chứa các chi tiết phụ thuộc công nghệ. |
| **HRM.Api** | ASP.NET Core, Kestrel Web Server | Điểm khởi đầu ứng dụng (Entrypoint): Tiếp nhận HTTP request, định tuyến Controllers, cấu hình Dependency Injection (IoC Container), chuỗi Middlewares an ninh (RateLimiting, SecurityStamp, Anti-CSRF). | Phụ thuộc vào `HRM.Application` và `HRM.Infrastructure` để ráp nối toàn bộ hệ thống lúc khởi động. |

---

## Câu 3: Cơ chế xác thực Cookie trên Web và Bearer Token trên Mobile được lưu trữ, kiểm tra và thu hồi như thế nào?

**Trả lời:**
Hệ thống sử dụng mô hình **Xác thực Lai (Hybrid Authentication Architecture)** được thiết kế riêng biệt cho từng nền tảng:
1. **Web Portal (Trình duyệt)**:
   - **Lưu trữ**: Lưu trong Cookie với các cờ bảo vệ tối đa: `HttpOnly` (JavaScript không thể đọc trộm qua XSS), `SameSite=Strict` (chỉ gửi trong cùng nguồn gốc), và `Secure` (bắt buộc truyền qua HTTPS).
   - **Chống CSRF**: Bắt buộc mọi yêu cầu thay đổi dữ liệu (`POST`, `PUT`, `DELETE`) phải gửi kèm header `X-XSRF-TOKEN` khớp với giá trị Anti-CSRF Cookie đã mã hóa.
2. **Mobile App (Thiết bị di động)**:
   - **Lưu trữ**: Token không lưu trong LocalStorage hay Shared Preferences thông thường, mà được lưu trong vùng nhớ mã hóa an toàn của hệ điều hành (`expo-secure-store` tương ứng với Android KeyStore và iOS Keychain).
   - **Vòng đời Token**: Access Token mang thời hạn sống ngắn (15 phút), đi kèm Refresh Token quay vòng (Rotating Refresh Token) được lưu trữ bản băm (Hash) trong CSDL.
3. **Cơ chế Thu hồi Phiên Tức thời (`SecurityStamp`)**:
   - Mỗi tài khoản người dùng mang một trường `SecurityStamp` (GUID) trong bảng `HrmUsers`. Giá trị này được nhúng làm một Claim trong Token JWT.
   - Khi người dùng đổi mật khẩu, đăng xuất tất cả thiết bị, hoặc HR khóa tài khoản, hệ thống cập nhật `SecurityStamp = NEWID()` trong CSDL.
   - Middleware `SecurityStampValidationMiddleware` trên Backend kiểm tra Claim này với cơ sở dữ liệu trên mỗi request. Nếu phát hiện không trùng khớp, hệ thống trả về mã **401 Unauthorized** ngay lập tức.
   - **SLA đo được**: Thời gian vô hiệu hóa phiên trên toàn hệ thống đạt $\le 0.05$ giây (đã kiểm chứng qua 49 bài test Jest của Mobile và 419 integration tests của Backend).

---

## Câu 4: Vì sao việc "ẩn nút bấm trên giao diện người dùng" là hoàn toàn chưa đủ để ngăn chặn việc đọc lén phiếu lương của người khác (Lỗ hổng IDOR)?

**Trả lời:**
- **Bản chất của Frontend**: Giao diện người dùng (React Web hay Mobile App) chạy trên thiết bị của khách hàng và hoàn toàn nằm dưới quyền kiểm soát của người dùng. Kẻ tấn công hoặc người dùng tò mò có thể dễ dàng mở DevTools (F12), dịch ngược file APK/Bundle, hoặc sử dụng công cụ như Postman, Burp Suite để gửi trực tiếp yêu cầu HTTP tới máy chủ mà không cần thông qua giao diện có nút bấm.
- **Lỗ hổng IDOR (Insecure Direct Object References)**: Nếu Backend chỉ đơn thuần lấy `id` từ tham số URL kiểu `GET /api/v1/payroll/payslips/{id}` và truy vấn thẳng CSDL mà không kiểm tra chủ quyền sở hữu, kẻ tấn công chỉ cần thay đổi số ID (ví dụ từ `1001` sang `1002`) là có thể xem toàn bộ thu nhập của người khác.
- **Giải pháp Phòng vệ Chiều sâu trong Hệ thống**:
  1. **Record Scoping**: Mọi endpoint dành cho nhân viên tự phục vụ đều sử dụng định tuyến ẩn danh danh tính `/me` (ví dụ: `GET /api/v1/payroll/me/payslips`). Backend tự trích xuất `UserId` từ Token đã xác thực một cách an toàn.
  2. **Authorization Enforcement tại Application Layer**: Khi truy vấn một tài nguyên cụ thể theo `Id`, hệ thống bắt buộc kiểm tra điều kiện quyền:
     ```csharp
     if (!currentUser.IsInRole("HRManager", "PayrollSpecialist") && payslip.EmployeeId != currentUser.EmployeeId)
     {
         return Result.Failure(CommonErrors.Forbidden("Bạn không có quyền truy cập phiếu lương này."));
     }
     ```
  - Cơ chế này đã được chứng minh qua bài kiểm thử an ninh tự động trong suite `T24`, giả lập 10 trường hợp hoán đổi URL ID và 100% đều bị chặn với mã `403 Forbidden` / `404 Not Found`.

---

## Câu 5: Khi hai Quản lý HR cùng bấm duyệt đơn nghỉ phép tại cùng một thời điểm, transaction và ràng buộc nào ngăn chặn việc quỹ phép bị âm?

**Trả lời:**
Hệ thống kết hợp **4 tầng khóa và ràng buộc liên hoàn** giữa Tầng Ứng dụng và Cơ sở Dữ liệu:
1. **Giao dịch Nguyên tử (Isolation Level ReadCommitted)**: Toàn bộ quá trình đọc số dư, ghi giảm ngày phép, tạo dòng sổ cái và đổi trạng thái đơn nghỉ phép được bọc trọn vẹn trong một `IDbContextTransaction`.
2. **Khóa Bi quan tại Mức hàng (`UPDLOCK, ROWLOCK`)**:
   - Khi HR 1 thực thi truy vấn nạp số dư ngày phép, EF Core gửi gợi ý khóa tới SQL Server:
     ```sql
     SELECT * FROM LeaveBalances WITH (UPDLOCK, ROWLOCK) WHERE EmployeeId = @empId AND Year = @year;
     ```
   - Gợi ý này cấp một khóa cập nhật độc quyền trên hàng dữ liệu của nhân viên đó. Khi HR 2 gửi lệnh duyệt gần như đồng thời, giao dịch của HR 2 sẽ bị chặn (Lock Wait) cho đến khi HR 1 commit hoặc rollback.
3. **Khóa Lạc quan với Thẻ Phiên bản (`RowVersion`)**:
   - Thực thể `LeaveBalance` mang cột `RowVersion` kiểu `rowversion/timestamp`. Nếu có xung đột cạnh tranh bỏ qua được khóa, việc cập nhật của giao dịch thứ hai sẽ gây ra lỗi `DbUpdateConcurrencyException`, trả về mã **409 Conflict** cho trình duyệt thứ hai.
4. **Ràng buộc Kiểm tra Toàn vẹn CSDL (Database Check Constraint)**:
   - Tại mức CSDL, bảng `LeaveBalances` được thiết lập ràng buộc vật lý không thể phá vỡ:
     ```sql
     ALTER TABLE LeaveBalances ADD CONSTRAINT CK_LeaveBalances_RemainingDays CHECK (RemainingDays >= 0);
     ```
   - Dù mã nguồn ứng dụng có gặp lỗi logic thì SQL Server cũng sẽ từ chối giao dịch (Throw Exception) và kích hoạt Rollback toàn bộ, bảo đảm **quỹ phép tuyệt đối không bao giờ âm**.

---

## Câu 6: Làm thế nào hệ thống phân biệt được giữa việc "người dùng bấm gửi lại (retry) sau khi bị timeout" với một "yêu cầu tạo sự kiện chấm công mới"?

**Trả lời:**
Sự phân biệt được bảo đảm tuyệt đối thông qua cơ chế **Idempotency Key kết hợp Mốc thời gian Máy chủ**:
1. **Khách hàng sinh Khóa Định danh Duy nhất (`Idempotency-Key`)**:
   - Mỗi khi nhân viên nhấn nút "Check-in", Mobile App tự sinh một chuỗi UUID ngẫu nhiên gắn với hành động đó và gửi trong Header HTTP:
     `Idempotency-Key: IDEM-ATT-20261119-a1b2c3d4`
2. **Chỉ mục Duy nhất tại Cơ sở Dữ liệu**:
   - Bảng `AttendanceEvents` có chỉ mục duy nhất:
     ```sql
     CREATE UNIQUE INDEX IX_AttendanceEvents_IdempotencyKey ON AttendanceEvents(IdempotencyKey);
     ```
3. **Quy trình Xử lý Tránh Trùng Lặp**:
   - **Trường hợp gửi lần đầu**: Hệ thống lưu sự kiện với giờ nhận tại máy chủ (`ServerReceivedAtUtc`), commit thành công.
   - **Trường hợp Timeout sau khi đã commit**: Điện thoại chưa nhận được phản hồi do mất sóng và tự động thử gửi lại (Retry) với **cùng Idempotency-Key**.
   - Khi request thứ hai tới, câu lệnh INSERT sẽ vi phạm chỉ mục duy nhất. Backend bắt ngoại lệ `DbUpdateException` (SqlError 2601/2627), tự động chuyển hướng truy vấn bản ghi đã tồn tại mang khóa này và trả về kết quả thành công ban đầu kèm mã `200 OK`.
   - **Trường hợp tạo sự kiện mới**: Một lần bấm check-in/out khác trong ngày sẽ mang một `Idempotency-Key` mới hoàn toàn, do đó được ghi nhận thành bản ghi thứ hai bình thường.

---

## Câu 7: Việc thay đổi mức lương cơ bản giữa tháng được xử lý thế nào, và vì sao các kỳ lương đã khóa trong quá khứ không bị ảnh hưởng?

**Trả lời:**
1. **Lịch sử Lương Chỉ Ghi Thêm (Append-Only Salary History)**:
   - Hệ thống không bao giờ ghi đè trực tiếp lên cột lương trong bảng hồ sơ. Mọi điều chỉnh lương đều tạo một bản ghi mới trong bảng `SalaryHistories` gồm: `BaseSalary`, `EffectiveDate` (Ngày có hiệu lực), và `CreatedAtUtc`.
2. **Thuật toán Xác định Mức lương theo Ngày Chốt (Cut-Off Date)**:
   - Khi động cơ tính lương chạy cho Kỳ Tháng $M$, hệ thống truy vấn mức lương cơ bản có hiệu lực tính đến ngày chốt sổ (`CutOffDate`):
     ```sql
     SELECT TOP 1 BaseSalary FROM SalaryHistories
     WHERE EmployeeId = @empId AND EffectiveDate <= @cutOffDate
     ORDER BY EffectiveDate DESC, CreatedAtUtc DESC;
     ```
3. **Bản chụp Bất biến (Immutable Payroll Calculation Snapshot)**:
   - Kết quả tính toán không lưu dưới dạng công thức động mà được đóng băng vào bảng `PayrollCalculationSnapshots` (chụp lại: Lương cơ bản snapshot, Giờ công snapshot, Giờ làm thêm snapshot, Thuế/Bảo hiểm snapshot).
   - Khi kỳ lương được Giám đốc phê duyệt và công bố, cờ `IsLocked` được bật sang `true`.
   - Mọi truy vấn xem phiếu lương trên Web hay Mobile đều đọc trực tiếp từ bảng Snapshot này. Do đó, việc nhân viên được tăng lương vào ngày 15 của tháng sau hoàn toàn không làm thay đổi phiếu lương của tháng trước. Nếu có sai sót cần điều chỉnh, quy trình bắt buộc phải tạo bút toán điều chỉnh hồi tố (Retroactive Adjustment) vào kỳ lương mở kế tiếp.

---

## Câu 8: Việc kiểm thử bằng Fake Repository / In-Memory Mock khác biệt thế nào so với kiểm thử trực tiếp trên Microsoft SQL Server thật?

**Trả lời:**
Sự khác biệt mang tính sống còn đối với các hệ thống tài chính và phân quyền:
- **Hạn chế của Fake Repository / In-Memory Mock**:
  - Không thể kiểm tra được **hành vi giao dịch thực tế** (`BEGIN TRANSACTION`, `COMMIT`, `ROLLBACK`).
  - Không kiểm tra được **khóa cạnh tranh** (`UPDLOCK`, `ROWLOCK`, Deadlock, Lost Updates).
  - Không kích hoạt được các **ràng buộc cơ sở dữ liệu** (`FOREIGN KEY`, `CHECK Constraints`, `UNIQUE Index`).
  - Không phản ánh đúng cơ chế dịch truy vấn LINQ sang SQL (nhiều câu lệnh chạy được trên bộ nhớ nhưng nổ lỗi khi EF Core dịch sang T-SQL).
- **Chiến lược Kiểm thử của Đề tài trên SQL Server Thật (`HRM.Tests.Integration`)**:
  - Toàn bộ **419 integration tests** của hệ thống chạy trực tiếp trên máy chủ **Microsoft SQL Server 2022 thật**.
  - Sử dụng cơ chế `SqlDatabaseFixture` tạo các cơ sở dữ liệu vật lý riêng biệt (`HRM_Next_Test_T05_{guid}`) cho từng lượt chạy kiểm thử và áp dụng 100% Migration thật.
  - Bằng chứng thực tế: Các bài test kiểm tra quỹ phép không âm, kiểm tra idempotency của chấm công và kiểm tra xung đột phiên bản `RowVersion` chỉ có thể phát hiện lỗi chính xác khi thực thi trên công cụ lưu trữ thật của SQL Server.

---

## Câu 9: Việc phục hồi cơ sở dữ liệu (Restore Data) khác biệt như thế nào so với việc hoàn tác mã nguồn (Rollback Source Code)?

**Trả lời:**
Đây là hai khái niệm vận hành hoàn toàn khác nhau ở hai tầng kiến trúc riêng biệt:
1. **Hoàn tác Mã nguồn (Rollback Source Code / Git Revert / Container Rollback)**:
   - Là hành động đưa phiên bản ứng dụng chạy (Application Code / Docker Image / Kestrel Binary) trở về phiên bản biên dịch trước đó khi phát hiện lỗi logic phần mềm mới triển khai.
   - **Tác động**: Chỉ thay đổi các tệp nhị phân và logic điều phối; **hoàn toàn không làm thay đổi trạng thái của dữ liệu** đang nằm trong cơ sở dữ liệu.
2. **Phục hồi Cơ sở Dữ liệu (Restore Database / Point-in-Time Recovery)**:
   - Là quy trình nạp lại tệp sao lưu (`.bak`) của cơ sở dữ liệu vào SQL Server để đưa toàn bộ bảng biểu, bản ghi giao dịch, số dư tài chính về trạng thái tại thời điểm tạo bản sao lưu.
   - **Mục đích**: Khắc phục thảm họa mất mát dữ liệu do phần cứng hư hỏng, tấn công mã hóa dữ liệu (Ransomware), hoặc sự cố ghi sai dữ liệu hàng loạt.
3. **Quy tắc phối hợp khi triển khai Staging/Production**:
   - Khi phát hành phiên bản mới có chứa Database Migration, trước tiên bắt buộc phải tạo bản sao lưu dữ liệu tĩnh (`backup.py`). Nếu bản phát hành gặp sự cố nghiêm trọng, đội ngũ vận hành phải kích hoạt quy trình kép: Vừa Rollback Docker Container về phiên bản cũ, vừa Restore cơ sở dữ liệu về snapshot sạch đã lưu trước khi nâng cấp.
   - Đề tài đã diễn tập thành công năng lực này với thời gian phục hồi CSDL đạt **0.96 giây**, đáp ứng mục tiêu RTO $\le 4$ giờ và RPO $\le 24$ giờ của đồ án.

---

## Câu 10: Những tính năng nào của hệ thống đã hoàn thành, tính năng nào đã chủ động cắt giảm/hoãn lại, AI đã hỗ trợ phần nào và bằng chứng nghiệm thu ở đâu?

**Trả lời:**
Báo cáo minh bạch và trung thực theo đúng chuẩn mực học thuật:
1. **Các tính năng đã hoàn thành 100% (Core MVP Đã Đóng Băng)**:
   - Toàn bộ 4 phân hệ cốt lõi: Hồ sơ nhân sự (Profile & Lifecycle), Chấm công (Attendance & Idempotency), Nghỉ phép (Leave & Concurrency Ledger), Tiền lương (Payroll Calculation & Immutable Snapshot).
   - Kiến trúc an ninh phòng vệ chiều sâu: Xác thực lai Cookie/Bearer, thu hồi phiên tức thời `SecurityStamp`, Transactional Outbox, chống IDOR và mã hóa dữ liệu.
   - Bộ kiểm thử tự động 549/549 tests PASS (419 Backend, 48 Web Vitest, 6 Playwright E2E, 49 Mobile Jest, 17 Migration & Rehearsal, 8 Fake SMTP Outbox, 2 Contract checks).
2. **Các tính năng đã chủ động hoãn lại ngoài phạm vi MVP (Deferred Scope)**:
   - **Ca kíp xuyên đêm (Overnight Shifts)**: Đã công bố hoãn lại tại tuần 3 do phức tạp trong việc xác định ngày công ranh giới giữa 23:00 và 03:00 sáng.
   - **Biểu thuế thu nhập cá nhân lũy tiến từng phần**: Đã cấu hình mức trích nộp bảo hiểm cố định, hoãn biểu thuế phức tạp nhiều bậc sang giai đoạn nâng cấp tiếp theo.
   - **Ứng dụng iOS**: Ưu tiên tối đa cho ứng dụng Android trên Expo, không hứa hẹn phát hành iOS trong đợt bảo vệ.
3. **Vai trò Hỗ trợ của Trí tuệ Nhân tạo (AI Contribution Transparency)**:
   - AI đóng vai trò như một kỹ sư lập trình trợ lý: Hỗ trợ sinh khung mã nguồn (Boilerplate code), cấu hình Dockerfile, viết tài liệu kỹ thuật và xây dựng các ca kiểm thử tự động (Test fixtures).
   - Tác giả đề tài (Sinh viên) trực tiếp: Thiết kế kiến trúc tổng thể, định nghĩa các ràng buộc và quy tắc nghiệp vụ bất biến, rà soát logic an ninh (Security Code Review), thiết lập môi trường CSDL SQL Server thật và chạy kiểm chứng toàn bộ 549 bài kiểm thử.
4. **Vị trí Lưu trữ Bằng chứng Nghiệm thu (Verification Evidence)**:
   - Báo cáo Kiểm thử Toàn diện & Ma trận An ninh: [T24-e2e-security-report.md](file:///d:/dotnet%20project/docs/verification/T24-e2e-security-report.md).
   - Báo cáo Đối soát Di chuyển Dữ liệu 1,000 bản ghi legacy: [T23-rehearsal-evidence.md](file:///d:/dotnet%20project/docs/verification/T23-rehearsal-evidence.md).
   - Báo cáo Thử nghiệm Tải và Phục hồi Thảm họa: [t26-performance-report.md](file:///d:/dotnet%20project/docs/benchmarks/t26-performance-report.md).
   - Biên bản Kiểm thử Chấp nhận Người dùng (UAT): [uat-protocol.md](file:///d:/dotnet%20project/docs/acceptance/uat-protocol.md).
   - Quyết định Đóng băng Phiên bản: [release-freeze-v1.0.0.md](file:///d:/dotnet%20project/docs/releases/release-freeze-v1.0.0.md).

---

## Liên Kết Tài Liệu Liên Quan

- [Kịch Bản 5 Luồng Demo Trực Tiếp](file:///d:/dotnet%20project/docs/demo/demo-scenarios.md)
- [Hướng Dẫn Reset Môi Trường Demo](file:///d:/dotnet%20project/docs/demo/demo-reset-guide.md)
- [Báo Cáo Nghiệm Thu T24](file:///d:/dotnet%20project/docs/verification/T24-e2e-security-report.md)
