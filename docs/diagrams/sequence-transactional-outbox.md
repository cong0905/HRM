# Biểu đồ Trình tự Transactional Outbox và Xử lý Gửi Email Nền (Sequence Diagram - Transactional Outbox & Email Worker)

Tài liệu này mô tả chi tiết mô hình thiết kế **Transactional Outbox Pattern** và tiến trình xử lý nền **EmailOutboxWorker** trong hệ thống HRM. Kiến trúc này giải quyết triệt để vấn đề:
1. **Tính nguyên tử kép (Dual-Write Problem)**: Đảm bảo dữ liệu nghiệp vụ và thông báo gửi email cùng được lưu thành công trong một giao dịch cơ sở dữ liệu (`IDbContextTransaction`), không bao giờ xảy ra tình trạng "đã gửi email nhưng dữ liệu nghiệp vụ rollback" hoặc "đã lưu nghiệp vụ nhưng rớt email".
2. **Cơ chế thử lại lũy thừa (Exponential Backoff Retry)**: Tự động phục hồi khi máy chủ SMTP bị nghẽn mạng hoặc khởi động lại.
3. **Chặn gửi token hết hạn (Pre-dispatch Expiration Abort)**: Không gửi email chứa token đổi mật khẩu đã quá hạn.
4. **Mặt nạ bảo mật nhật ký (Safe Log Masking)**: Ngăn chặn tuyệt đối việc rò rỉ token hoặc thông tin bí mật ra tệp nhật ký hệ thống.

---

## 1. Biểu đồ Trình tự Xử lý (Mermaid Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor User as "Người dùng / HR"
    participant API as "HRM.Api (Controllers)"
    participant App as "HRM.Application (Command Handlers)"
    participant DB as "SQL Server 2022 (HrmDbContext)"
    participant Worker as "EmailOutboxWorker (BackgroundService)"
    participant SMTP as "Dịch vụ Mail SMTP (MailHog / Exchange)"

    %% Giai đoạn 1: Ghi nhận Outbox trong cùng một Transaction
    Note over User, DB: GIAI ĐOẠN 1: ENQUEUE OUTBOX ĐỒNG THỜI TRONG TRANSACTION
    User->>+API: POST /api/v1/auth/forgot-password { email: "user@example.com" }
    API->>+App: Handle(RequestPasswordResetCommand)
    App->>App: Sinh mã Token an toàn (Hạn dùng 15 phút)
    App->>+DB: BEGIN TRANSACTION (IDbContextTransaction)
    App->>DB: UPDATE HrmUsers SET PasswordResetToken = @hash, TokenExpiresAtUtc = @expiry
    App->>DB: INSERT INTO EmailOutboxMessages<br/>(Recipient, TemplateCode, Body, Status, MaxAttempts, ExpiresAtUtc, IdempotencyKey)<br/>VALUES ('user@example.com', 'PasswordReset', '...token...', 'Queued', 3, @expiry, @idemKey)
    App->>DB: COMMIT TRANSACTION
    Note right of DB: Cả hai bảng được lưu nguyên tử.<br/>Nếu 1 trong 2 lỗi -> Toàn bộ Rollback 100%!
    DB-->>-App: Commit Success
    App-->>-API: Result.Success()
    API-->>-User: 200 OK { message: "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu đã được gửi" }

    %% Giai đoạn 2: Worker Quét và Xử lý Thư
    Note over DB, SMTP: GIAI ĐOẠN 2: BACKGROUND WORKER QUÉT VÀ ĐIỀU PHỐI THƯ
    loop Chu kỳ Quét Nền (Mỗi 5 giây)
        Worker->>+DB: SELECT TOP(20) * FROM EmailOutboxMessages WITH (UPDLOCK)<br/>WHERE Status IN ('Queued', 'Retrying')<br/>AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc <= GETUTCDATE())<br/>.AsTracking()
        DB-->>-Worker: Trả về danh sách thư đủ điều kiện gửi
        
        alt Trường hợp 1: Token đã quá hạn (ExpiresAtUtc < UtcNow)
            Note over Worker: Kiểm tra Pre-Dispatch Expiry:<br/>Token đã hết hạn 15 phút trước khi đến lượt gửi!
            Worker->>DB: UPDATE EmailOutboxMessages<br/>SET Status = 'Failed', LastError = 'Pre-dispatch aborted: Reset token has expired.'
            Worker->>Worker: Ghi log cảnh báo (Đã lọc mặt nạ qua EmailLogMasker)
        else Trường hợp 2: SMTP Gặp Lỗi Tạm Thời (Transient Error / Timeout)
            Worker->>+SMTP: SendEmailAsync(MimeMessage)
            SMTP-->>-Worker: Lỗi kết nối Socket / TimeoutException / 421 Service Unavailable
            Note over Worker: Xác định lỗi tạm thời (Transient Fault).<br/>Tính toán Exponential Backoff: delay = 2^(attempt) * 5s<br/>(Lần 1: 10s, Lần 2: 20s, Lần 3: 40s)
            Worker->>DB: UPDATE EmailOutboxMessages<br/>SET Status = 'Retrying', AttemptCount = AttemptCount + 1,<br/>NextAttemptAtUtc = DATEADD(second, @delay, GETUTCDATE()),<br/>LastError = 'SmtpException: Connection timed out'
            Worker->>Worker: Ghi log Retry (Token được che: ***REDACTED***)
        else Trường hợp 3: Gửi Thành Công
            Worker->>+SMTP: SendEmailAsync(MimeMessage)
            SMTP-->>-Worker: 250 OK: Message queued for delivery
            Worker->>DB: UPDATE EmailOutboxMessages<br/>SET Status = 'Sent', SentAtUtc = GETUTCDATE(),<br/>AttemptCount = AttemptCount + 1, LastError = NULL
            Worker->>Worker: Ghi log hoàn tất giao dịch thư thành công
        end
        Worker->>DB: SaveChangesAsync()
    end
```

---

## 2. Các Đặc tính Kỹ thuật Trọng yếu (Design Highlights)

1. **Explicit Tracking (`.AsTracking()`)**:
   - `HrmDbContext` được cấu hình mặc định toàn cục `QueryTrackingBehavior.NoTracking` để tăng tối đa thông lượng đọc.
   - Do đó, `EmailOutboxWorker` bắt buộc phải gọi `.AsTracking()` một cách tường minh khi truy vấn các bản ghi `EmailOutboxMessages`, bảo đảm `SaveChangesAsync()` kích hoạt câu lệnh `UPDATE` trạng thái xuống SQL Server.
2. **Cơ chế Idempotency chống gửi trùng**:
   - Trường `IdempotencyKey` ngăn ngừa việc đưa hai thông báo cùng loại cho cùng một sự kiện vào cơ sở dữ liệu.
3. **Pre-dispatch Expiration Check**:
   - Nếu hàng đợi bị nghẽn (ví dụ mất điện hoặc đứt cáp Internet 30 phút), khi worker chạy lại, các email chứa link reset mật khẩu (hạn dùng 15 phút) sẽ bị hủy ngay trước khi gửi đi, ngăn ngừa gửi email chứa liên kết vô hiệu cho người dùng.
4. **Bảo mật Nhật ký Hệ thống (Log Sanitization)**:
   - Sử dụng bộ lọc `EmailLogMasker` với các biểu thức chính quy (Regex) tự động tìm và thay thế các chuỗi bí mật:
     ```csharp
     sanitized = Regex.Replace(rawContent, @"(token=|code=)[a-zA-Z0-9_\-]+", "$1***REDACTED***");
     ```

---

## 3. Liên kết Tài liệu Liên quan

- [Sơ đồ Kiến trúc Tổng thể](file:///d:/dotnet%20project/docs/diagrams/architecture-overview.md)
- [Hướng dẫn Vận hành Email](file:///d:/dotnet%20project/docs/operations/email.md)
- [Báo cáo Kiểm thử T22 - 8/8 Tests Pass trên Fake SMTP](file:///d:/dotnet%20project/docs/operations/email.md#4-kiem-thu-kiem-chung-fake-smtp)
