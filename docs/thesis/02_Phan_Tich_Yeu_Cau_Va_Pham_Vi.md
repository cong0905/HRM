# CHƯƠNG 2: PHÂN TÍCH YÊU CẦU VÀ MA TRẬN NGHIỆP VỤ

## 2.1. Yêu cầu Chức năng (Functional Requirements)

Hệ thống HRM hiện đại hóa được phân rã thành 6 nhóm nghiệp vụ chính:

### 1. Phân hệ Quản trị Danh tính và Tài khoản (Identity & Access Control)
- **Đăng nhập & Phiên làm việc**: Hỗ trợ đăng nhập trên Web (cấp Cookie an toàn) và Mobile (cấp cặp Bearer Access Token / Refresh Token).
- **Thu hồi phiên tức thời (Instant Revocation)**: Khi người dùng đổi mật khẩu, đăng xuất tất cả thiết bị, hoặc tài khoản bị vô hiệu hóa bởi Admin, toàn bộ phiên làm việc của tài khoản đó lập tức bị từ chối truy cập.
- **Quên mật khẩu & Kích hoạt tài khoản**: Gửi email chứa liên kết an toàn kèm token dùng một lần, có thời hạn 15 phút, tự động hủy khi hết hạn.

### 2. Phân hệ Quản lý Tổ chức và Hồ sơ Nhân sự (Personnel)
- **Hồ sơ nhân viên**: Quản lý thông tin định danh, liên hệ, ngày sinh, ngày vào làm, chức vụ, phòng ban, tình trạng làm việc (`DangLamViec`, `DaNghiViec`).
- **Cấp mã nhân viên tự động**: Sinh mã chuẩn `NV{YYYY}{XXXX}` duy nhất (tối đa 9999 nhân viên/năm) với cơ chế khóa tuần tự chống trùng mã khi tạo đồng thời.
- **Phân công phạm vi quản lý (HR Scope Assignment)**: Cho phép cấu hình phân quyền HR chỉ được xem/sửa hồ sơ nhân viên trong phạm vi được phân công.
- **Quy trình cho thôi việc (Offboarding)**: Đánh dấu nhân viên nghỉ việc (`DaNghiViec`), tự động vô hiệu hóa tài khoản và thu hồi toàn bộ phiên đăng nhập trong cùng một giao dịch.

### 3. Phân hệ Lịch công ty và Chấm công (Work Calendar & Attendance)
- **Lịch làm việc**: Định nghĩa ca làm việc chuẩn (giờ bắt đầu 08:30, kết thúc 17:30, nghỉ trưa 12:00–13:00, tổng chuẩn 8 giờ/ngày) theo ngày hiệu lực.
- **Chấm công trực tuyến**: Nhân viên check-in / check-out qua Mobile App; ghi nhận thời gian theo giờ chuẩn của máy chủ trung tâm (`Server Clock`); bắt buộc gửi kèm `Idempotency-Key` để chống ghi nhận công trùng lặp do lỗi mạng.
- **Điều chỉnh công & Duyệt làm thêm (OT)**: Cho phép HR điều chỉnh giờ công khi có lý do chính đáng; ghi nhận nhật ký kiểm toán Before/After đầy đủ; HR xác nhận số phút làm thêm giờ hợp lệ.

### 4. Phân hệ Quản lý Nghỉ phép (Leave Management)
- **Quỹ phép năm**: Cấp phát quỹ phép chuẩn (12 ngày/năm). Hệ thống ghi nhận số ngày được cấp (`AllocatedDays`) và số ngày đã sử dụng (`UsedDays`).
- **Quy trình đơn nghỉ**: Nhân viên tạo đơn phép (`Pending`); người quản lý/HR duyệt (`Approved`) hoặc từ chối (`Rejected`); nhân viên được quyền hủy (`Cancelled`) đơn khi còn ở trạng thái `Pending`.
- **Sổ cái nghỉ phép (Leave Ledger)**: Mọi biến động tăng/giảm quỹ phép đều ghi nhận dòng sổ cái tương ứng (`Grant`, `Approve`, `Cancel`) phục vụ truy vết.

### 5. Phân hệ Tính toán và Quản lý Tiền lương (Payroll)
- **Lịch sử lương (Salary History)**: Quản lý các quyết định lương theo khoảng thời gian hiệu lực (`EffectiveFrom` đến `EffectiveTo`). Cấm ghi đè lịch sử cũ, chỉ cho phép nối tiếp quyết định mới.
- **Kỳ lương & Vòng đời**: Kỳ lương chuyển đổi trạng thái nghiêm ngặt: `Draft` (Nháp) -> `Calculated` (Đã tính) -> `Approved` (Đã duyệt) -> `Published` (Đã công bố) -> `Locked` (Đã khóa).
- **Snapshot bất biến**: Mỗi lần tính toán tạo một bản ghi snapshot độc lập lưu giữ nguyên trạng công chuẩn, giờ làm, số tiền phụ cấp/khấu trừ và lương thực nhận. Kỳ lương đã công bố hoặc đã khóa là bất biến vĩnh viễn.
- **Xuất bảng lương an toàn**: Xuất dữ liệu bảng lương ra file CSV có cơ chế chống tiêm nhiễm công thức độc hại (Anti-CSV Formula Injection).

### 6. Phân hệ Thông báo Outbox (Transactional Outbox)
- Đưa các yêu cầu gửi email đặt lại mật khẩu và kích hoạt tài khoản vào hàng đợi cơ sở dữ liệu `[notifications].[EmailOutboxMessages]`.
- Worker nền tự động gửi thư qua SMTP, xử lý retry với exponential backoff khi gặp sự cố mạng, bảo vệ thông tin token không bị rò rỉ trong log.

---

## 2.2. Ma trận Phân quyền Bắt buộc (Role-Based Access Control Matrix)

Hệ thống định nghĩa 4 vai trò chuẩn với phạm vi quyền hạn phân tách độc lập:

| Thao tác nghiệp vụ | Employee (Nhân viên) | HR (Nhân sự) | Payroll (Kế toán) | SystemAdmin (Quản trị) |
|---|:---:|:---:|:---:|:---:|
| **Hồ sơ cá nhân** | Xem thông tin của chính mình | Xem theo phạm vi quản lý | Xem trường cần tính lương | Không mặc định xem PII |
| **Quản lý Nhân viên** | Không có quyền | Tạo mới, sửa hồ sơ, offboarding | Xem theo danh sách cấp quyền | Gán vai trò, quản lý tài khoản |
| **Chấm công** | Chấm công của mình, xem lịch sử | Tra cứu toàn công ty, sửa công | Đọc bảng công tổng hợp | Cấu hình tham số hệ thống |
| **Đơn nghỉ phép** | Tạo đơn, hủy đơn Pending của mình | Duyệt/từ chối đơn (Cấm tự duyệt) | Đọc dữ liệu nghỉ hưởng lương | Không duyệt đơn thay HR |
| **Phiếu lương** | Xem phiếu đã công bố của mình | Chỉ khi được cấp thêm quyền | Lập kỳ, tính nháp, duyệt, công bố | Không xem chi tiết tiền lương |
| **Tài khoản & Phân quyền** | Đổi mật khẩu cá nhân | Yêu cầu khóa tài khoản nghỉ việc | Không có quyền | Quản lý tài khoản, gán quyền |

> **Nguyên tắc cốt lõi**:
> 1. Máy chủ xác định danh tính trực tiếp từ Claim phiên làm việc, client không được truyền `employeeId` của mình lên các endpoint cá nhân (`/me/*`) nhằm triệt tiêu nguy cơ lỗ hổng IDOR (Insecure Direct Object References).
> 2. Giao diện ẩn nút bấm tương ứng với quyền hạn, đồng thời tầng API bắt buộc kiểm tra `Authorize` và trả về `401 Unauthorized` hoặc `403 Forbidden` nếu người dùng cố tình gọi trực tiếp endpoint.

---

## 2.3. Các Ràng buộc Bất biến Cốt lõi (System Invariants)

Hệ thống cam kết duy trì tuyệt đối các bất biến sau tại tầng Domain và Cơ sở dữ liệu:
1. **Không âm quỹ phép**: `UsedDays <= AllocatedDays` và `UsedDays >= 0` (được bảo vệ bằng SQL Check Constraint `CK_LeaveBalances_NoOverdraft` và `CK_LeaveBalances_UsedDays_NonNegative`).
2. **Không tự duyệt phép**: Người duyệt (`ApproverId`) bắt buộc phải khác người nộp đơn (`EmployeeId`).
3. **Tính bất biến của kỳ lương đã chốt**: Bảng tính lương và phiếu lương ở trạng thái `Published` hoặc `Locked` không được phép chỉnh sửa số tiền hay xóa bỏ (`payroll.period_immutable`).
4. **Không tính lặp công và phép**: Một ngày làm việc của nhân viên không thể vừa tính trọn vẹn giờ đi làm vừa tính nghỉ phép hưởng lương vượt quá thời lượng ca chuẩn (8 giờ).
5. **Đồng hồ chuẩn Server**: Thời gian chấm công luôn lấy `UtcNow` từ máy chủ backend; cấm nhận timestamp từ client.
6. **Mật khẩu và Token an toàn**: Mật khẩu bắt buộc tối thiểu 12 ký tự gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt; cấm lưu trữ mật khẩu dạng rõ hoặc MD5; token đặt lại mật khẩu không bao giờ được xuất hiện trong URL log.

---

## 2.4. Danh mục Phạm vi Hoàn thành và Tính năng Hoãn lại (Scope & Deferred)

Để đảm bảo dự án hoàn thành đúng tiến độ 8 tuần với chất lượng và độ tin cậy cao nhất, nhóm tác giả phân định minh bạch phạm vi:

### Tính năng đã hoàn thành 100% trong MVP:
- Toàn bộ nghiệp vụ quản lý nhân sự, chấm công, nghỉ phép, tính lương theo ca chuẩn hành chính ban ngày.
- Bộ API chuẩn OpenAPI/Swagger và TypeScript SDK đồng bộ tự động.
- Giao diện Web SPA quản trị đầy đủ các phân hệ.
- Ứng dụng di động Mobile Android cho nhân viên (Chấm công, Nghỉ phép, Phiếu lương, Hồ sơ, Cache wipe on logout).
- Bộ công cụ di chuyển dữ liệu (T23), sao lưu phục hồi (T26) và đóng gói staging Docker (T25).

### Tính năng hoãn lại (Deferred Beyond MVP) có ghi nhận lý do:
- **Ca làm việc qua đêm (Overnight shifts)**: Nghiệp vụ ca gãy, ca đêm có cách tính phụ cấp ca đêm và ngày công phức tạp, hoãn lại để chốt chính sách lao động ở giai đoạn 2.
- **Tự động trích nộp Thuế TNCN và Bảo hiểm theo bậc lũy tiến**: Chính sách thuế và bảo hiểm xã hội thay đổi theo từng thời kỳ, trong phạm vi đồ án áp dụng mức khấu trừ phẳng được cấu hình tường minh để đảm bảo tính ổn định của công thức tính.
- **Ứng dụng iOS**: Tập trung hoàn thiện và kiểm thử thực tế trên nền tảng Android trước; hoãn triển khai iOS do yêu cầu thiết bị phần cứng macOS và tài khoản Apple Developer.
- **Thông báo đẩy (Push Notifications) và Chatbot hỗ trợ**: Cắt giảm theo chiến lược tối ưu tuần 7-8 để tập trung 100% nguồn lực vào tính toàn vẹn giao dịch và bảo mật.
