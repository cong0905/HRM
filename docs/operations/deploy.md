# Hướng dẫn Triển khai & Rollback Hệ thống (T25)

Tài liệu quy định quy trình đóng gói, cấu hình môi trường staging, pipeline CI/CD tự động và chiến lược phục hồi/rollback cho toàn bộ hệ thống HRM.

---

## 1. Kiến trúc Triển khai Môi trường Staging

Môi trường staging được đóng gói và vận hành qua Docker Compose với mô hình **Same-Origin Reverse Proxy** nhằm đảm bảo an toàn tuyệt đối cho Cookie (`SameSite=Strict`, `Secure`), chống CSRF và tối ưu hóa kết nối giữa Web, Mobile và Backend API.

```
                  ┌────────────────────────┐
                  │ Client (Web / Mobile)  │
                  └───────────┬────────────┘
                              │ HTTPS (Port 443 / TLS 1.2+)
                              ▼
        ┌──────────────────────────────────────────────┐
        │       hrm-staging-proxy (Nginx)              │
        │  - TLS Termination & HTTP->HTTPS Redirect   │
        │  - Security Headers: HSTS, CSP, X-Frame-Opt  │
        │  - X-Request-ID Trace ID Forwarding          │
        └───────┬──────────────────────────────┬───────┘
                │ Path: /                      │ Path: /api/, /health/, /openapi/
                ▼                              ▼
  ┌───────────────────────────┐  ┌───────────────────────────┐
  │     hrm-staging-web       │  │     hrm-staging-api       │
  │  - Nginx Static SPA       │  │  - .NET 10 API (Non-root) │
  │  - Multi-stage build      │  │  - Health probes (live/ready)
  │  - Cache: 1y for assets   │  │  - Structured JSON logs   │
  └───────────────────────────┘  └─────────────┬─────────────┘
                                               │ TCP 1433 (Mạng nội bộ cô lập)
                                               ▼
                                 ┌───────────────────────────┐
                                 │   hrm-staging-sqlserver   │
                                 │  - SQL Server 2022 Dev    │
                                 │  - KHÔNG MỞ PORT HOST     │
                                 │  - Least-privilege roles  │
                                 └───────────────────────────┘
```

### Các nguyên tắc an toàn bắt buộc:
1. **Không mở cổng Database ra ngoài**: Container `sqlserver` chỉ giao tiếp trong mạng nội bộ Docker (`hrm-internal: internal=true`), không expose port 1433 ra máy chủ vật lý hay Internet.
2. **Tách biệt quyền truy cập Database (Least-Privilege Roles)**:
   - **`hrm_app_user`** (API Runtime): Chỉ cấp quyền `db_datareader`, `db_datawriter` và `EXECUTE`. Tuyệt đối không cấp `db_ddladmin` hay `db_owner`. API không thể tự ý thay đổi cấu trúc bảng hay xóa cơ sở dữ liệu.
   - **`hrm_migrator`** (Database Migrator): Chỉ dùng trong bước migrate schema kiểm soát riêng biệt, có quyền `db_ddladmin`.
3. **Migration là bước kiểm soát riêng**:
   - Ứng dụng API khi khởi động **không tự động gọi `MigrateAsync()`** (đã cấu hình chặt chẽ trong `Program.cs`).
   - Bước migrate được thực thi qua container điều phối `db-migrator` hoàn tất trước khi container `api` khởi chạy.
4. **Không lưu trữ Secret trong mã nguồn/Image**: Mọi mật khẩu, private key và connection string đều nạp qua biến môi trường hoặc Secret Store ngoài repo.

---

## 2. Chuẩn bị & Cấu hình Staging

### 2.1. Yêu cầu tiên quyết
- Máy chủ Linux (Ubuntu 22.04 LTS / Debian 12) hoặc Windows Server có cài đặt:
  - Docker Engine 24.0+ & Docker Compose v2.20+
  - OpenSSL (để sinh chứng chỉ TLS cho staging)

### 2.2. Các bước thiết lập biến môi trường
Sao chép tệp mẫu và cập nhật các giá trị mật khẩu mạnh:
```bash
cp ops/staging/.env.staging.example ops/staging/.env.staging
```
Cập nhật các biến tối thiểu trong `.env.staging`:
- `MSSQL_SA_PASSWORD`: Mật khẩu quản trị SA (ít nhất 12 ký tự, đủ chữ hoa, thường, số, ký tự đặc biệt).
- `APP_DB_PASSWORD`: Mật khẩu tài khoản `hrm_app_user`.
- `MIGRATOR_PASSWORD`: Mật khẩu tài khoản `hrm_migrator`.
- `STAGING_HOST`: Tên miền staging (ví dụ: `staging.hrm.local`).

### 2.3. Sinh chứng chỉ số TLS cho Staging
Sử dụng script sinh chứng chỉ tự ký (Self-signed) kèm SAN cho staging:
- Trên Linux/macOS:
  ```bash
  bash ops/staging/generate-certs.sh
  ```
- Trên Windows PowerShell:
  ```powershell
  pwsh ops/staging/generate-certs.ps1
  ```
Chứng chỉ `staging.crt` và khóa bí mật `staging.key` sẽ được lưu trong thư mục `ops/staging/certs/` với phân quyền bảo mật 600.

---

## 3. Khởi chạy & Kiểm tra Nghiệm thu (Smoke Test)

### 3.1. Lệnh khởi chạy toàn bộ hệ thống
```bash
docker compose -f ops/staging/docker-compose.yml --env-file ops/staging/.env.staging up -d --build
```

### 3.2. Quy trình kiểm tra Smoke Test tự động
Sau khi các container ở trạng thái `healthy`, thực hiện các bước kiểm tra sau:

1. **Kiểm tra Liveness & Readiness của API**:
   ```bash
   curl -k -i https://localhost/health/live
   # Kỳ vọng: HTTP 200 {"status":"Healthy"}

   curl -k -i https://localhost/health/ready
   # Kỳ vọng: HTTP 200 {"status":"Healthy"} (Xác nhận SQL Server đã sẵn sàng)
   ```

2. **Kiểm tra Chuyển hướng HTTPS (Port 80 -> 443)**:
   ```bash
   curl -i http://localhost/
   # Kỳ vọng: HTTP 301 Moved Permanently -> https://localhost/
   ```

3. **Kiểm tra Cookie Bảo mật & CSRF Token**:
   ```bash
   curl -k -i https://localhost/api/v1/auth/csrf
   # Kỳ vọng: HTTP 200 kèm Set-Cookie "__Host-Hrm.Csrf" với Secure, HttpOnly, SameSite=Strict
   ```

4. **Kiểm tra Web SPA & Static Assets**:
   ```bash
   curl -k -i https://localhost/
   # Kỳ vọng: HTTP 200 HTML content, Header: Content-Security-Policy, X-Frame-Options: DENY
   ```

5. **Kiểm tra Hộp thư Thử nghiệm Staging (MailHog)**:
   - Truy cập giao diện web: `http://<STAGING_IP>:8025/`
   - Xác nhận email đặt lại mật khẩu từ API được hứng cục bộ, có nhãn `[DEV SINK]`, không bị gửi ra ngoài Internet.

---

## 4. Pipeline Tích hợp Liên tục (CI Pipeline)

Quy trình tự động hóa được định nghĩa trong `.github/workflows/ci.yml` kích hoạt trên mỗi commit hoặc Pull Request:

| Nhánh công việc | Mô tả | Tiêu chuẩn Đạt (Gate) |
|---|---|---|
| `build-and-test` | Kiểm tra toàn bộ mã legacy, restore locked dependencies, chạy test hồi quy T04 trên Windows. | 100% test PASS, 0 package vulnerability mức high trở lên. |
| `backend-sql-contract` | Khởi tạo SQL Server 2022 container độc lập, chạy kiểm thử tích hợp T16, kiểm tra drift OpenAPI, chạy bộ test Email Outbox (T22). | Toàn bộ SQL Integration Tests PASS, schema contract đồng bộ. |
| `web-checks` | Cài đặt locked npm, typecheck TypeScript, lint ESLint, unit/component tests, build production bundle. | 0 TypeScript error, 0 lint warning/error, test PASS. |
| `mobile-checks` | Cài đặt locked npm, typecheck React Native TypeScript, chạy toàn bộ 49 mobile unit/integration tests. | 0 lỗi kiểu dữ liệu, 49/49 mobile tests PASS. |
| `docker-packaging` | Đóng gói kiểm thử Dockerfile cho cả Backend API và Web Frontend bằng Docker Buildx. | Image build thành công, multi-stage hoàn tất. |

---

## 5. Chiến lược Diễn tập Rollback (Rollback Rehearsal)

Hệ thống tuân thủ nguyên tắc **Tương thích ngược (Backward Compatibility)** theo mô hình **Expand / Contract**:
- Mã ứng dụng mới luôn tương thích với cấu trúc cơ sở dữ liệu của phiên bản trước đó.
- Không bao giờ xóa hoặc đổi tên cột đang dùng trong một bước duy nhất.

### 5.1. Kịch bản 1: Rollback Mã ứng dụng (Application Code Rollback)
Nếu phiên bản ứng dụng mới gặp lỗi nghiệp vụ nghiêm trọng nhưng không thay đổi cấu trúc dữ liệu:
1. Thay đổi thẻ phiên bản (Image Tag) trong `docker-compose.yml` về phiên bản ổn định trước đó (ví dụ: `hrm-api:v1.0.0` thay vì `hrm-api:v1.1.0`).
2. Khởi động lại service API:
   ```bash
   docker compose -f ops/staging/docker-compose.yml up -d api web
   ```
3. Do dữ liệu và schema tương thích ngược, quá trình rollback hoàn tất trong **dưới 30 giây** mà **không mất mát dữ liệu**.

### 5.2. Kịch bản 2: Phục hồi Cơ sở dữ liệu (Database Schema Recovery)
Trong trường hợp một migration mới cần thu hồi:
1. **Sử dụng EF Core Down Migration có kiểm soát**:
   ```bash
   # Chạy migrator lùi về phiên bản mong muốn
   docker compose -f ops/staging/docker-compose.yml run --rm db-migrator \
     dotnet ef database update <TargetPreviousMigrationName> --connection "$MIGRATION_CONNECTION_STRING"
   ```
2. **Khôi phục từ Pre-cutover Backup (Nếu rollback migration thất bại)**:
   - Trước mỗi lần phát hành lớn, quy trình sao lưu tự động tạo snapshot:
     ```bash
     bash ops/backup/backup.sh pre-deploy-v1.1.0
     ```
   - Khi cần khôi phục khẩn cấp:
     ```bash
     bash ops/backup/restore.sh pre-deploy-v1.1.0
     ```
3. Sau khi phục hồi DB, đưa phiên bản API tương ứng lên chạy lại và thực thi smoke test.

---

## 6. Nhật ký Vận hành & Giám sát

### 6.1. Xem nhật ký tập trung có gắn Trace ID
```bash
# Xem log Nginx kèm request_id
docker logs -f hrm-staging-proxy

# Xem log Backend API kèm traceId và errorCode
docker logs -f hrm-staging-api
```

### 6.2. Kiểm tra trạng thái tài nguyên
```bash
docker compose -f ops/staging/docker-compose.yml ps
docker stats
```
