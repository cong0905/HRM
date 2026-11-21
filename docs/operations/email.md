# Vận hành Email & Transactional Outbox (T22)

Tài liệu hướng dẫn cấu hình, vận hành và giám sát hệ thống gửi email qua mô hình **Transactional Outbox** trong HRM.

---

## 1. Kiến trúc Outbox & Nguyên lý hoạt động

Nhằm đảm bảo **tính toàn vẹn giao dịch** và **độ tin cậy vận hành**, hệ thống không thực hiện gọi SMTP trực tiếp đồng bộ trong các luồng xử lý HTTP request (như Quên mật khẩu, Kích hoạt tài khoản).

```
[Client / User]
       │ HTTP POST /auth/forgot-password
       ▼
┌──────────────────────────────────────────────┐
│  API Transaction (Unit of Work)              │
│  1. Xác thực tài khoản & sinh token ASP.NET │
│  2. Ghi bản ghi vào [notifications].[EmailOutboxMessages]
│     - Status = 'Queued'                      │
│     - ExpiresAtUtc = UtcNow + 15 phút       │
│     - IdempotencyKey (chống trùng lặp)       │
│  3. Commit DB Transaction                    │
└──────────────────────────────────────────────┘
       │ Trả về HTTP 202 Accepted ngay lập tức
       ▼
┌──────────────────────────────────────────────┐
│  EmailOutboxWorker (Background Service)      │
│  - Poll định kỳ mỗi N giây                   │
│  - Kiểm tra hạn token (ExpiresAtUtc)         │
│  - Gửi qua IEmailSender (SMTP / DevSink)    │
│  - Thành công ──► Status = 'Sent', SentAtUtc │
│  - Lỗi tạm thời ──► Backoff lũy thừa, Retry   │
│  - Quá MaxAttempts ──► Status = 'Failed'     │
└──────────────────────────────────────────────┘
```

### Các trạng thái của Outbox Message (`Status`)
- **`Queued`**: Bản ghi đã được ghi thành công trong giao dịch nghiệp vụ, đang chờ hoặc đang trong lịch retry gửi.
- **`Sent`**: Email đã được máy chủ SMTP tiếp nhận thành công (trả mã 250 OK) hoặc sink ghi nhận. Có timestamp `SentAtUtc`.
- **`Failed`**: Bản ghi thất bại vĩnh viễn (do lỗi không thể retry như mã 5xx, hoặc đã vượt quá `MaxAttempts`, hoặc token đã hết hạn trước khi worker kịp gửi). Có chi tiết `LastError` đã che giấu thông tin nhạy cảm.

---

## 2. Bảng cấu hình môi trường & Tham số

Cấu hình trong file `appsettings.json` hoặc biến môi trường với tiền tố `Email__`:

| Tham số | Biến môi trường | Mặc định | Ý nghĩa |
|---|---|---|---|
| `DeliveryMode` | `Email__DeliveryMode` | `dev-sink` | Chế độ gửi: `smtp`, `dev-sink`, `test-sink`, `not-configured` |
| `ResetUrlOrigin` | `Email__ResetUrlOrigin` | `https://localhost:5001` | Origin gốc cho link đặt lại mật khẩu trong email |
| `ResetTokenLifespanMinutes` | `Email__ResetTokenLifespanMinutes` | `15` | Thời hạn hiệu lực của link/token đặt lại mật khẩu (phút) |
| `SmtpHost` | `Email__SmtpHost` | `127.0.0.1` | Địa chỉ máy chủ SMTP |
| `SmtpPort` | `Email__SmtpPort` | `25` | Cổng máy chủ SMTP (25, 587, 465) |
| `EnableSsl` | `Email__EnableSsl` | `false` | Bật STARTTLS / SSL khi kết nối SMTP |
| `SmtpUsername` | `Email__SmtpUsername` | `null` | Tên người dùng xác thực SMTP |
| `SmtpPassword` | `Email__SmtpPassword` | `null` | Mật khẩu xác thực SMTP (lấy từ Secret Store) |
| `FromAddress` | `Email__FromAddress` | `noreply@hrm.local` | Địa chỉ email người gửi |
| `FromName` | `Email__FromName` | `HRM Notification` | Tên hiển thị người gửi |
| `SmtpTimeoutMilliseconds` | `Email__SmtpTimeoutMilliseconds` | `5000` | Thời gian timeout kết nối SMTP (ms) |
| `MaxRetryAttempts` | `Email__MaxRetryAttempts` | `5` | Số lần gửi lại tối đa trước khi đánh dấu `Failed` |
| `BackoffInitialSeconds` | `Email__BackoffInitialSeconds` | `10` | Thời gian chờ retry ban đầu (giây) |
| `BackoffMaxSeconds` | `Email__BackoffMaxSeconds` | `3600` | Thời gian chờ tối đa giữa các lần retry (giây) |
| `WorkerPollIntervalSeconds` | `Email__WorkerPollIntervalSeconds` | `5` | Chu kỳ quét bảng outbox của background worker (giây) |

---

## 3. Chính sách an toàn & Chống thất thoát dữ liệu

### 3.1. Ràng buộc Production an toàn (Anti-Silent-NullSender)
- Trong môi trường `Production` (`ASPNETCORE_ENVIRONMENT=Production`):
  - `EmailOptionsValidator` sẽ **chặn đứng quá trình khởi động** (Throw on startup) nếu cấu hình email để `dev-sink`, `not-configured` hoặc thiếu `SmtpHost`.
  - Hệ thống **không bao giờ âm thầm chuyển sang NullEmailSender** mà không có cảnh báo.

### 3.2. Che giấu Token & PII trong Nhật ký (Log Redaction)
- **Không bao giờ log Body email**: Nhật ký hệ thống chỉ ghi nhận `MessageId`, `Purpose`, `Recipient` đã được che (`u***@domain.com`).
- **Khử trùng lỗi (`LastError`)**: Mọi tham số nhạy cảm trong URL hoặc thông báo ngoại lệ (như `code=...`, `token=...`, `password=...`, `Bearer ...`) đều được bộ lọc `EmailLogMasker` thay thế bằng `[REDACTED]`.

### 3.3. Chống sử dụng Token hết hạn gửi muộn
- Mỗi email đặt lại mật khẩu lưu `ExpiresAtUtc` tương ứng với thời hạn backend (15 phút).
- Khi worker xử lý một thông điệp bị dồn ứ (do SMTP downtime trước đó), nếu phát hiện `UtcNow > ExpiresAtUtc`, worker sẽ **từ chối gửi ra ngoài**, chuyển trạng thái sang `Failed` kèm lý do:
  > *"Message expired before delivery."*
- Giúp người dùng không nhận được các email chứa token đã chết, tránh nhầm lẫn và rủi ro bảo mật.

### 3.4. Cơ chế Backoff lũy thừa (Exponential Backoff)
- Khi gặp lỗi tạm thời (Timeout, mạng chập chờn, SMTP 421):
  $$\Delta t = \min(\text{BackoffInitialSeconds} \times 2^{\text{AttemptCount} - 1}, \text{BackoffMaxSeconds})$$
- Tránh dồn dập tấn công máy chủ SMTP khi đang có sự cố gián đoạn.

---

## 4. Chế độ Dev Sink (Môi trường Phát triển)

Trong môi trường cục bộ hoặc staging test:
- Thiết lập `Email__DeliveryMode = "dev-sink"`.
- Mọi email phát sinh không gửi ra Internet mà được hứng lại vào `DevSinkInbox` (in-memory buffer).
- Tiêu đề thư được tiền tố tự động: `[DEV SINK] Yêu cầu đặt lại mật khẩu HRM`.
- Nội dung thư đính kèm cảnh báo:
  ```
  --- [DEV SINK - TEST NOTICE: THIS EMAIL WAS CAPTURED LOCALLY AND NOT SENT TO REAL USER] ---
  ```

---

## 5. Truy vấn giám sát vận hành (SQL Runbook)

### 5.1. Kiểm tra số lượng thư tồn đọng (Backlog)
```sql
SELECT Status, COUNT(*) AS TotalCount, MIN(CreatedAtUtc) AS OldestMessage
FROM [notifications].[EmailOutboxMessages]
GROUP BY Status;
```

### 5.2. Danh sách các thư gửi thất bại cần kiểm tra
```sql
SELECT Id, [To], Purpose, AttemptCount, LastError, CreatedAtUtc, ExpiresAtUtc
FROM [notifications].[EmailOutboxMessages]
WHERE Status = 'Failed'
ORDER BY CreatedAtUtc DESC;
```

### 5.3. Kích hoạt gửi lại thủ công một thông điệp thất bại
```sql
UPDATE [notifications].[EmailOutboxMessages]
SET Status = 'Queued', AttemptCount = 0, NextAttemptAtUtc = SYSUTCDATETIME(), LastError = NULL
WHERE Id = 'YOUR-MESSAGE-GUID-HERE';
```
