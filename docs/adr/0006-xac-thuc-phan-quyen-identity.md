# ADR-0006: Xác thực Tập trung bằng ASP.NET Core Identity và Phân quyền Server-Side

> **Trạng thái:** `CONFIRMED` (Đã được Chủ dự án chính thức ký duyệt — Quyết định `OPEN-07`, `OPEN-10`, `OPEN-11`)  
> **Ngày quyết định:** 10/11/2026  
> **Tác giả:** Chủ dự án & AI Coding Assistant

---

## 1. Bối cảnh và Vấn đề (Context & Problem Statement)

Trong hệ thống WinForms cũ:
- Việc xác thực chỉ đơn thuần là kiểm tra mật khẩu qua helper `PasswordHelper` (BCrypt), sau đó trả về đối tượng `UserSessionDTO` trong bộ nhớ ứng dụng desktop (F09).
- Việc phân quyền chỉ thực hiện trên giao diện GUI (ẩn/hiện menu dựa trên chuỗi `VaiTro`), các Service BLL chấp nhận tham số `bool isAdmin` hoặc ID người dùng do client tự truyền lên mà không có cơ chế xác thực danh tính tại máy chủ.
- Chức năng quên mật khẩu dùng OTP 6 số ngẫu nhiên không an toàn, lưu mã thô, không kiểm tra trường `Purpose`, và việc cập nhật mật khẩu với đánh dấu token đã dùng không nằm trong cùng một transaction (F06).
- `NullEmailSender` trả về hoàn tất ảo làm GUI hiểu nhầm là đã gửi email thành công (F07).
- Thiếu cơ chế thu hồi phiên làm việc tức thời khi tài khoản bị khóa hoặc đổi mật khẩu (F13).

Khi chuyển đổi sang kiến trúc Backend phục vụ Web và Mobile, hệ thống cần một giải pháp xác thực và phân quyền tiêu chuẩn, an toàn và chống lại các cuộc tấn công phổ biến (IDOR, CSRF, Brute-force).

---

## 2. Các Yếu tố Thúc đẩy Quyết định (Decision Drivers)

1. **Tuân thủ chuẩn bảo mật công nghiệp:** Không tự viết thuật toán ký, mã hóa hay sinh token tự chế.
2. **Hỗ trợ đa nền tảng (Web + Mobile):** Web trình duyệt cần cơ chế bảo vệ cookie và chống CSRF; Mobile cần cơ chế token an toàn qua kết nối HTTPS.
3. **Mục tiêu thu hồi quyền truy cập tức thời (Session Revocation Goal):** Bất kỳ request nghiệp vụ nào bắt đầu sau khi thao tác khóa tài khoản hoặc thu hồi đã commit thành công vào cơ sở dữ liệu đều bắt buộc phải bị từ chối.
4. **Phân quyền tại biên Server (Server-Side Authorization):** Server tự động trích xuất danh tính người dùng từ Claims; không bao giờ tin cậy ID người dùng hay quyền do client tự gửi trong request body.

---

## 3. Quyết định (Decision Outcome)

**Chốt giải pháp: Sử dụng thư viện tiêu chuẩn ASP.NET Core Identity.**

### 3.1. Phân biệt Phần Framework Chuẩn và Phần Mở rộng:
- **Phần Framework chuẩn của ASP.NET Core Identity:**
  - Quản lý người dùng (`IdentityUser`), băm mật khẩu bảo mật (PBKDF2-HMAC-SHA256).
  - Quản lý phiên bản bảo mật (`SecurityStamp`).
  - Quản lý khóa tài khoản sau nhiều lần thử sai (`LockoutEnabled`, `AccessFailedCount`).
  - Sinh token đặt lại mật khẩu và kích hoạt tài khoản chuẩn mực qua Data Protection API (`UserManager.GeneratePasswordResetTokenAsync`, `GenerateEmailConfirmationTokenAsync`).
- **Phần Mở rộng Cần thiết cho Mobile:**
  - ASP.NET Core Identity cơ bản không mặc định có sẵn bảng `UserRefreshTokens`. Do đó, cơ chế quản lý Refresh Token (cấp phát, lưu trữ hạn dùng, đánh dấu `IsRevoked`) sẽ được thiết kế như một module mở rộng tại tầng Infrastructure của dự án. Quyết định chi tiết về cấu trúc bảng và lifecycle sẽ được chốt tại **Task T06**.

### 3.2. Cơ chế Xác thực cho Web Frontend:
- Sử dụng **Cookie có cờ `HttpOnly`, `Secure`, và `SameSite=Strict/Lax`**.
- *Đánh giá an toàn:* Cờ `HttpOnly` ngăn chặn script client đọc trực tiếp giá trị cookie, nhưng **không làm cho ứng dụng miễn nhiễm hoàn toàn với XSS** (kẻ tấn công XSS vẫn có thể kích hoạt các request mang cookie từ trình duyệt nạn nhân). Do đó, ứng dụng bắt buộc phải kết hợp:
  - Cơ chế chống tấn công CSRF (Anti-Forgery Token qua header `X-XSRF-TOKEN`) cho mọi request thay đổi trạng thái (POST, PUT, PATCH, DELETE).
  - Áp dụng Content Security Policy (CSP) và mã hóa dữ liệu output trên giao diện.

### 3.3. Cơ chế Xác thực cho Mobile App:
- Cấp phát cặp token: **Bearer Access Token** (thời hạn ngắn: 15 phút) và **Refresh Token** (thời hạn: 7 ngày, lưu trữ có trạng thái tại Database).
- Toàn bộ token trên thiết bị di động được lưu trữ an toàn trong **Android Keystore** (qua `expo-secure-store` hoặc `react-native-keychain`); tuyệt đối không lưu trong `AsyncStorage` thông thường.

### 3.4. Cơ chế Kiểm tra và Thu hồi Quyền truy cập phía Server:
- **Nguyên tắc MVP:** Kiểm tra trạng thái tài khoản (`IsActive`) và phiên bản bảo mật (`SecurityStamp`) **trực tiếp phía server cho mọi request nghiệp vụ đã xác thực; CHƯA áp dụng bộ nhớ đệm (cache) có nguy cơ làm lọt tài khoản vừa bị khóa**.
- **Định nghĩa ranh giới thu hồi:**
  - Bất kỳ request nghiệp vụ nào bắt đầu **sau khi thao tác khóa tài khoản hoặc thu hồi đã commit thành công** vào cơ sở dữ liệu đều bắt buộc phải bị từ chối với mã lỗi `401 Unauthorized`.
  - Không tuyên bố hủy ngược các request đã hoàn thành trước thời điểm commit.
- **Đối với Cookie Web:** Cấu hình `SecurityStampValidator` có chu kỳ kiểm tra tức thời hoặc tích hợp middleware kiểm tra phiên ở mỗi request.
- **Đối với Bearer Access Token hiện hữu:** Middleware tại server trích xuất `UserId` từ token và kiểm tra trực tiếp trạng thái `IsActive` và `SecurityStamp` trong database.
- **Đối với Refresh Token:** Khi tài khoản bị khóa hoặc đổi mật khẩu, toàn bộ Refresh Token của tài khoản đó lập tức bị đánh dấu revoked. Mọi yêu cầu xin cấp Access Token mới sẽ bị từ chối `401 Unauthorized`.

### 3.5. Kích hoạt Tài khoản và Quên Mật khẩu (Reset Token):
- Dùng lời mời kích hoạt có thời hạn để nhân viên tự đặt mật khẩu ban đầu; không có mật khẩu mặc định, HR không nhìn thấy mật khẩu.
- Token quên mật khẩu có thời hạn 15 phút, dùng duy nhất một lần và kiểm tra đúng mục đích (`Purpose = "ResetPassword"`).
- Trong môi trường phát triển (Dev), sử dụng hộp thư thử nghiệm / sender thử có gắn nhãn rõ ràng (dự kiến ở Task T22); không báo gửi email thật khi chưa cấu hình máy chủ SMTP thật.

---

## 4. Tiêu chí Kiểm thử Tự động Cần Triển khai và Kiểm chứng

Các mục tiêu bảo mật dưới đây cần được triển khai và kiểm chứng thông qua các bài kiểm thử tự động (Unit / Integration Tests):
1. **Test Cookie Validation:** Kiểm chứng sau khi commit thao tác khóa tài khoản hoặc đổi mật khẩu, request mang cookie cũ gửi tiếp theo bị từ chối `401 Unauthorized`.
2. **Test Access Token hiện hữu:** Kiểm chứng access token đã cấp phát còn hạn nhưng khi tài khoản bị chuyển sang `IsActive = false`, request gửi tiếp theo bị server từ chối `401 Unauthorized`.
3. **Test Refresh Token:** Kiểm chứng việc dùng Refresh Token của tài khoản đã bị khóa hoặc đã đổi mật khẩu sẽ bị từ chối cấp token mới (`401 Unauthorized`).
4. **Test Reset Token:** Kiểm chứng việc tái sử dụng token đã dùng, dùng token hết hạn hoặc dùng sai mục đích (Purpose) đều bị từ chối an toàn.

---

## 5. Đánh đổi và Hệ quả (Trade-offs & Consequences)

### Điểm tích cực (Pros):
- Đặt mục tiêu xử lý dứt điểm các phát hiện bảo mật F06, F07, F08, F09, F13 thông qua framework tiêu chuẩn.
- Tích hợp sẵn tính năng khóa tài khoản sau nhiều lần đăng nhập sai (Lockout), bảo vệ chống Brute-force.
- Phân quyền chặt chẽ theo Claims và Policy (`[Authorize(Policy = "RequireHrRole")]`, `[Authorize(Policy = "CanApprovePayroll")]`, `[Authorize(Policy = "ManageSalaryHistory")]`).

### Điểm hạn chế và Thách thức (Cons & Mitigations):
- Cơ sở dữ liệu sẽ tích hợp các bảng chuẩn `AspNetUsers`, `AspNetRoles`... kết hợp module mở rộng `UserRefreshTokens`.
  - *Giải pháp:* Tại Task T02 và T06, thực hiện ánh xạ (Mapping) liên kết 1-1 giữa `IdentityUser` và bảng thực thể `NhanVien` thông qua khóa ngoại rõ ràng `MaNhanVien`.
