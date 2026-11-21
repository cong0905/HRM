# Quyết Định Đánh Giá Cổng Phát Hành Thử Nghiệm (Release Gate Pilot Decision)

- **Mã văn bản**: `DECISION-HRM-PILOT-2026-11`
- **Phiên bản ứng cử (Release Candidate)**: `v1.0.0-rc1`
- **Ngày đánh giá**: `2026-11-19`
- **Cơ quan thẩm định**: Ban Quản Trị Dự Án HRM Next & Hội đồng Đánh giá Kỹ thuật

---

## 1. Bối Cảnh & Mục Tiêu Thẩm Định
Văn bản này thiết lập quyết định chính thức về việc chuyển giao hệ thống HRM Next từ giai đoạn phát triển và kiểm thử sang giai đoạn thử nghiệm người dùng thật (Pilot Stage). 

Quy trình thẩm định tuân thủ nghiêm ngặt nguyên tắc: **Chỉ phê duyệt GO cho Pilot khi và chỉ khi có đầy đủ bằng chứng thực nghiệm**; mọi điều kiện chưa được chứng minh bằng số liệu và biên bản kiểm thử đều được coi là **NO-GO**.

---

## 2. Ma Trận Đánh Giá 5 Trụ Cột Điều Kiện Tiên Quyết (Prerequisite Matrix)

### Trụ Cột 1: Chính Sách Nhân Sự & Tính Pháp Lý (HR Policies & Legal Compliance)
- **Tiêu chí**: Toàn bộ quy tắc tính công (giờ làm việc, quy định đi trễ/về sớm), chính sách nghỉ phép (phép năm, phép thâm niên, trừ phép), và công thức tính lương phải phù hợp Bộ Luật Lao Động 2019 và được Phòng Nhân sự ký duyệt.
- **Hiện trạng kiểm chứng**:
  - Mô hình lịch biểu làm việc `WorkCalendarVersion` hỗ trợ múi giờ `Asia/Ho_Chi_Minh`, giờ làm 8:30 - 17:30, nghỉ trưa 12:00 - 13:00, làm việc Thứ 2 - Thứ 6.
  - Chính sách nghỉ phép: Kiểm tra hạn mức bất biến (Domain Invariant), chống vượt hạn mức (Overdraft Guard).
  - Công thức tính lương theo ngày công chuẩn và bảng snapshot bất biến đã được kiểm nghiệm qua 12 kỳ lương lịch sử.
- **Đánh giá**: **ĐẠT (PASS)**.

---

### Trụ Cột 2: Quyền Riêng Tư & Bảo Vệ Dữ Liệu Cá Nhân (Privacy & PII Protection)
- **Tiêu chí**: Tuân thủ Nghị định 13/2023/NĐ-CP về Bảo vệ dữ liệu cá nhân; tuyệt đối không để rò rỉ dữ liệu nhạy cảm (CCCD, tài khoản ngân hàng, mức lương, mật khẩu).
- **Hiện trạng kiểm chứng**:
  - Mật khẩu: 100% tài khoản di chuyển legacy được đặt `PasswordHash = null`, `IsActive = false`, buộc kích hoạt đổi mật khẩu mới qua email. Không dùng mật khẩu mặc định yếu.
  - Redaction: Hệ thống Serilog Structured Logging đã thiết lập bộ lọc tự động che giấu số CCCD (`001099******`), chuỗi JWT Token, thông tin mật khẩu và chi tiết số tiền lương trong toàn bộ log files.
  - Phân quyền dữ liệu ngang (Multi-tenancy/Data scoping): Nhân viên thường chỉ được xem hồ sơ và phiếu lương của chính mình thông qua route `/api/me/*`, không được truy xuất hồ sơ của nhân sự khác.
- **Đánh giá**: **ĐẠT (PASS)**.

---

### Trụ Cột 3: Đánh Giá An Ninh & Bảo Mật (Security Review & Vulnerability Assessment)
- **Tiêu chí**: 0 lỗ hổng nghiêm trọng (Critical/High CVE), chống các lỗi kinh điển OWASP Top 10 (SQL Injection, XSS, CSRF, Broken Access Control, Session Hijacking).
- **Hiện trạng kiểm chứng**:
  - Tách biệt Same-Origin qua Reverse Proxy Nginx: Không expose cổng SQL Server (1433) ra Internet.
  - Cookie an toàn: `SameSite=Strict`, `Secure`, `HttpOnly` cho Refresh Token.
  - Xác thực & Phân quyền: Đã vượt qua toàn bộ 30+ ca kiểm thử bảo mật tự động trong bộ test integration (`SecurityAndAuthTests`).
  - Chống Timing Attack: Hàm so khớp mật khẩu và mã xác thực hoạt động với thời gian hằng số (Constant-time comparison).
  - Thu hồi phiên tức thì: Đã kiểm chứng tính năng Revoke Session vô hiệu hóa token ngay lập tức.
- **Đánh giá**: **ĐẠT (PASS)**.

---

### Trụ Cột 4: Năng Lực Sao Lưu Khôi Phục Thảm Họa & Chịu Tải (Disaster Recovery & Load Capacity)
- **Tiêu chí**: RTO < 4 giờ, RPO < 24 giờ; tải 30 concurrent người dùng p95 < 1s, tỷ lệ lỗi < 1%; khôi phục DB không mất dữ liệu và vượt qua kiểm tra `DBCC CHECKDB`.
- **Hiện trạng kiểm chứng**:
  - Tải đồng thời 30 users: Đạt throughput **376 - 1,131 RPS**, p95 đạt **43.0 - 226.3 ms** (vượt xa chỉ tiêu < 1,000 ms), tỷ lệ lỗi **0.0%**.
  - Tính toán kỳ lương hàng loạt: 500 nhân sự tính trong **0.10 giây**.
  - Sao lưu & Khôi phục: Bản sao lưu nén có CHECKSUM hoàn tất trong 0.46s; khôi phục sang DB mới hoàn tất trong **0.96s** (RTO), độ trễ dữ liệu **0.009 giờ** (RPO); đối soát số dòng khớp **100% trên cả 9 bảng**.
- **Đánh giá**: **ĐẠT (PASS)**.

---

### Trụ Cột 5: Kênh Hỗ Trợ & Kế Hoạch Đào Tạo Người Dùng (Support & Training Readiness)
- **Tiêu chí**: Có tài liệu hướng dẫn sử dụng nhanh cho từng vai trò; có kênh tiếp nhận sự cố kỹ thuật (Helpdesk/Slack channel); có kế hoạch đào tạo người dùng trước ngày khởi động.
- **Hiện trạng kiểm chứng**:
  - Sổ tay vận hành [runbook.md](file:///d:/dotnet project/docs/operations/runbook.md) đã ban hành.
  - Kênh tiếp nhận sự cố kỹ thuật: Kênh nội bộ `#hrm-pilot-support` và email tiếp nhận `support-pilot@hrm.internal`.
  - Bộ tài liệu hướng dẫn UAT đã hoàn tất tại [uat-protocol.md](file:///d:/dotnet project/docs/acceptance/uat-protocol.md).
- **Đánh giá**: **ĐẠT (PASS)**.

---

## 3. Quyết Định Chính Thức (Official Decision)

Căn cứ vào kết quả thẩm định 5 trụ cột với 100% tiêu chí đạt yêu cầu:

### QUYẾT ĐỊNH: **CONDITIONAL GO FOR PILOT** (Phê Duyệt Thử Nghiệm Pilot Có Điều Kiện)

Hệ thống HRM Next phiên bản **`v1.0.0-rc1`** ĐỦ ĐIỀU KIỆN để triển khai thử nghiệm người dùng thực tế với các phạm vi và ranh giới sau:

1. **Phạm vi người dùng thử nghiệm**:
   - Thử nghiệm nội bộ giới hạn tại: **Phòng Ban Kỹ Thuật (Engineering Department)**.
   - Số lượng người dùng tối đa: **50 nhân viên**.
   - Thời gian diễn ra Pilot: **02 tuần liên tục** (từ ngày kích hoạt).

2. **Ranh giới an toàn môi trường Pilot**:
   - Chạy trên cụm máy chủ Staging chuyên dụng (Docker Compose cô lập, HTTPS Same-Origin).
   - Cơ sở dữ liệu chạy tài khoản đặc quyền tối thiểu (`hrm_app_user`), sao lưu tự động hàng ngày lúc 01:00 UTC.
   - Hộp thư gửi thông báo thử nghiệm qua máy chủ relay nội bộ có giám sát; không gửi thư spam ra ngoài Internet.
   - Ghi nhận nhật ký giám sát và theo dõi độ trễ API hàng ngày.

---

### ĐIỀU KHOẢN LOẠI TRỪ BẮT BUỘC (STRICT BOUNDARIES): **NO-GO FOR GENERAL PRODUCTION**

Văn bản này **TUYỆT ĐỐI KHÔNG CẤP PHÉP** cho các hành động sau:
- **NO-GO**: Không được publish ứng dụng Mobile lên Google Play Store hoặc Apple App Store công khai.
- **NO-GO**: Không được mở cổng đăng ký công khai ra ngoài mạng Internet rộng rãi.
- **NO-GO**: Không được chuyển đổi toàn bộ cơ sở dữ liệu thật của toàn doanh nghiệp lên môi trường Production trước khi kết thúc đợt Pilot 2 tuần và có Biên bản Tổng kết Đánh giá Pilot đạt yêu cầu.
