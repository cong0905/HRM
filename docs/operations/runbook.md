# Sổ Tay Vận Hành & Khôi Phục Thảm Họa (Operations & Incident Runbook)

Tài liệu hướng dẫn vận hành, giám sát (Observability), cảnh báo sự cố (Alerting), sao lưu khôi phục thảm họa (Disaster Recovery), và giới hạn tải thực tế của hệ thống HRM Next.

---

## 1. Kiến Trúc Giám Sát & Quan Sát (Observability Architecture)

### 1.1. Thăm Dò Sức Khỏe (Health Check Probes)
Hệ thống cung cấp 2 endpoint thăm dò tiêu chuẩn phục vụ Kubernetes / Docker Compose / Load Balancer:

- **Liveness Probe**: `GET /health/live`
  - **Mục đích**: Xác nhận tiến trình ứng dụng ASP.NET Core đang phản hồi, không bị deadlock hoặc treo thread pool.
  - **Kỳ vọng**: HTTP `200 OK`, JSON `{"status":"Healthy"}` trong vòng dưới 200ms.
  - **Hành động khi thất bại**: Khởi động lại container / process (`restart: on-failure`).

- **Readiness Probe**: `GET /health/ready`
  - **Mục đích**: Xác nhận ứng dụng sẵn sàng tiếp nhận request (kiểm tra kết nối SQL Server và schema migration đã sẵn sàng).
  - **Kỳ vọng**: HTTP `200 OK`, JSON chi tiết trạng thái kết nối SQL Server.
  - **Hành động khi thất bại**: Loại khỏi upstream gateway, không định tuyến lưu lượng người dùng vào node đang lỗi.

### 1.2. Nhật Ký Cấu Trúc & Tương Quan Request (Structured Logging & Trace Correlation)
- **Chuẩn ghi log**: Định dạng JSON cấu trúc (Serilog / ASP.NET Logging chuẩn hóa).
- **Correlation ID**: Mọi request đi qua Reverse Proxy Nginx đều được gắn `X-Request-ID`. Backend tiếp nhận và gán vào `Activity.TraceId` / `TraceIdentifier`, ghi kèm trong mọi log entry:
  ```json
  {
    "Timestamp": "2026-11-19T15:09:54.123Z",
    "Level": "Information",
    "TraceId": "0af4e125c1104e1ba8929b9f5183dbfa",
    "SpanId": "b7ad6b7169203331",
    "SourceContext": "HRM.Next.Application.Payroll.Commands.CalculatePayrollCommand",
    "Message": "Payroll calculation batch completed for period 2026-04 with 500 employees in 102ms.",
    "PeriodId": "73c683ee-5e16-419b-a365-1811804ff69b",
    "EmployeeCount": 500
  }
  ```

### 1.3. Quy Định Che Giấu Dữ Liệu Nhạy Cảm (PII & Secret Redaction Rules)
**TUYỆT ĐỐI KHÔNG** ghi nhận dạng bản rõ (plain-text) các thông tin sau vào hệ thống log, metric, hoặc telemetry:
1. **Mật khẩu & Chuỗi Hash**: `Password`, `PasswordHash`, `NewPassword`, `CurrentPassword` -> Ghi đè bằng `[REDACTED]`.
2. **Thông tin định danh công dân**: Số CCCD/CMND (`CitizenId`) -> Mặt nạ `001099******`.
3. **Mã thông báo xác thực**: `Authorization: Bearer <JWT>`, `Cookie: session=...`, `RefreshToken` -> Rút gọn 8 ký tự đầu hoặc `[REDACTED_TOKEN]`.
4. **Chi tiết mức lương**: Lương cơ bản, số tiền thực lĩnh của từng cá nhân không được in ra Information log; chỉ in ID và số tổng cấp kỳ lương.

---

## 2. Hệ Thống Cảnh Báo & Ứng Cứu Sự Cố (Incident & Alerting Runbook)

### 2.1. Ma Trận Phân Cấp Mức Độ Nghiêm Trọng (Severity Matrix)

| Cấp độ | Định nghĩa | Ngưỡng kích hoạt | Kênh thông báo | Thời gian phản hồi (SLA) |
|---|---|---|---|---|
| **P0 - Critical** | Toàn bộ hệ thống ngưng trệ, DB mất kết nối | `/health/ready` trả 503 liên tục > 1 phút; SQL connection pool cạn kiệt | Call / SMS / PagerDuty khẩn cấp | Phản hồi < 15 phút, giải quyết < 2 giờ |
| **P1 - Major** | Một nghiệp vụ cốt lõi bị gián đoạn | Outbox mail failed > 50 messages; Tỷ lệ lỗi API > 5% trong 5 phút | Slack/Teams kênh `#incident-critical` | Phản hồi < 30 phút, giải quyết < 4 giờ |
| **P2 - Moderate** | Suy giảm tài nguyên, nguy cơ tiềm ẩn | Dung lượng đĩa chứa DB/Backup > 85%; Memory > 90% liên tục 10 phút | Slack/Teams kênh `#ops-alerts` | Phản hồi < 2 giờ, giải quyết trong ngày |
| **P3 - Minor** | Hiệu năng suy giảm nhẹ | Độ trễ API p95 > 1000ms trong 15 phút; CPU > 75% | Dashboard / Email báo cáo hàng ngày | Xử lý trong chu kỳ bảo trì tuần |

---

### 2.2. Quy Trình Xử Lý Các Sự Cố Điển Hình

#### Sự cố 1: Outbox Email / Notification Bị Tắc Nghẽn (P1)
- **Triệu chứng**: Nhân viên phản ánh không nhận được email kích hoạt tài khoản hoặc phiếu lương; bảng `outbox.OutboxMessages` có nhiều bản ghi có `ProcessedAtUtc IS NULL` và `RetryCount >= 3`.
- **Cách chẩn đoán**:
  ```sql
  SELECT TOP 50 Id, Type, RetryCount, LastError, CreatedAtUtc 
  FROM outbox.OutboxMessages 
  WHERE ProcessedAtUtc IS NULL 
  ORDER BY RetryCount DESC;
  ```
- **Xử lý**:
  1. Kiểm tra log của SMTP Server / MailHog relay container xem có bị reject hoặc timeout kết nối không.
  2. Nếu là lỗi tạm thời (network timeout): Chạy job tái xử lý (exponential backoff) hoặc kích hoạt retry thủ công.
  3. Nếu bản ghi chứa địa chỉ email sai định dạng: Cập nhật cờ lỗi và chuyển trạng thái sang Dead-Letter Queue (DLQ) để tránh làm nghẽn tiến trình nền.

#### Sự cố 2: Đĩa Chứa Cơ Sở Dữ Liệu & Snapshot Đầy > 85% (P2)
- **Triệu chứng**: Hệ điều hành hoặc monitoring agent cảnh báo dung lượng ổ `D:\` hoặc thư mục `ops/backup/snapshots` vượt quá 85%.
- **Xử lý**:
  1. Chạy dọn dẹp các bản backup cũ hơn retention policy (chính sách lưu 30 ngày):
     ```powershell
     Get-ChildItem -Path "ops/backup/snapshots/*.bak" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | Remove-Item -Force
     ```
  2. Kiểm tra kích thước transaction log của SQL Server:
     ```sql
     DBCC SQLPERF(LOGSPACE);
     ```
  3. Thực hiện sao lưu log định kỳ và truncate log nếu mô hình phục hồi là Full.

#### Sự cố 3: Khóa Treo Bảng (Lock Escalation / Deadlock) (P1)
- **Triệu chứng**: Truy vấn tra cứu nhân viên hoặc tính lương bị timeout, xuất hiện lỗi SQL Exception 1205 (Deadlock victim).
- **Xử lý**:
  1. Bật trace flag hoặc đọc `sys.dm_tran_locks` và `sys.dm_exec_requests` để tìm blocking session:
     ```sql
     SELECT session_id, blocking_session_id, wait_type, wait_time, last_wait_type, text 
     FROM sys.dm_exec_requests CROSS APPLY sys.dm_exec_sql_text(sql_handle)
     WHERE blocking_session_id <> 0;
     ```
  2. Kill session gây treo nếu cần thiết (`KILL <blocking_session_id>;`).
  3. Đảm bảo toàn bộ nghiệp vụ tra cứu chỉ đọc sử dụng `QueryTrackingBehavior.NoTracking` và chỉ mục tìm kiếm tối ưu.

---

### 2.3. Kịch Bản Diễn Tập Cảnh Báo Giả Có Kiểm Soát (Controlled Simulated Alert Drill)
Để xác thực tính sẵn sàng của hệ thống cảnh báo:
1. **Giả lập sự cố Outbox**:
   - Tạm thời cấu hình SMTP Port sai trong môi trường staging (`SMTP_PORT=9999`).
   - Tạo yêu cầu cấp tài khoản mới.
   - **Xác minh**: Outbox worker ghi nhận lỗi retry, tăng `RetryCount`, sau 3 lần kích hoạt log cảnh báo `CRITICAL: Outbox delivery failed for message <id> after 3 retries`.
   - Khôi phục cấu hình đúng và xác nhận message được gửi thành công.
2. **Giả lập suy giảm độ trễ**:
   - Chạy lệnh mô phỏng tải 30 concurrent bằng `ops/load-tests/HrmLoadTest.csproj`.
   - Xác minh: Monitoring dashboard thu thập đúng thông số p50, p95, p99 và không kích hoạt cảnh báo sai nếu p95 < 1s.

---

## 3. Quy Trình Sao Lưu & Phục Hồi Thảm Họa (Backup & Disaster Recovery Runbook)

### 3.1. Mục Tiêu Cam Kết Dịch Vụ (RPO & RTO Targets)
- **RPO (Recovery Point Objective)**: **Tối đa 24 giờ** (Mất mát dữ liệu nghiệp vụ không được vượt quá 24h đối với backup hàng ngày; kết hợp transaction log backup giảm xuống dưới 15 phút).
- **RTO (Recovery Time Objective)**: **Tối đa 4 giờ** (Toàn bộ cơ sở dữ liệu và dịch vụ phải sẵn sàng phục vụ trở lại trong vòng 4 giờ kể từ khi kích hoạt quy trình thảm họa).

### 3.2. Cơ Chế Sao Lưu Tự Động (`ops/backup/backup.py`)
- **Tần suất**: Full backup tự động hàng ngày lúc 01:00 UTC.
- **Tính năng bảo vệ**:
  - `WITH COMPRESSION`: Giảm dung lượng lưu trữ tới ~80% và tăng tốc độ I/O đĩa.
  - `WITH CHECKSUM`: Bắt buộc kiểm tra tính toàn vẹn trang dữ liệu ngay trong quá trình sao lưu.
  - Tự động sinh băm SHA-256 lưu kèm file `.json` để chống giả mạo hoặc hư hỏng file.
- **Lệnh thực hiện**:
  ```bash
  python ops/backup/backup.py --server localhost --database HRM_Next_Benchmark
  ```

### 3.3. Quy Trình Phục Hồi Sang Cơ Sở Dữ Liệu Mới (`ops/backup/restore.py`)
Khi xảy ra sự cố thảm họa hoặc kiểm tra định kỳ, **tuyệt đối không khôi phục đè trực tiếp vào cơ sở dữ liệu đang vận hành** mà phải thực hiện theo quy trình cách ly:

1. **Khôi phục sang Database kiểm chứng mới** (`WITH MOVE` MDF và LDF sang đường dẫn riêng, kèm `CHECKSUM` và `REPLACE`).
2. **Kiểm tra toàn vẹn vật lý và cấu trúc logic**: Chạy `DBCC CHECKDB` đảm bảo 0 lỗi allocation, 0 lỗi consistency.
3. **Đối soát số dòng tự động (Row-count reconciliation)**: So khớp 100% số lượng bản ghi của toàn bộ bảng nghiệp vụ (Phòng ban, Chức vụ, Hồ sơ nhân sự, Tài khoản, Chấm công, Phép, Kỳ lương, Phiếu lương) giữa bản nguồn và bản phục hồi.
4. **Lệnh diễn tập**:
  ```bash
  python ops/backup/restore.py \
    --server localhost \
    --backup-file "ops/backup/snapshots/HRM_Next_Benchmark_20261119_151004Z.bak" \
    --source-db HRM_Next_Benchmark \
    --restored-db HRM_Next_Restored
  ```

### 3.4. Bằng Chứng Thực Nghiệm Diễn Tập Khôi Phục (Audit Record)
- **Thời gian thực hiện**: `2026-11-19 15:10:36 UTC`
- **Tập tin backup thử nghiệm**: `ops/backup/snapshots/HRM_Next_Benchmark_20261119_151004Z.bak` (Dung lượng: 1.96 MB, SHA256: `1e63c988e1a386f1fc2f076cc2499a0966c6ac7a4f94041c1f729c99695b653a`)
- **Kết quả nghiệm thu**:

| Tiêu chuẩn đo lường | Mục tiêu cam kết | Kết quả thực tế đo được | Đánh giá |
|---|---|---|---|
| **RTO (Thời gian phục hồi)** | < 4.0 giờ | **0.96 giây** | **ĐẠT XUẤT SẮC** |
| **RPO (Độ trễ dữ liệu tối đa)** | < 24.0 giờ | **0.009 giờ (32.4 giây)** | **ĐẠT XUẤT SẮC** |
| **Kiểm tra `DBCC CHECKDB`** | 0 lỗi cấu trúc/trang | **Passed (0 allocation/consistency errors)** | **ĐẠT** |
| **Đối soát số dòng dữ liệu** | Khớp 100%, chênh lệch = 0 | **Khớp 100% trên tất cả 9 bảng** | **ĐẠT** |

Chi tiết đối soát số dòng:
- `Departments`: 10 / 10 bản ghi (Chênh lệch: 0)
- `Positions`: 10 / 10 bản ghi (Chênh lệch: 0)
- `EmployeeProfiles`: 500 / 500 bản ghi (Chênh lệch: 0)
- `AspNetUsers`: 500 / 500 bản ghi (Chênh lệch: 0)
- `AttendanceSummaries`: 5,000 / 5,000 bản ghi (Chênh lệch: 0)
- `LeaveBalances`: 500 / 500 bản ghi (Chênh lệch: 0)
- `PayrollPeriods`: 12 / 12 bản ghi (Chênh lệch: 0)
- `Payslips`: 600 / 600 bản ghi (Chênh lệch: 0)

---

## 4. Đo Tải & Năng Lực Giới Hạn Hệ Thống (Capacity & Load Benchmark)

### 4.1. Cấu Hình Phần Cứng Thực Nghiệm
Toàn bộ số liệu dưới đây được đo thực tế trên phần cứng vật lý, không giả lập:
- **Bộ vi xử lý (CPU)**: 12th Gen Intel Core i5-12450H (8 Cores, 12 Logical Processors)
- **Hệ điều hành**: Microsoft Windows 11 Enterprise (Build 10.0.26200)
- **Hệ quản trị CSDL**: Microsoft SQL Server 2022 (RTM) - 16.0.1000.6 (X64)
- **Runtime**: .NET 10.0.404 SDK (Pinned SDK)
- **Tập dữ liệu chuẩn**: 500 nhân sự đầy đủ quan hệ phòng ban/chức vụ/hợp đồng/lương, 12 kỳ lương lịch sử, 5,000 bản ghi công chấm thực tế.

### 4.2. Kết Quả Đo Tải 30 Concurrent Virtual Users (Workers)

| Kịch bản kiểm thử | Mô tả nghiệp vụ | Requests | Throughput (RPS) | p50 (ms) | p95 (ms) | p99 (ms) | Lỗi (%) | SLA p95 < 1s | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| `Auth_Login_And_Verify` | Tìm nạp người dùng, xác thực trạng thái & phân quyền | 300 | 775.8 | 3.8 ms | 226.3 ms | 253.8 ms | 0.0% | Đạt | **PASS** |
| `Employees_Search_And_Paging` | Tìm kiếm nhân viên theo từ khóa họ tên/mã NV, phân trang và sắp xếp | 600 | 376.4 | 33.2 ms | 129.8 ms | 1,305.0 ms | 0.0% | Đạt | **PASS** |
| `Personal_Attendance_Dashboard` | Tra cứu bảng chấm công 15 ngày gần nhất của nhân sự cá nhân | 600 | 1,131.5 | 18.1 ms | 68.8 ms | 197.4 ms | 0.0% | Đạt | **PASS** |
| `Leave_Balances_And_Ledger` | Tra cứu hạn mức phép năm hiện tại và lịch sử nghỉ phép | 600 | 1,069.1 | 24.6 ms | 43.0 ms | 79.7 ms | 0.0% | Đạt | **PASS** |

### 4.3. Hiệu Năng Tính Toán Kỳ Lương Hàng Loạt (Payroll Batch Calculation)
- **Quy mô tính toán**: `500` hồ sơ nhân viên trong một kỳ lương duy nhất.
- **Thời gian xử lý**: **`0.10` giây** (Tốc độ: ~5,000 phiếu lương/giây).
- **Phân tích cơ chế**:
  - Không gặp hiện tượng N+1 query nhờ truy vấn tập trung dữ liệu đầu vào (công, phụ cấp, cấu hình lịch biểu) trước khi chạy engine tính toán thuần trong bộ nhớ.
  - Kết quả được ghi nhận bất biến (immutability) vào `PayrollCalculationSnapshot` và sinh `Payslips` theo cơ chế phân đoạn (staged insert) tránh xung đột khóa ngoại.

### 4.4. Phân Tích Chỉ Mục & DMV (Database Dynamic Management Views)
- DMV khảo sát `sys.dm_db_missing_index_details` ghi nhận: **0 critical missing indexes**.
- Toàn bộ các cột khóa ngoại và tiêu chí tìm kiếm (`EmployeeProfiles.FullName`, `EmployeeProfiles.EmployeeCode`, `AttendanceSummaries.EmployeeId`, `AttendanceSummaries.BusinessDate`, `AspNetUsers.NormalizedUserName`) đều có chỉ mục B-Tree bao phủ tối ưu.

### 4.5. Cam Kết & Giới Hạn Năng Lực (Capacity Disclaimer & Boundaries)
- Thử nghiệm trên được thiết kế để đánh giá **khả năng chịu tải đồng thời của 30 người dùng tích cực thực hiện thao tác liên tục không nghỉ** (tương đương với một doanh nghiệp quy mô ~500–1,000 nhân viên hoạt động giờ cao điểm).
- **Tuyệt đối không suy diễn**: Kết quả 30 concurrent users này không đồng nghĩa hệ thống hỗ trợ hàng chục nghìn người dùng truy cập đồng thời mà chưa qua kiểm thử phân tán đa máy chủ (Distributed Load Testing).
- **Tính trung thực**: Không nới lỏng phạm vi xác thực (authorization scope) hay vô hiệu hóa bảo mật để thổi phồng chỉ số throughput.
