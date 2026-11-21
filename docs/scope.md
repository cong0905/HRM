# Phạm vi dự án Hệ thống Quản lý Nhân sự (HRM) — Kế hoạch 8 tuần

> **Tài liệu xác định phạm vi thực hiện đồ án tốt nghiệp**  
> **Phiên bản:** 1.2 (Chốt quyết định T00) — Ngày lập: 10/11/2026  
> **Chủ dự án / Người kiểm duyệt:** Sinh viên thực hiện đồ án  
> **Đơn vị hỗ trợ code:** AI Coding Assistant (Antigravity)  
> **Mục tiêu:** Bản nghiệm thu đồ án tốt nghiệp kỹ sư CNTT (Web + Mobile Android + Backend API)  
> **Trạng thái:** `READY_FOR_REVIEW` (Đã cập nhật đầy đủ các quyết định chốt từ chủ dự án; chờ nghiệm thu tài liệu T00)

---

## 1. Mục tiêu đồ án trong 8 tuần

### 1.1. Bối cảnh và thời hạn
- **Thời gian thực hiện:** Khoảng 8 tuần (từ ngày 09/11/2026 đến đầu tháng 01/2027).
- **Mô hình phối hợp:** Một người kiểm duyệt duy nhất (chủ dự án trực tiếp kiểm tra, chạy thử nghiệm thu); AI hỗ trợ sinh mã nguồn, đề xuất kiểm thử và soạn thảo tài liệu. Mọi quyết định kỹ thuật và nghiệp vụ đều do chủ dự án phê duyệt.
- **Nền tảng công nghệ đã chốt cho bản mới:**
  - **Backend mới:** ASP.NET Core trên nền **.NET 10 LTS** (C#), kiến trúc Modular Monolith. Mã nguồn .NET 8 hiện tại được giữ nguyên làm baseline legacy tham chiếu; việc chuẩn bị môi trường SDK sẽ thực hiện theo từng task, không tự ý nâng cấp toàn bộ code legacy trong T00.
  - **Cơ sở dữ liệu:** SQL Server. Sử dụng database phát triển riêng **`HRM_Next_Dev`** và database test riêng **`HRM_Next_Test`**; giữ nguyên `HRM_System` cũ làm đối chiếu. Chưa tạo DB hoặc chạy migration trong T00.
  - **Web Frontend:** React, TypeScript, Vite.
  - **Mobile App:** **React Native**, TypeScript. Nghiệm thu trên nền tảng **Android** trước; iOS tạm hoãn sau đồ án. Phiên bản Android tối thiểu sẽ căn cứ theo độ tương thích của React Native/Expo được chọn và thiết bị/emulator thực tế khi cấu hình ở Task T20.

### 1.2. Mục tiêu kỹ thuật cốt lõi
1. **Xây dựng một Backend API tập trung duy nhất** phục vụ đồng thời cho cả Web SPA và Mobile App, thay thế kiến trúc Desktop WinForms cũ vốn gọi trực tiếp Database trong cùng tiến trình.
2. **Khép kín chuỗi nghiệp vụ nhân sự trọng tâm:** Hồ sơ nhân sự $\rightarrow$ Lịch làm việc $\rightarrow$ Chấm công $\rightarrow$ Đơn nghỉ phép $\rightarrow$ Tính toán, phê duyệt, công bố và khóa bảng lương.
3. **Bảo mật và toàn vẹn dữ liệu từ phía Server:** Kiểm soát quyền hạn (RBAC), phạm vi dữ liệu theo bản ghi (Data Scoping), transaction chống âm quỹ phép khi có xung đột đồng thời, giờ chấm công lấy từ máy chủ, và đóng băng bảng lương lịch sử (Immutability).
4. **Bộ kiểm thử tự động và tài liệu hoàn chỉnh:** Bộ kiểm thử hồi quy (xUnit cho Backend, Integration test trên SQL Server thật, Component/E2E test), hồ sơ kiến trúc ADR và kịch bản demo bảo vệ có video dự phòng.

---

## 2. Phân định phạm vi chức năng (In-Scope vs. Deferred)

### 2.1. Chức năng trong phạm vi 8 tuần (IN-SCOPE)

| Phân hệ | Nghiệp vụ chi tiết | Nền tảng hỗ trợ | Ghi chú kỹ thuật |
| :--- | :--- | :--- | :--- |
| **Xác thực & Quản trị phiên** | Đăng nhập, đăng xuất, đổi mật khẩu, quên mật khẩu (reset token), khóa tài khoản. Dùng lời mời kích hoạt có thời hạn để nhân viên tự đặt mật khẩu (không có mật khẩu mặc định, không gửi mật khẩu thô qua email, HR không thấy mật khẩu). Tài khoản chưa kích hoạt bị chặn đăng nhập. Thu hồi phiên trực tiếp phía server cho request nghiệp vụ đã xác thực. | Web & Mobile | Dùng ASP.NET Core Identity. Web dùng Cookie HttpOnly + Anti-CSRF; Mobile dùng Bearer Access Token (hạn ngắn) + Refresh Token lưu trữ trong Secure Storage. Phân định rõ tiêu chí test riêng cho Cookie, Access Token và Refresh Token. |
| **Hồ sơ Nhân sự & Tổ chức** | Quản lý cơ cấu phòng ban, chức vụ; thêm/sửa hồ sơ nhân viên; quy trình thôi việc (offboarding) gắn liền với tự động khóa tài khoản và thu hồi quyền truy cập. Mã nhân viên tự sinh `NV{yyyy}{NNNN}` an toàn đồng thời, duy nhất và bất biến. Nhân viên tự xem hồ sơ cá nhân. | Web (đầy đủ) & Mobile (chỉ xem hồ sơ bản thân) | DTO cập nhật riêng biệt, tránh ghi đè làm mất trường thông tin địa chỉ/tình trạng hôn nhân. Phân quyền chặt chẽ: Chỉ tài khoản có quyền `ManageSalaryHistory` mới được quản lý mức lương. |
| **Lịch làm việc chuẩn** | Cấu hình lịch làm việc công ty theo ca ngày cố định (Thứ 2 – Thứ 6: 08:30 – 17:30, nghỉ trưa 12:00 – 13:00; ngày nghỉ lấy từ lịch cấu hình có version); nhân viên xem lịch làm việc. | Web (cấu hình & xem) & Mobile (nhân viên xem) | Múi giờ chuẩn `Asia/Ho_Chi_Minh` (UTC+7). Mọi timestamp lưu UTC ở database. Ca ngày cố định ở bản MVP. Phân biệt rõ thời gian hiện diện với thời gian đủ điều kiện hưởng công chuẩn. |
| **Chấm công Online** | Nhân viên check-in/check-out online; hệ thống tự động ghi nhận giờ server; tự động tính số phút đi muộn / về sớm; tổng hợp công ngày; xử lý quên checkout; HR tra cứu và điều chỉnh công có lý do & audit log. | Web (HR tra cứu/điều chỉnh) & Mobile (nhân viên check-in/out, xem lịch sử) | Server time bắt buộc. Idempotency-Key phân phạm vi theo UserId + OperationType; retry cùng thao tác phải dùng lại key; payload khác cùng key bị từ chối 422/409. Giờ ngoài ca không bù cho đi muộn/về sớm và không tự thành OT. Cấm sửa công của kỳ đã công bố. |
| **Nghỉ phép & Quỹ phép** | Quản lý danh mục loại phép; cấp quỹ phép năm demo 12 ngày/năm (cấp 1 lần khi khởi tạo quỹ năm hoặc khi nhân viên mới vào trong năm, không tích lũy theo tháng); nhân viên tạo đơn nghỉ phép nguyên ngày, xem trạng thái, hủy đơn đang chờ (`Pending`); HR duyệt/từ chối đơn nghỉ; hệ thống tự động trừ quỹ phép theo ngày làm việc thực tế. | Web (HR duyệt, nhân viên tạo/hủy) & Mobile (nhân viên tạo/hủy/xem quỹ) | Transaction + Optimistic Concurrency (RowVersion) chống âm quỹ phép khi duyệt đồng thời. Đếm ngày làm việc, không trừ ngày nghỉ cuối tuần/lễ. Đơn vắt qua năm bị từ chối. Hủy đơn đã `Approved` nằm ngoài MVP. Cấm sửa phép của kỳ đã công bố. |
| **Quản lý Tiền lương** | Quản lý lịch sử mức lương nhân viên theo ngày hiệu lực (`SalaryHistory`); tính toán bảng lương nháp theo tháng; phân đoạn lương khi đổi lương giữa tháng; tích hợp công chuẩn và phút làm thêm giờ (OT) được HR duyệt; các khoản phụ cấp và khấu trừ nhập tay có nhãn demo; quy trình duyệt $\rightarrow$ công bố $\rightarrow$ khóa kỳ lương; xuất bảng lương ra CSV an toàn; nhân viên xem phiếu lương cá nhân. | Web (Payroll tính/duyệt/xuất CSV) & Mobile (nhân viên xem phiếu lương đã công bố) | Chọn nhân viên có thời gian làm việc giao với kỳ lương. Tắt liên kết tự động KPI–lương. Bảng lương `Published` và `Locked` là bất biến (nhân viên vẫn xem được sau khi khóa). Kiểm tra version đầu vào ở cả Approve và Publish. Net âm bị chặn duyệt để xử lý tại kỳ (không tự động chuyển nợ sang kỳ sau). Các khoảng hiệu lực SalaryHistory không được chồng lấn; cấm sửa đổi làm biến động kỳ đã công bố. |
| **Dashboard & Báo cáo** | Thống kê số lượng nhân sự, tỷ lệ đi làm trong ngày, số đơn nghỉ chờ duyệt, tổng chi lương kỳ gần nhất; tóm tắt chỉ số cá nhân của nhân viên. | Web (Dashboard quản trị) & Mobile (Widget tóm tắt cá nhân) | Lấy trực tiếp từ API tổng hợp thật, không dùng dữ liệu giả lập (mock). |

### 2.2. Chức năng hoãn lại sau đồ án (DEFERRED / OUT OF SCOPE)

Các tính năng sau đây được **chủ động loại bỏ khỏi phạm vi 8 tuần** để tập trung tài nguyên vào chất lượng kỹ thuật lõi:

1. **Kiến trúc Microservices & Đa doanh nghiệp (Multi-tenant SaaS):** Giữ mô hình Modular Monolith phục vụ một doanh nghiệp duy nhất; không làm cơ chế phân tách tenant phức tạp.
2. **Chấm công sinh trắc học & GPS chống gian lận:** Hoãn nhận diện khuôn mặt (Face Recognition) bằng AI và định vị GPS chống fake vị trí (hoãn sang task P02).
3. **Hàng đợi chấm công ngoại tuyến (Offline Punch Queue):** Không hỗ trợ chấm công khi mất mạng trên mobile trong bản MVP; chỉ chấp nhận chấm công online có kết nối internet để đảm bảo giờ server chuẩn xác.
4. **Tự động hóa tính Thuế TNCN & Bảo hiểm xã hội theo luật:** MVP chỉ hỗ trợ các khoản nhập tay có nhãn demo; không xây dựng công cụ tự động tính thuế lũy tiến từng phần hay biểu phí BHXH theo luật lao động thực tế (hoãn sang P03).
5. **Mở rộng quản lý dư nợ/công nợ tạm ứng sang kỳ sau:** Khi lương thực nhận bị âm, hệ thống chỉ chặn duyệt và yêu cầu xử lý thủ công tại kỳ hiện tại; không tự động sinh nghiệp vụ chuyển nợ lũy kế sang kỳ tiếp theo.
6. **Quy trình hủy/hoàn đơn nghỉ phép đã duyệt (`Approved`):** Đơn đã duyệt không thể tự hủy; quy trình reversal hoàn phép có workflow và audit đầy đủ được chuyển sau MVP.
7. **Trợ lý ảo AI Gemini:** Tạm tắt tính năng chatbot trong MVP nhằm loại bỏ rủi ro rò rỉ dữ liệu cá nhân (PII) và không làm phức tạp trust boundary khi bảo vệ đồ án (hoãn sang P07).
8. **Tuyển dụng & Đào tạo chuyên sâu:** Giữ nguyên dữ liệu legacy tham chiếu; không xây dựng lại màn hình tuyển dụng, lọc CV, vòng phỏng vấn hay quản lý chứng chỉ trong 8 tuần này (hoãn sang P01).
9. **Nền tảng iOS:** Nghiệm thu trên nền tảng **Android** trước. Không cam kết hỗ trợ, build hay kiểm thử trên iOS trong 8 tuần này (hoãn sang P05).
10. **Quy tắc nghỉ phép nâng cao:** Chưa hỗ trợ đơn nghỉ nửa ngày, phép tích lũy theo tháng, phép thâm niên cộng dồn, hay quy tắc kết chuyển phép tồn sang năm sau.

---

## 3. Đối tượng sử dụng và Phân định vai trò (Actors & RBAC)

1. **Employee (Nhân viên):**
   - Đăng nhập (sau khi tự kích hoạt qua thư mời), đổi mật khẩu, xem hồ sơ cá nhân của mình.
   - Chấm công online hàng ngày theo giờ máy chủ; xem lịch sử công của bản thân.
   - Tra cứu số dư phép năm; tạo đơn nghỉ phép nguyên ngày; xem tiến trình phê duyệt; hủy đơn phép khi còn ở trạng thái `Pending`.
   - Xem phiếu lương cá nhân của các kỳ đã được công bố (`Published`), kể cả sau khi kỳ lương đã chuyển sang trạng thái Khóa (`Locked`).
   - *Ràng buộc:* Tuyệt đối không thể truy cập, xem hoặc chỉnh sửa dữ liệu của nhân viên khác.

2. **HR (Quản lý Nhân sự):**
   - Quản lý danh mục phòng ban, chức vụ; thêm mới, sửa thông tin hồ sơ nhân viên; thực hiện thôi việc (offboarding).
   - Khi offboarding: Khóa tài khoản và thu hồi quyền truy cập; xử lý các đơn nghỉ phép `Pending` bằng thao tác `Reject` có lý do và audit (cấm duyệt ngày nghỉ sau ngày thôi việc); đơn `Approved` giữ nguyên.
   - Cấu hình lịch làm việc, ngày nghỉ lễ của công ty có phiên bản.
   - Tra cứu dữ liệu chấm công toàn công ty; điều chỉnh chấm công có lý do hợp lệ và lưu vết audit log (cấm sửa công của kỳ đã công bố).
   - Tiếp nhận và phê duyệt (`Approve`) hoặc từ chối (`Reject`) đơn nghỉ phép của nhân viên (cấm duyệt phép của kỳ đã công bố).
   - *Ràng buộc:* Không được tự phê duyệt đơn nghỉ phép của chính mình; không mặc định có quyền thay đổi mức lương của nhân viên trừ khi được cấp thêm quyền `ManageSalaryHistory`.

3. **Payroll (Kế toán tiền lương):**
   - Quản lý lịch sử mức lương của nhân viên (`SalaryHistory`) nếu có quyền `ManageSalaryHistory`.
   - Nhập các khoản phụ cấp, thưởng, phạt, tạm ứng demo cho kỳ lương.
   - Khởi tạo kỳ lương tháng, chạy tiến trình tính toán lương nháp (`CalculatePayroll`).
   - Rà soát bảng lương, đối soát số liệu công/phép.
   - Phê duyệt bảng lương (`ApprovePayroll`): **Người tính không được tự duyệt chính run đó.**
   - Công bố bảng lương (`PublishPayroll`), khóa kỳ lương (`LockPayroll`), xuất dữ liệu ra tệp CSV an toàn (`ExportPayroll`).

4. **SystemAdmin (Quản trị hệ thống):**
   - Khởi tạo tài khoản nhân viên (gửi lời mời kích hoạt), gán vai trò (`Roles`) và quyền hạn (`Permissions`).
   - Khóa / Mở khóa tài khoản người dùng; kích hoạt cơ chế thu hồi phiên làm việc tức thời.
   - Giám sát nhật ký hoạt động hệ thống (Audit Logs), theo dõi cấu hình kỹ thuật.
   - *Ràng buộc:* **SystemAdmin không mặc nhiên có quyền xem bảng lương hay phiếu lương của nhân viên.**

---

## 4. Phân chia phạm vi Web và Mobile

| Tính năng | Web Portal (React / TypeScript) | Mobile App (React Native - Android) |
| :--- | :---: | :---: |
| **Đối tượng sử dụng** | Quản trị viên, HR, Kế toán lương, Nhân viên | Nhân viên thông thường (Employee Self-Service) |
| **Đăng nhập / Tự kích hoạt / Đổi / Quên mật khẩu** | Có | Có |
| **Xem hồ sơ cá nhân** | Có | Có |
| **Quản lý Nhân viên & Phòng ban** | Có (CRUD, Offboarding) | Không |
| **Cấu hình lịch làm việc & Ngày lễ** | Có | Không (Chỉ xem lịch áp dụng) |
| **Chấm công Check-in / Check-out** | Có (Dành cho nhân viên văn phòng) | Có (Trọng tâm trải nghiệm mobile) |
| **Xem lịch sử công & Trạng thái** | Có | Có |
| **Điều chỉnh chấm công (Sửa giờ)** | Có (HR thao tác duyệt & sửa có lý do) | Không |
| **Tạo & Hủy đơn nghỉ phép** | Có | Có (Chỉ tạo ngày nguyên, chỉ hủy đơn `Pending`) |
| **Duyệt / Từ chối đơn nghỉ phép** | Có (HR duyệt danh sách tập trung) | Không (Tập trung trên Web ở MVP) |
| **Xem quỹ phép còn lại** | Có | Có |
| **Tính toán & Khóa bảng lương** | Có (Payroll quản lý toàn bộ vòng đời) | Không |
| **Xem phiếu lương cá nhân** | Có (Kỳ `Published` và `Locked`) | Có (Kỳ `Published` và `Locked`) |
| **Xuất báo cáo lương CSV** | Có | Không |
| **Quản trị tài khoản & Xem Audit Log** | Có (SystemAdmin) | Không |

---

## 5. Điều kiện bắt buộc để coi là hoàn thành đồ án (DoD tốt nghiệp)

Dự án chỉ được coi là đạt điều kiện bảo vệ tốt nghiệp khi thỏa mãn đầy đủ các tiêu chí nghiệm thu sau:

1. **Toàn vẹn kiến trúc:**
   - Backend ASP.NET Core (.NET 10 LTS) hoạt động độc lập, không phụ thuộc vào project WinForms cũ.
   - Giao tiếp giữa Web/Mobile và Backend 100% thông qua RESTful API chuẩn hóa, có tài liệu OpenAPI/Swagger đồng bộ.
   - Web và Mobile dùng chung một tập hợp Domain Rules và DTOs logic tại Backend.

2. **Chất lượng và Kiểm thử phần mềm:**
   - Mã nguồn Backend có bộ kiểm thử tự động (Unit Tests) bao phủ các công thức tính lương thuần, logic trừ quỹ phép và state machine của bảng lương.
   - Có kiểm thử tích hợp (Integration Tests) chạy trên môi trường cơ sở dữ liệu SQL Server thật để kiểm chứng hành vi transaction và chống race condition.
   - Không còn cảnh báo bảo mật nghiêm trọng (High/Critical) trong danh mục package dependencies.

3. **Khép kín 12 kịch bản nghiệm thu bắt buộc (R01 $\rightarrow$ R12):**
   - [ ] **R01 (IDOR):** Nhân viên A sửa ID/URL sang bản ghi của nhân viên B $\rightarrow$ Backend trả về `403 Forbidden` hoặc `404 Not Found`.
   - [ ] **R02 (Revocation):** Khóa tài khoản hoặc đổi mật khẩu $\rightarrow$ Kiểm chứng tiêu chí thu hồi cho Cookie, Access Token hiện hữu và Refresh Token (request bắt đầu sau khi commit thao tác bị từ chối).
   - [ ] **R03 (Concurrency Leave):** Hai người duyệt đồng thời 2 đơn phép vượt quá quỹ $\rightarrow$ Hệ thống bắt xung đột, không để quỹ phép bị âm.
   - [ ] **R04 (Idempotent Attendance):** Mạng lag gửi lặp check-in $\rightarrow$ Chỉ ghi nhận đúng 1 lần công duy nhất.
   - [ ] **R05 (Salary History):** Tăng lương từ tháng sau $\rightarrow$ Phiếu lương của kỳ tháng trước đã khóa giữ nguyên 100% số tiền cũ.
   - [ ] **R06 (Atomic Payroll):** Tính lương gặp lỗi nửa chừng $\rightarrow$ Tự động rollback, không để lại trạng thái dữ liệu rác hay bảng lương một nửa.
   - [ ] **R07 (Calculation Precision):** Kết quả tính toán công, phép, OT và lương khớp chính xác với kết quả tính tay trong 10 bộ dữ liệu kiểm thử.
   - [ ] **R08 (Partial Update):** Sửa số điện thoại nhân viên mà không gửi địa chỉ $\rightarrow$ Không làm mất trường địa chỉ cũ trong database.
   - [ ] **R09 (Password Reset Token):** Reset sai purpose, hết hạn 15 phút, hoặc token đã dùng $\rightarrow$ Bị từ chối an toàn; không lộ thông tin email.
   - [ ] **R10 (Clean Migration):** Dựng cơ sở dữ liệu mới hoàn toàn từ Migration script không gặp lỗi thiếu cột (`Hwid`).
   - [ ] **R11 (CSV Security):** Tải file CSV bảng lương $\rightarrow$ Dữ liệu chứa ký tự lạ (`=`, `@`, `+`, `-`) được escape an toàn, không thực thi mã độc trong Excel.
   - [ ] **R12 (Backup & Restore):** Quy trình sao lưu và khôi phục cơ sở dữ liệu được thực nghiệm thành công, toàn vẹn số liệu.

---

## 6. Phân biệt phiên bản đồ án với phiên bản thử nghiệm thực tế (Pilot)

| Tiêu chí | Phiên bản Đồ án Tốt nghiệp (8 tuần) | Phiên bản Thử nghiệm Thực tế (Pilot / Production) |
| :--- | :--- | :--- |
| **Mục đích chính** | Đánh giá năng lực kỹ thuật, kiến trúc phần mềm, bảo mật và khả năng giải quyết bài toán nghiệp vụ trong môi trường học thuật. | Đưa hệ thống vào vận hành cho một doanh nghiệp thật sử dụng để chấm công và chi trả tiền lương thực tế. |
| **Quy định pháp lý & Thuế** | Áp dụng công thức tính lương, phụ cấp, giảm trừ mẫu (Demo parameters). Không cam kết tuân thủ chính xác 100% Luật Lao động & Luật Thuế TNCN hiện hành. | BẮT BUỘC có sự rà soát và xác nhận của Luật sư lao động và Chuyên gia Kế toán tiền lương theo chính sách thực tế của tổ chức và quy định hiện hành (Task P03). |
| **Chống gian lận chấm công** | Kiểm soát bằng Server Time, trạng thái mạng công ty và Idempotency. | Tùy thuộc vào yêu cầu khảo sát thực tế của doanh nghiệp: Đánh giá nhu cầu bổ sung xác thực sinh trắc học hoặc định vị (Task P02); không mặc định ép buộc mọi mô hình pilot. |
| **Bảo vệ dữ liệu cá nhân** | Dữ liệu nhân viên, số điện thoại là dữ liệu giả lập (Mock data). Hệ thống phân quyền chặt chẽ trên môi trường Staging/Dev. | Rà soát và tuân thủ các quy định hiện hành về bảo vệ dữ liệu cá nhân: mã hóa thông tin nhạy cảm at-rest, cam kết quyền riêng tư, quy trình thỏa thuận xử lý dữ liệu (Task P04). |
| **Hạ tầng & Vận hành** | Chạy thử nghiệm trên máy chủ cục bộ hoặc VPS Staging cấu hình vừa phải; SQL Server Developer Edition. Cấu hình email thử nghiệm (dự kiến ở T22). | Triển khai trên môi trường Production tiêu chuẩn phù hợp với quy mô tải thực tế; bản quyền SQL Server thương mại hợp lệ; thiết lập sao lưu định kỳ và giám sát lỗi. |
| **Người sử dụng** | Sinh viên, giảng viên hướng dẫn và hội đồng chấm đồ án đóng vai các nhân vật demo. | Nhân viên và các phòng ban thực tế của doanh nghiệp với quy trình đào tạo và hỗ trợ trực tiếp. |
