# T06 — Nghiệm thu theo ủy quyền

Ngày 11/11/2026, Antigravity thực hiện review và chạy lại toàn bộ gate kiểm chứng độc lập theo ủy quyền của chủ dự án tại prompt giao việc: “Tôi ủy quyền chốt VERIFIED cho T06 nếu review và kiểm chứng thực tế đạt; phải lập biên bản, không chỉ sửa nhãn trạng thái.”

Trạng thái: **VERIFIED (Antigravity theo ủy quyền chủ dự án)**.

## 1. Kết quả review code auth và cơ chế bảo mật

- **Identity & Account Lifecycle:** Chuẩn hóa username unique, email unique, password hashing chuẩn ASP.NET Core Identity PBKDF2; không mở public registration, không seed tài khoản/mật khẩu mặc định.
- **Reset Token & Kích hoạt:** Token activation và password reset tách biệt provider/purpose (`HrmActivation` / `ResetPassword`). Hạn reset chuẩn 15 phút theo ADR-0006/0009. Token sai account, hết hạn hoặc dùng lại đều bị từ chối; concurrent reset với cùng token được kiểm soát atomic chỉ 1 commit.
- **CSRF & Cookie Protection:** Web cookie `__Host-Hrm.Session` có cờ `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/`, thời hạn tuyệt đối 8 giờ không sliding. Header `X-XSRF-TOKEN` bắt buộc cho mọi request unsafe mang cookie web và web login.
- **Mobile Bearer & Refresh Token:** Bearer opaque token (15 phút) và Refresh Token (7 ngày) dùng purpose-separated ticket protectors của ASP.NET Core, không phải JWT tự chế. Không lưu raw token trong DB. Phiên lưu trong bảng `AccountSessions` với atomic compare-and-swap (CAS) trên `RefreshVersion` chống replay và race condition.
- **Revocation SLA theo T00/ADR-0006:** Mọi request bắt đầu sau khi thao tác đổi mật khẩu, reset mật khẩu, disable tài khoản, logout hoặc logout-all đã commit vào database đều bị từ chối `401 Unauthorized` ngay lập tức (không qua cache lọt tài khoản). Port offboarding tham gia cùng transaction với nghiệp vụ caller để rollback đồng thời nếu có lỗi.
- **Rate Limit & Anti-Enumeration:** 20 auth requests/IP/phút trả `429 Too Many Requests` + `Retry-After: 60`. Thất bại đăng nhập do sai user, sai pass, tài khoản inactive hay lockout đều trả chung mã `auth.invalid` với thời gian xử lý cân bằng bằng dummy password verification.
- **Email & Log:** Không cấu hình SMTP trả `202 Accepted` với thông báo trung thực `Chưa cấu hình email; không có email nào được gửi`, không lộ sự tồn tại của account. Không ghi token, password hay reset code ra log hoặc response.

## 2. Bằng chứng kiểm chứng thực tế

Lệnh thực thi độc lập:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t06.py --dotnet .dotnet/dotnet.exe
```

Thư mục bằng chứng: [runs/T06-20261111T105803Z-bf3c1838](../verification/runs/T06-20261111T105803Z-bf3c1838/result.json).

- **SQL Integration Tests:** 57/57 executed, 57 passed, 0 failed, 0 skipped. Gồm:
  - 34/34 ca kiểm thử nền tảng T05.
  - 23/23 ca kiểm thử auth matrix T06 (invitation, reset purpose/expiry/concurrency, cookie/CSRF, revocation SLA 6 scenarios, refresh atomic CAS/replay/expiry, lockout/rate limit, isolation, migration rollback/upgrade, offboarding rollback).
- **Python Verifiers:** 49/49 passed (bao gồm negative assertions, baseline integrity, orchestration).
- **Build:** `backend/HRM.Next.slnx` và `db/HRM.sln` đạt 0 warning, 0 error.
- **Bảo mật thư viện:** `dotnet list package --vulnerable` và `npm audit` đạt 0 lỗ hổng.
- **Hợp đồng & Client:** Runtime OpenAPI schema và TypeScript client sinh tự động hoàn toàn đồng bộ, type checks và Kestrel HTTP smoke test đạt.
- **SQL Cleanup:** Database test `HRM_Next_Test_T05_b156280ac6054f81b317177608f7079a` tự động tạo trên SQL Server thật và dọn dẹp sạch sau khi test xong (`cleanupCompleted: true`).
- **Legacy Gate:** 8 lỗi hồi quy legacy vẫn fail đúng danh mục manifest đã biết; không sửa expected để che lỗi.

## 3. Quyết định nghiệm thu

- Chốt nghiệm thu **T06: VERIFIED**.
- Cho phép chuyển sang triển khai nhiệm vụ phụ thuộc **T07 — Quyền theo chức năng và phạm vi bản ghi**.
