# Biểu đồ Trình tự Xác thực Lai và Thu hồi Phiên Tức thời (Sequence Diagram - Hybrid Auth & Session Revocation)

Tài liệu này mô tả chi tiết mô hình **Xác thực Lai (Hybrid Authentication Architecture)** và cơ chế **Thu hồi Phiên Đăng nhập Tức thời (Instant Session Revocation)** thông qua `SecurityStamp` trong hệ thống HRM.

---

## 1. Biểu đồ Trình tự Xử lý (Mermaid Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as "Quản trị HR / SystemAdmin"
    actor Attacker as "Kẻ tấn công / Phiên bị xâm nhập"
    actor VictimMobile as "Nhân viên Bị hại (Mobile App)"
    participant API as "HRM.Api (Middlewares & Auth)"
    participant DB as "SQL Server 2022 (HrmDbContext)"

    %% Giai đoạn 1: Thiết lập Phiên Đăng nhập Lai
    Note over Admin, DB: GIAI ĐOẠN 1: THIẾT LẬP PHIÊN XÁC THỰC LAI (HYBRID AUTH)
    Note over Attacker, API: Web Client: Nhận Cookie HttpOnly (SameSite=Strict, Secure) + Anti-CSRF Header<br/>Mobile Client: Nhận Access Token (JWT 15m) mang Claim SecurityStamp + Refresh Token trong SecureStore

    %% Giai đoạn 2: Quản trị viên Khóa tài khoản hoặc Đổi mật khẩu
    Note over Admin, DB: GIAI ĐOẠN 2: KÍCH HOẠT THU HỒI PHIÊN KHẨN CẤP (REVOCATION TRIGGER)
    Admin->>+API: POST /api/v1/employees/{empId}/lock-account<br/>(Hoặc Nhân viên tự bấm Đổi mật khẩu khẩn cấp)
    API->>+DB: BEGIN TRANSACTION
    API->>DB: UPDATE HrmUsers<br/>SET IsActive = false, SecurityStamp = NEWID(), UpdatedAtUtc = GETUTCDATE()<br/>WHERE Id = @empId
    API->>DB: UPDATE AccountSessions SET IsRevoked = true, RevokedAtUtc = GETUTCDATE()<br/>WHERE UserId = @empId
    API->>DB: COMMIT TRANSACTION
    DB-->>-API: Commit Thành công
    API-->>-Admin: 200 OK { message: "Tài khoản đã bị khóa và toàn bộ phiên đã bị thu hồi" }

    %% Giai đoạn 3: Kẻ tấn công hoặc Phiên cũ cố gắng truy cập bằng Token cũ
    Note over Attacker, API: GIAI ĐOẠN 3: KIỂM CHỨNG TỨC THỜI QUA MIDDLEWARE (SLA <= 0.05 GIÂY)
    Attacker->>+API: GET /api/v1/payroll/me/payslips<br/>[Gửi kèm Bearer Token cũ còn hạn JWT 12 phút, SecurityStamp = 'STAMP-OLD']
    
    API->>API: 1. JwtBearerHandler: Token signature hợp lệ, chưa hết hạn thời gian (Expires: Valid)
    API->>+DB: 2. SecurityStampValidationMiddleware:<br/>SELECT SecurityStamp, IsActive FROM HrmUsers WHERE Id = @userId
    DB-->>-API: Trả về: IsActive = false, SecurityStamp = 'STAMP-NEW'
    
    Note right of API: PHÁT HIỆN SAI LỆCH CON DẤU AN NINH (SecurityStamp Mismatch)!<br/>'STAMP-OLD' != 'STAMP-NEW' HOẶC IsActive == false.<br/>Ngăn chặn ngay lập tức, không cho phép truy cập tầng nghiệp vụ.
    
    API-->>-Attacker: 401 Unauthorized<br/>{ code: "SESSION_REVOKED", message: "Phiên đăng nhập đã bị hủy bỏ hoặc tài khoản bị khóa" }

    %% Giai đoạn 4: Mobile App phản hồi và xóa sạch dữ liệu nhạy cảm
    Note over VictimMobile, API: GIAI ĐOẠN 4: THU DỌN CACHE & DỮ LIỆU BẢO MẬT PHÍA CLIENT
    VictimMobile->>+API: POST /api/v1/attendance/me/check-in<br/>[Gửi kèm Access Token cũ]
    API-->>-VictimMobile: 401 Unauthorized { code: "SESSION_REVOKED" }
    
    Note over VictimMobile: Client Interceptor (Axios / Fetch) bắt lỗi 401:<br/>- Xóa sạch SecureStore (Tokens, Refresh Token)<br/>- Xóa 100% RAM Cache thông tin cá nhân & bảng lương<br/>- Điều hướng cưỡng bức về màn hình Đăng nhập (Login Screen)
```

---

## 2. So sánh Chiến lược Xác thực Web vs. Mobile

| Tiêu chí | Web Portal (Admin / HR / Quản lý) | Mobile App (Nhân viên) |
|---|---|---|
| **Cơ chế chính** | Cookie `HttpOnly`, `SameSite=Strict`, `Secure` | Bearer Token (JWT 15 phút) mang Claim `SecurityStamp` |
| **Phòng chống XSS** | JavaScript hoàn toàn không thể đọc Cookie qua `document.cookie` | Token được lưu trong `SecureStore` (Android KeyStore / iOS Keychain) |
| **Phòng chống CSRF** | Bắt buộc đính kèm header `X-XSRF-TOKEN` cho mọi mutation (`POST`, `PUT`, `DELETE`) | Không áp dụng cơ chế tự gửi Cookie, miễn nhiễm tự nhiên với CSRF |
| **SLA Thu hồi Phiên** | $\le 0.05$ giây (Kiểm tra cơ sở dữ liệu trên mỗi request mutation) | $\le 0.05$ giây (Middleware kiểm tra đối chiếu `SecurityStamp`) |
| **Hành vi khi Logout** | Xóa Cookie trên trình duyệt và đánh dấu `IsRevoked` trên CSDL | Xóa Keychain / SecureStore và xóa toàn bộ state trong bộ nhớ RAM |

---

## 3. Liên kết Tài liệu Liên quan

- [Sơ đồ Kiến trúc Tổng thể](file:///d:/dotnet%20project/docs/diagrams/architecture-overview.md)
- [Báo cáo Kiểm thử T24 - Kiểm chứng Thu hồi Phiên](file:///d:/dotnet%20project/docs/verification/T24-e2e-security-report.md)
- [Kịch bản Demo Bảo vệ](file:///d:/dotnet%20project/docs/demo/demo-scenarios.md)
