# 📁 Chi Tiết Các Thư Mục Quan Trọng Trong Dự Án HRM

Để trở thành một "cao thủ" trong dự án này, bạn cần biết chính xác code của mình nên nằm ở thư mục nào. Dưới đây là bảng phân tích chi tiết:

---

## 1. 📂 HRM.DAL (Data Access Layer) - "Làm việc với Database"
Đây là nơi chứa tất cả những gì liên quan đến dữ liệu thô và SQL.

*   **`Context/`**: Chứa `HrmDbContext.cs`. Đây là tệp quan trọng nhất của EF Core, đóng vai trò kết nối giữa các Class C# và các bảng SQL.
*   **`Repositories/`**: Chứa các "Kho chứa dữ liệu". Mỗi bảng sẽ có một Repository riêng (vd: `NhanVienRepository.cs`) để thực hiện các lệnh CRUD (Thêm, Đọc, Sửa, Xóa).
*   **`Configurations/`**: Chứa các quy tắc thiết kế bảng. Ví dụ: Cột `HoTen` dài tối đa bao nhiêu ký tự, cột nào là Khóa chính... Đều được cấu hình bằng code tại đây thay vì chỉnh tay trong SQL.
*   **`Migrations/`**: Chứa lịch sử thay đổi của Database. Mỗi khi bạn thêm 1 cột mới trong code và chạy lệnh cập nhật, EF Core sẽ tạo ra 1 tệp trong này để "nhớ" lịch sử đó.

---

## 2. 📂 HRM.BLL (Business Logic Layer) - "Xử lý nghiệp vụ"
Đây là nơi chứa chất xám của hệ thống, không quan tâm giao diện trông thế nào hay DB lưu ở đâu.

*   **`Interfaces/`**: Chứa các "Hợp đồng" (Interface). Ví dụ: `INhanVienService` định nghĩa là *"Tôi có hàm tính lương"*. 
*   **`Services/`**: Chứa phần triển khai thực tế của các Hợp đồng trên. Đây là nơi bạn viết các câu lệnh `if...else`, các công thức tính toán phức tạp.

---

## 3. 📂 HRM.Domain & HRM.Common - "Nguyên liệu chung"
Hai thư mục này giống như các thư viện dùng chung cho toàn dự án.

*   **`HRM.Domain/Entities/`**: Chứa định dạng của các bảng. Ví dụ: File `NhanVien.cs` định nghĩa chính xác cấu trúc một nhân viên trong hệ thống.
*   **`HRM.Common/DTOs/`**: Chứa các "Gói dữ liệu rút gọn". Khi bạn muốn gửi dữ liệu từ tầng DAL lên GUI để hiển thị, bạn dùng DTO để dữ liệu được gọn nhẹ và bảo mật.
*   **`HRM.Common/Helpers/`**: Chứa các công cụ tiện ích dùng ở nhiều nơi (vd: Hàm định dạng ngày tháng, hàm mã hóa mật khẩu...).

---

## 4. 📂 HRM.GUI (Presentation Layer) - "Giao diện người dùng"
Đây là nơi "vẽ" ra phần mềm Windows.

*   **`Forms/`**: Thư mục lớn nhất, chứa toàn bộ các màn hình của app. 
    *   Thường được chia nhỏ theo module: `Forms/Main/`, `Forms/Auth/`...
    *   Mỗi màn hình thường có 3 file đi kèm: `.cs` (logic giao diện), `.Designer.cs` (mã tự sinh), và `.resx` (hình ảnh, icon).
*   **`Program.cs`**: File chạy đầu tiên khi bạn mở app. Nó quyết định màn hình nào sẽ hiện ra trước (thường là màn hình Đăng nhập).

---

## 💡 Tóm tắt "Cần gì - Tìm đâu":
1.  **Muốn thêm cột vào bảng DB?** -> Vào `HRM.Domain/Entities/`.
2.  **Muốn chỉnh sửa bảng hiển thị trên màn hình?** -> Vào `HRM.GUI/Forms/`.
3.  **Muốn sửa công thức tính lương/phép?** -> Vào `HRM.BLL/Services/`.
4.  **Muốn viết một câu truy vấn tìm kiếm mới?** -> Vào `HRM.DAL/Repositories/`.
5.  **Muốn định nghĩa một trạng thái mới (vd: Đang chờ duyệt)?** -> Vào `HRM.Domain/Enums/`.

---
*Việc nắm vững các thư mục này sẽ giúp bạn không bao giờ bị lạc lối trong hàng trăm tệp tin của dự án!*
