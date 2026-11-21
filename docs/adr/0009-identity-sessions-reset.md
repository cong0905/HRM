# ADR-0009 — Identity, phiên web/mobile và token dùng một lần

Ngày 11/11/2026. Triển khai T06 theo ADR-0006 và SLA đã ký ở T00; READY_FOR_REVIEW.
Không thay quyết định nghiệp vụ đã chốt. Identity độc lập trong DB HRM Next, không sử dụng token/reset
legacy. T08 sẽ bổ sung entity Employee và FK/account lifecycle cùng migration, không gắn ID legacy vào
một cột giả chưa có ràng buộc. T07 tiếp tục quyền chức năng và scope bản ghi.

## Identity và mời kích hoạt

- ASP.NET Core Identity 10.0.15, EF Identity store trong HrmDbContext. Domain/Application không tham chiếu Identity.
  Username chuẩn hóa theo Identity, unique index ở SQL; normalized email cũng unique. Dùng password hasher
  Identity V3 và policy tối thiểu 12 ký tự, chữ hoa/thường/số/ký tự đặc biệt. Không tự viết thuật toán hash/token.
- Không map public self-register và không tự tạo tài khoản/role mặc định. Port `AccountService.InviteAsync`
  dành cho workflow tin cậy T08: tạo user inactive không password, gửi lời mời purpose/provider Activation riêng,
  hạn 24h. Nhận đúng lời mời mới đặt được mật khẩu và active; active rồi không dùng lại lời mời.
- Khoá login sau 5 lần sai trong 15 phút; anonymous login không phân biệt tên sai, mật khẩu sai, inactive hay locked
  trong status/errorCode. Missing/inactive/locked path vẫn thực hiện password hash verification giả để giảm
  chênh lệch thời gian; không cam kết thời gian toàn request tuyệt đối bằng nhau. Rate limit thêm 20 request
  auth/IP/phút, queue 0, trả 429 + Retry-After. Bộ giới hạn hiện in-process; reverse proxy/multi-instance thuộc T25.

## Web và mobile

- Auth route yêu cầu HTTPS. Web cookie `__Host-Hrm.Session`, Secure, HttpOnly, SameSite=Strict, Path=/,
  thời hạn tuyệt đối 8h, không sliding. GET `/auth/csrf` tạo anti-forgery cookie HttpOnly/Secure và trả request
  token; client gửi header **X-XSRF-TOKEN**. Web login và mọi unsafe request mang session cookie đều phải
  có anti-forgery token hợp lệ. Sau login cần lấy token mới vì token gắn với identity hiện tại.
- Mobile dùng bearer opaque chuẩn framework, **không phải JWT**. Framework BearerTokenProtector và
  RefreshTokenProtector có purpose khác nhau, bảo vệ AuthenticationTicket. Access hạn 15 phút;
  refresh/session hạn tuyệt đối **7 ngày** từ login, refresh không kéo dài hạn. Không dùng access làm refresh
  hoặc ngược lại. T20 phải lưu token trong Secure Storage và chỉ truyền qua HTTPS.
- `AccountSessions` giữ Id, UserId FK, SecurityStamp, RefreshVersion, expiry và revoked; không lưu raw token.
  RefreshVersion là nonce bên trong ticket được framework bảo vệ. SQL compare-and-swap chỉ cho một request
  consume version cũ; request đồng thời/phát lại trả401, không cấp token thứ hai. Không tự hủy cả phiên khi
  thấy stale refresh, để không giết phiên của request thắng khi hai request hợp lệ đua nhau; client phải serialize
  refresh và xử lý401. Session vẫn có thể logout/revoke toàn bộ.
- `SessionAuthenticationHandler` gọi handler cookie/bearer chuẩn để kiểm crypto/expiry, rồi query SQL trực tiếp
  cho mỗi request đã xác thực: session còn hạn/chưa revoke, user active, SecurityStamp cả ba phía khớp,
  không bị lockout. Không cache. Có Authorization header thì không fallback cookie khi bearer sai.
  HTTP challenge giữ mã lỗi T05 `authentication.required`/`authorization.forbidden`.

## Thu hồi và transaction

- Theo SLA T00: request bắt đầu sau khi revoke/disable/change/reset password đã commit bị401 với cookie,
  bearer còn hạn và refresh cũ. Không hủy ngược request đang xử lý trước commit.
- Logout thu hồi session hiện tại và xóa cookie; token/cookie copy của session đó không dùng lại được;
  session khác còn hoạt động. Logout-all xoay SecurityStamp và revoke mọi session. Đổi/reset password cũng
  đổi stamp và revoke mọi session. Disable/offboarding đổi active=false, đổi stamp và revoke trong cùng transaction.
- Login/reset/change/revoke/refresh khóa user trước rồi session, thống nhất lock order. Reset đặt password
  và revoke atomic; concurrent reset cùng code chỉ một commit. Port offboarding tham gia transaction có sẵn
  của caller để T08 có thể rollback cả nhân sự lẫn tài khoản; API use case tự mở/commit khi chưa có transaction.
- Không thêm public disable/admin endpoint khi T07 chưa có permission policy. Auth/me chỉ trả ID và username;
  login không tự cấp quyền đọc phòng ban, PII hay lương.

## Reset, email và vận hành

- Reset dùng UserManager/Data Protection, purpose ResetPassword, hạn **15 phút** theo ADR-0006.
  Verify-email/activation token, sai account, token cũ/đã dùng/hết hạn bị từ chối. Password policy vẫn áp dụng.
  Identity dùng thời gian hệ thống trong token provider; test expiry làm token hết hạn bằng cấu hình lifespan
  âm ở test-only, đồng thời assert lifespan production 15 phút. Cookie/bearer/session tests dùng TimeProvider.
- Default sender trả mode `not-configured`: HTTP202 nói rõ **không có email nào được gửi**, giống nhau cho
  account có/không tồn tại. Không trả raw code, không log credentials. Sender in-memory `test-only` chỉ tồn tại
  trong test assembly, không gửi email thật. Không nhận reset token từ query string để tránh access logs.
- Port IAccountMessageSender là điểm tích hợp T22. T22 cần outbox transaction, retry, encrypted payload/retention,
  idempotency và delivery status; T06 không tuyên bố đã gửi hoặc đã persist outbox. Lỗi sender không được
  làm response tiết lộ account có tồn tại. Hiện không có SMTP hoặc mailbox UI.
- Data Protection có application name HRM.Next, cho phép cấu hình `DataProtection__KeyDirectory`.
  Dev dùng key store mặc định framework; test dùng ephemeral provider. Triển khai nhiều instance cần chung
  key ring, ACL và cơ chế bảo vệ key at rest; cấu hình chứng chỉ/secret store/reverse proxy thuộc T25.
  Không ghi key/password/token vào repository. Đây chưa phải đánh giá production-ready.

Nguồn chuẩn: [Identity API](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0),
[framework bearer handler](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Security/Authentication/BearerToken/src/BearerTokenHandler.cs),
[ASP.NET Core antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).
