# CHƯƠNG 1: TỔNG QUAN VÀ HIỆN TRẠNG HỆ THỐNG LEGACY

## 1.1. Bối cảnh và Tính cấp thiết của Đề tài
Quản trị nguồn nhân lực (Human Resource Management - HRM) là phân hệ cốt lõi trong mọi tổ chức, doanh nghiệp, bao gồm các hoạt động trọng yếu: quản lý hồ sơ nhân viên, theo dõi chấm công hàng ngày, giải quyết chế độ nghỉ phép và tính toán chi trả lương. Sự chính xác, minh bạch và an toàn của dữ liệu nhân sự - tiền lương tác động trực tiếp đến quyền lợi người lao động, nghĩa vụ thuế, bảo hiểm và sự ổn định vận hành của tổ chức.

Trước khi thực hiện đề tài, hệ thống quản trị nhân sự hiện hữu của đơn vị được xây dựng trên nền tảng ứng dụng desktop (.NET Framework WinForms client-server 3 tầng: `HRM.GUI` -> `HRM.BLL` -> `HRM.DAL`). Mặc dù hệ thống này đã phục vụ nghiệp vụ nội bộ trong một thời gian dài, nhưng trước sự phát triển của công nghệ và mô hình làm việc kết hợp (hybrid/remote), hệ thống bộc lộ những hạn chế nghiêm trọng về kiến trúc, bảo mật và tính toàn vẹn dữ liệu.

---

## 1.2. Phân tích Hiện trạng Hệ thống Cũ (Legacy Analysis)

Qua quá trình khảo sát và phân tích mã nguồn hệ thống cũ (`db/src/`), nhóm nghiên cứu đã xác định các điểm nghẽn kỹ thuật và rủi ro vận hành sau:

### 1. Kiến trúc Client béo (Fat Client) và Phụ thuộc nền tảng Desktop:
- Ứng dụng WinForms yêu cầu cài đặt trực tiếp trên từng máy trạm của nhân viên phòng nhân sự.
- Không thể truy cập từ xa qua môi trường Web hoặc thiết bị di động (Smartphone). Nhân viên thông thường không có công cụ tự tra cứu công, nộp đơn nghỉ phép hay xem phiếu lương trực tuyến, dẫn đến việc phòng nhân sự phải xử lý thủ công qua giấy tờ hoặc email phân tán.

### 2. Lỗ hổng Bảo mật và Quản lý Phiên đăng nhập:
- Hệ thống cũ sử dụng kết nối cơ sở dữ liệu trực tiếp hoặc thông qua các thủ tục lưu trữ (Stored Procedures) với tài khoản có đặc quyền cao.
- Mật khẩu người dùng lưu trữ theo cơ chế băm cũ, thiếu cơ chế xác thực đa yếu tố, thiếu khả năng kiểm soát phiên đăng nhập tập trung. Khi một tài khoản bị vô hiệu hóa hoặc nhân viên nghỉ việc, không có cơ chế thu hồi tức thì (Instant Session Revocation) các phiên đang hoạt động.
- Không có phân tách rõ ràng giữa danh tính xác thực (Identity User) và hồ sơ nhân sự (Employee Profile).

### 3. Rủi ro Vi phạm Tính toàn vẹn Dữ liệu và Xung đột Đồng thời (Concurrency Issues):
- **Phép âm khi duyệt đồng thời (Race Condition)**: Khi hai người quản lý cùng mở duyệt đơn nghỉ phép của một nhân viên có số dư phép còn 1 ngày, hệ thống cũ không sử dụng khóa đồng thời (Optimistic/Pessimistic Locking) hay giao dịch cách ly. Hậu quả là cả hai đơn đều được duyệt thành công, dẫn đến quỹ phép bị âm (`UsedDays > AllocatedDays`).
- **Gian lận thời gian chấm công**: Hệ thống cũ ghi nhận thời gian chấm công dựa trên đồng hồ cục bộ của máy client thay vì đồng hồ chuẩn của máy chủ trung tâm (`Server Clock`), tạo kẽ hở cho việc chỉnh sửa giờ hệ thống để chấm công sai lệch.
- **Sửa đè lịch sử lương và làm sai lệch kỳ lương đã công bố**: Hệ thống cũ cho phép cập nhật trực tiếp mức lương cơ bản trong bảng nhân viên. Khi tính toán lại, các kỳ lương cũ trong quá khứ đã được kế toán chốt và trả tiền bị tính lại theo mức lương mới, phá vỡ tính bất biến của sổ cái tài chính.

### 4. Khó khăn trong Bảo trì và Mở rộng:
- Mã nguồn nghiệp vụ bị phân tán rải rác giữa tầng giao diện (`HRM.GUI`) và các thủ tục cơ sở dữ liệu (`Stored Procedures`), gây khó khăn cho việc viết kiểm thử tự động (Unit Test / Integration Test).
- Quá trình phát hành phiên bản mới đòi hỏi phải nâng cấp thủ công từng máy client, rủi ro không đồng bộ phiên bản rất cao.

---

## 1.3. Mục tiêu Hiện đại hóa Hệ thống

Để giải quyết triệt để các tồn tại trên, đề tài đặt ra các mục tiêu kỹ thuật cụ thể:

1. **Chuyển đổi Kiến trúc sang Modular Monolith hiện đại**:
   - Xây dựng lại hệ thống trên nền tảng **.NET 10** với kiến trúc đa tầng sạch (Clean Architecture: Domain, Application, Infrastructure, Api).
   - Tách biệt rõ ràng ranh giới nghiệp vụ (Bounded Contexts) giữa Nhân sự, Chấm công, Nghỉ phép, Tiền lương và Thông báo.
2. **Đảm bảo Tính toàn vẹn Dữ liệu Tuyệt đối (Data Integrity & Invariants)**:
   - Áp dụng triệt để khóa lạc quan (`RowVersion / ConcurrencyCheck`) và giao dịch cách ly cơ sở dữ liệu trong các luồng phê duyệt và điều chỉnh số dư.
   - Xây dựng cơ chế **Snapshot bất biến** cho bảng tính lương: các kỳ lương đã khóa (Locked) hoặc đã công bố (Published) là bất biến vĩnh viễn, không thể bị sửa đổi hay tính lại bởi các thay đổi lương trong tương lai.
   - Đồng bộ thời gian chấm công theo giờ chuẩn máy chủ (`Server Clock`) và chống gửi lặp bằng mã định danh giao dịch (`Idempotency-Key`).
3. **Mô hình Bảo mật Phiên Đa nền tảng**:
   - Hỗ trợ đồng thời Web SPA (sử dụng HttpOnly Secure Cookie với cơ chế chống CSRF nghiêm ngặt) và Mobile App (sử dụng Bearer/Refresh Token quay vòng tất định).
   - Cơ chế thu hồi phiên tức thời: đổi mật khẩu, đăng xuất từ xa hoặc khóa tài khoản sẽ vô hiệu hóa ngay lập tức mọi phiên đang hoạt động trên cả hai nền tảng.
4. **Phát triển Giao diện Đa nền tảng Tiện ích**:
   - Xây dựng ứng dụng **Web Single Page Application (SPA)** hiện đại bằng React 18, Vite và TypeScript dành cho công tác quản trị của Nhân sự, Kế toán và Ban Quản trị.
   - Xây dựng ứng dụng di động **Mobile App** (React Native / Android) dành cho nhân viên tự chấm công, nộp đơn nghỉ phép và tra cứu phiếu lương.
5. **Đảm bảo Độ tin cậy và Khả năng Triển khai An toàn**:
   - Sử dụng mô hình **Transactional Outbox** cho luồng gửi email thông báo, không để lỗi mạng SMTP làm gián đoạn giao dịch nghiệp vụ.
   - Quy trình di chuyển dữ liệu (Data Migration) từ cơ sở dữ liệu cũ sang mới có đối soát số dòng 100% và bảo toàn tài chính với sai số 0.00 VND.
   - Tự động hóa kiểm thử và đóng gói triển khai staging bằng Docker Compose và GitHub Actions CI.
