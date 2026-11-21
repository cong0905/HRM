# Hướng Dẫn Thiết Lập & Khôi Phục Môi Trường Demo Bảo Vệ (Demo Reset Guide)

Tài liệu cung cấp quy trình và lệnh thực thi chuẩn xác giúp người trình bày (diễn giả/sinh viên bảo vệ đồ án) chuẩn bị, làm sạch, và khôi phục môi trường demo trước và sau mỗi phiên bảo vệ.

---

## 1. Danh Sách Tài Khoản Mẫu Phục Vụ 5 Luồng Demo

Toàn bộ tài khoản demo đã được cấu hình sẵn trong tập dữ liệu chuẩn:

| Vai trò nghiệp vụ | Tên đăng nhập (Username) | Mật khẩu mặc định | Hồ sơ gắn liền | Luồng demo tương ứng |
|---|---|---|---|---|
| **SystemAdmin** | `admin` | `[mật khẩu demo cấp riêng]` | Quản trị viên hệ thống Phạm Quang Minh | Quản lý tài khoản, RBAC, kiểm tra Outbox & Thu hồi phiên |
| **HR Manager** | `hr_manager` | `[mật khẩu demo cấp riêng]` | Trưởng phòng Nhân sự Trần Thị Mai | Quản lý hồ sơ nhân viên, duyệt đơn nghỉ phép, cập nhật lương |
| **Payroll Specialist** | `payroll_specialist` | `[mật khẩu demo cấp riêng]` | Chuyên viên tính lương Lê Hoàng Nam | Tạo kỳ lương, tính toán Snapshot hàng loạt, phát hành phiếu lương |
| **Employee (Kỹ sư)** | `employee_01` | `[mật khẩu demo cấp riêng]` | Kỹ sư phần mềm Nguyễn Văn An | Xem bảng công, nộp đơn nghỉ phép, xem phiếu lương cá nhân |
| **Employee (Kiểm thử)** | `employee_02` | `[mật khẩu demo cấp riêng]` | Chuyên viên QA Lê Thị Bích | Kiểm thử nộp phép vượt hạn mức (chặn lỗi Invariant) |

---

## 2. Quy Trình Làm Sạch & Khởi Động Môi Trường Demo (Trước Phiên Bảo Vệ)

### Bước 1: Khởi động Hạ tầng Docker Compose Staging
Mở PowerShell tại thư mục gốc của repository:
```powershell
cd "d:\dotnet project\ops\staging"
docker compose down -v
docker compose up -d
```
Chờ khoảng 10 giây để Nginx Reverse Proxy, Backend API, Web UI và MailHog hoàn tất khởi tạo.

### Bước 2: Khôi phục Dữ liệu Demo Chuẩn từ Bản Snapshot Đã Kiểm Chứng
Thay vì phải tạo lại dữ liệu bằng tay, sử dụng script phục hồi tự động của T26 để nạp snapshot chuẩn vào cơ sở dữ liệu demo trong **dưới 2 giây**:
```powershell
cd "d:\dotnet project"
python ops/backup/restore.py `
  --server localhost `
  --backup-file "ops/backup/snapshots/HRM_Next_Benchmark_20261119_151004Z.bak" `
  --source-db HRM_Next_Benchmark `
  --restored-db HRM_Next_Staging
```

### Bước 3: Kiểm tra Trạng thái Sẵn sàng của Các Dịch vụ
- **Web Portal**: Truy cập `https://localhost` (Chấp nhận chứng chỉ tự ký).
- **Backend Health Check**: Truy cập `https://localhost/health/ready` (Kỳ vọng HTTP `200 OK`).
- **MailHog Hộp thư Demo**: Truy cập `http://localhost:8025` để xem các email kích hoạt và thông báo phiếu lương.

---

## 3. Quy Trình Khôi Phục Khẩn Cấp (Emergency Demo Reset in < 10s)

Nếu trong quá trình thao tác demo bị lỗi dữ liệu thao tác nhầm hoặc muốn diễn tập lại từ đầu cho hội đồng phản biện:

Chạy một dòng lệnh duy nhất để khôi phục trạng thái ban đầu:
```powershell
python ops/backup/restore.py --server localhost --backup-file "ops/backup/snapshots/HRM_Next_Benchmark_20261119_151004Z.bak" --restored-db HRM_Next_Staging
```
*Thời gian hoàn tất trung bình: ~1.0 giây, toàn bộ dữ liệu trở về trạng thái sạch sẽ hoàn toàn.*
