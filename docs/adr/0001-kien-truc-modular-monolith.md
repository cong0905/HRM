# ADR-0001: Lựa chọn Kiến trúc Modular Monolith cho Backend

> **Trạng thái:** `PROPOSED` (Đề xuất chờ Chủ dự án xác nhận)  
> **Ngày quyết định:** 09/11/2026  
> **Tác giả:** AI Coding Assistant / Nhóm phát triển HRM

---

## 1. Bối cảnh và Vấn đề (Context & Problem Statement)

Dự án hiện tại là ứng dụng Desktop WinForms (.NET 8) sử dụng kiến trúc N-Tier 5 lớp (`HRM.GUI`, `HRM.BLL`, `HRM.DAL`, `HRM.Domain`, `HRM.Common`). Trong kiến trúc cũ:

- Ứng dụng chạy trực tiếp trên máy client của người dùng và kết nối thẳng vào SQL Server trong cùng tiến trình.
- Không có tầng API trung gian để phục vụ cho các nền tảng Web và Mobile.
- Các quy tắc nghiệp vụ quan trọng (phân quyền vai trò, kiểm tra giờ máy, kiểm tra số dư phép) bị trộn lẫn hoặc phụ thuộc vào giao diện người dùng Desktop.

Mục tiêu mới của đồ án là xây dựng một hệ thống đa nền tảng gồm Web và Mobile trong vòng 8 tuần, do một người kiểm duyệt duy nhất quản lý cùng sự hỗ trợ của AI. Câu hỏi kiến trúc đặt ra là: **Nên tổ chức Backend theo mô hình Microservices phân tán hay Modular Monolith (Monolith theo khối module)?**

---

## 2. Các Yếu tố Thúc đẩy Quyết định (Decision Drivers)

1. **Giới hạn thời gian và nhân lực:** Dự án chỉ có thời gian 8 tuần và 1 sinh viên duy nhất kiểm duyệt mã nguồn. Cần tối thiểu hóa chi phí hạ tầng, devops và cấu hình mạng phức tạp.
2. **Tính toàn vẹn của dữ liệu và giao dịch:** Nghiệp vụ cốt lõi (tính lương, trừ quỹ phép) đòi hỏi các giao dịch ACID mạnh mẽ trên cơ sở dữ liệu. Microservices với distributed transactions (Saga/2PC) tiềm ẩn rủi ro sai sót cực lớn về tiền và số dư phép.
3. **Dễ kiểm thử và bảo vệ đồ án:** Sinh viên cần phải chạy được toàn bộ hệ thống trên một máy tính cá nhân để demo trực tiếp trước hội đồng chấm thi và giải thích mạch lạc kiến trúc.

---

## 3. Các Phương án đã Cân nhắc (Considered Options)

1. **Phương án A: Microservices Architecture**
   - Tách thành các dịch vụ độc lập: `AuthService`, `EmployeeService`, `AttendanceService`, `LeaveService`, `PayrollService`, kết nối qua API Gateway và Message Broker (RabbitMQ/Kafka).
2. **Phương án B: Traditional Monolith (Layered N-Tier cũ)**
   - Tiếp tục bọc các tầng cũ thành ASP.NET Core Web API chung một khối duy nhất không phân ranh giới domain.
3. **Phương án C (Được chọn): Modular Monolith Architecture**
   - Xây dựng một ứng dụng triển khai duy nhất (Single Deployment Unit) nhưng bên trong được tổ chức chặt chẽ thành các Module nghiệp vụ độc lập (`Identity`, `Personnel`, `Attendance`, `Leave`, `Payroll`) tuân thủ Clean Architecture (Domain, Application, Infrastructure, Api).

---

## 4. Quyết định (Decision Outcome)

**Lựa chọn Phương án C: Modular Monolith.**

Backend sẽ được cấu trúc thành một Solution duy nhất gồm các project phân tách ranh giới rõ ràng:

- `HRM.Domain`: Định nghĩa các thực thể cốt lõi, Value Objects và Domain Rules thuần túy (không phụ thuộc EF hay framework ngoài).
- `HRM.Application`: Định nghĩa các Use Cases, Command/Query handlers, Interfaces và DTOs.
- `HRM.Infrastructure`: Cài đặt tương tác cơ sở dữ liệu qua EF Core, Repositories, dịch vụ gửi email ngoài và Outbox worker.
- `HRM.Api`: Tầng giao tiếp REST API nhận HTTP requests, xử lý xác thực/phân quyền và gọi vào tầng Application.

---

## 5. Đánh đổi và Hệ quả (Trade-offs & Consequences)

### Điểm tích cực (Pros):

- **Phù hợp năng lực vận hành:** Một tiến trình Backend duy nhất chạy trên máy chủ cục bộ hoặc VPS, khởi động tức thì, không cần Docker Swarm hay Kubernetes phức tạp.
- **Bảo đảm giao dịch ACID:** Các thao tác duyệt đơn phép và tính toán bảng lương có thể tận dụng triệt để Transaction của SQL Server mà không lo ngại rủi ro phân tán dữ liệu.
- **Khả năng tiến hóa lâu dài:** Khi một module (ví dụ Chấm công) có tải cao trong tương lai sau đồ án, kiến trúc theo khối module cho phép tách module đó thành service độc lập dễ dàng mà không phải viết lại từ đầu.

### Điểm hạn chế và Thách thức (Cons & Mitigations):

- **Nguy cơ phụ thuộc chéo (Coupling):** Nếu lập trình viên không kỷ luật, các module có thể gọi trực tiếp sang DbContext hoặc Repository của nhau.
  - _Giải pháp:_ Thiết lập ranh giới module nghiêm ngặt ở tầng Application qua các Interface hợp đồng (Contracts); sử dụng bài kiểm thử kiến trúc (ArchUnitNET / tests) để ngăn chặn việc tham chiếu chéo không hợp lệ.
