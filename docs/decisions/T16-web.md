# T16 — Khung web cookie và permission từ server

Ngày13/11/2026, theo yêu cầu chủ dự án: Anti triển khai phần tiếp theo, Codex review và kiểm chứng độc lập. Phạm vi T16; T17+ chưa triển khai từ task này.

## Hợp đồng

React TypeScript Vite SPA, Router và TanStack Query, Ant Design theo ADR0004. Package versions exact và lockfile; Node24 nhất quán shared contracts. Lint là ESLint thực, tách typecheck. Không SSR hoặc bearer-token storage.

Endpoint mới duy nhất: GET /api/v1/access/me [Authorize], Cache-Control:no-store, trả Id/Username/Roles/Permissions từ ISecurityContext đã xác thực. Không tự cấp role/permission, không PII profile/salary. CurrentAccount của auth/me giữ nguyên. API/client được sinh lại, không module augmentation/hand-maintained DTO thay generated schema.

Menu và route guard dùng permission key thật của backend, không tự suy ra quyền tài chính từ SystemAdmin. Navigation chỉ hỗ trợ UX; server tiếp tục quyết định. Các module chưa tới T17+ hiển thị phạm vi chưa triển khai, không fake dữ liệu/counters hoặc gọi là demo hoàn chỉnh.

Web dùng HttpOnly Secure __Host cookies với HTTPS same-origin proxy. GET auth/csrf lấy requestToken, X-XSRF-TOKEN trên mutation kể cả generated-client Request object. Lấy token mới sau login để gắn principal hiện tại. CSRF invalid không tự replay mutation; người dùng chủ động thử lại. ProblemDetails dùng errorCode/traceId theo API.

Bootstrap401 nghĩa anonymous/expired, không retry loop; hủy/xóa private cache khi mất phiên hoặc đổi account. 403 không tự logout. Network error có trạng thái lỗi/retry riêng. Query cache không persist. Logout và đổi mật khẩu hủy/xóa private cache; logout server lỗi phải báo đúng chưa xác nhận, không giả cookie đã bị hủy. Không token/password/cookie trong localStorage/sessionStorage, không tự refresh mobile bearer.

Forms có labels/autocomplete/disabled pending, keyboard/skip-link và viewport375px. Forgot phản ánh deliveryMode thật, not-configured không là email đã gửi. Reset/activate code giữ memory và làm sạch URL; không echo credential trong lỗi/log.

## Bằng chứng

Anti dùng native file tools; command bị headless từ chối được ghi nhận, không lách permissions. Codex chạy npm/audit/build/typecheck/lint/components và test regressions độc lập. Backend gate388 tests gồm383 hồi quy +5 access SQL; scope gate backend không tự chứng minh UI. Web và browser có manifest/hash/evidence riêng, đối chiếu cuối trước chốt.

Browser review dùng API thật trên Kestrel HTTPS, real Identity và SQL GUID fixture, tài khoản test dùng một lần. Chứng chỉ self-signed/test credential tạm chỉ phục vụ localhost và cleanup; không SQL HRM_System hoặc email thật. Test clock/session expiry chỉ trong harness test, không thêm endpoint/debug bypass production. Mocks chỉ dùng isolated component/fault tests, không fake E2E success.

CI thêm web checks và backend T16 gate, nhưng remote CI/human UAT/deploy chưa chạy nếu không có bằng chứng riêng. Không sửa nghiệp vụ công/phép/lương hoặc test cũ để pass UI.
