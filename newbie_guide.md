# 📘 Hướng Dẫn Chi Tiết Dự Án HRM Cho Người Mới

Chào mừng bạn gia nhập dự án **HRM (Human Resource Management)**. Tài liệu này được thiết kế để giúp bạn từ một người "không biết gì" trở thành người có thể đọc hiểu và bắt đầu sửa code trong dự án này.

---

## 1. Bản đồ cấu trúc Project (Solution Structure)
Dự án được chia làm 5 vùng nội bộ chính. Khi mở Visual Studio, bạn sẽ thấy:

1.  **HRM.GUI:** (Presentation) Chứa toàn bộ giao diện người dùng (Windows Forms).
2.  **HRM.BLL:** (Business Logic) Nơi xử lý các "quy tắc nghiệp vụ".
3.  **HRM.DAL:** (Data Access) Nơi thực hiện các lệnh đọc/ghi vào Cơ sở dữ liệu.
4.  **HRM.Domain:** Định nghĩa các bảng Database (Entities).
5.  **HRM.Common:** Chứa các đối tượng trung chuyển dữ liệu (DTOs) và các hằng số dùng chung.

---

## 2. Đi sâu vào từng lớp (Chi tiết kỹ thuật)

### A. HRM.Domain & HRM.Common (Gốc rễ)
- **Entities:** Bạn muốn thêm một cột vào bảng `NhanVien`? Hãy mở `Entities/NhanVien.cs`. Mỗi thuộc tính ở đây tương ứng 1 cột trong SQL.
- **DTOs:** Dùng để gửi dữ liệu từ Database lên Giao diện. Ví dụ: `NhanVienDTO` chỉ chứa các thông tin cần hiển thị, che giấu các thông tin nhạy cảm như Password.

### B. HRM.DAL (Lớp làm việc với DB)
Sử dụng **Entity Framework Core (EF Core)**. Không có câu lệnh SQL `SELECT/INSERT/UPDATE` nào được viết tay ở đây.
- **Context (`HrmDbContext.cs`):** Đăng ký các bảng với EF Core.
- **Repository Pattern:** Mỗi module có 1 Repository (vd: `PhongBanRepository.cs`). Nó chứa các hàm chuyên biệt như `SearchAsync`, `GetByIdWithDetails`.

### C. HRM.BLL (Lớp xử lý Logic)
- **Service Pattern:** Đây là "bộ não". Nếu bạn muốn thêm logic: *"Không được cho nhân viên chấm công quá 2 lần/ngày"*, bạn phải viết ở đây. Các Service (vd: `ChamCongService.cs`) sẽ gọi Repository để lấy dữ liệu, sau đó thực hiện tính toán.

### D. HRM.GUI (Lớp giao diện)
- Sử dụng **Windows Forms**. 
- **Quy tắc quan trọng:** Tuyệt đối không gọi trực tiếp Repository từ đây. Phải gọi qua Service của lớp BLL.
- **Thiết kế:** Bạn sẽ thấy các tệp `.Designer.cs` (mã tự sinh cho UI) và `.cs` (mã xử lý sự kiện nút bấm).

---

## 3. Luồng đi của dữ liệu (The Flow)
Hãy tưởng tượng luồng đi của 1 yêu cầu **"Xem danh sách phòng ban"**:

1.  **GUI:** `frmMain` gọi hàm `_phongBanService.GetAllAsync()`.
2.  **BLL:** `PhongBanService` nhận lệnh, yêu cầu `_phongBanRepository.GetAllWithTruongPhongAsync()`.
3.  **DAL:** `PhongBanRepository` chạy lệnh LINQ của EF Core. EF Core dịch nó thành SQL gửi xuống Database.
4.  **Database:** Trả về dữ liệu thô.
5.  **DAL:** Nhận dữ liệu thô (Entities), trả về cho BLL.
6.  **BLL:** Nhận Entities, chuyển đổi thành **DTOs** (để lọc bớt thông tin thừa) rồi trả về cho GUI.
7.  **GUI:** Nhận DTOs và hiển thị lên bảng `DataGridView`.

---

## 4. Hướng dẫn: Cách thêm 1 chức năng mới
Giả sử bạn cần thêm chức năng **"Cập nhật trạng thái nhân viên"**:

1.  **Bước 1 (Domain):** Kiểm tra xem thực thể `NhanVien.cs` đã có trường `TrangThai` chưa.
2.  **Bước 2 (DAL):** Trong `INhanVienRepository`, đảm bảo hàm `UpdateAsync` đã sẵn sàng (thường đã có sẵn trong lớp Generic Repository).
3.  **Bước 3 (BLL):** Mở `NhanVienService.cs`, viết hàm `UpdateStatusAsync(int id, string status)`. Tại đây, kiểm tra xem ID có tồn tại không trước khi gọi DAL.
4.  **Bước 4 (GUI):** Ở màn hình danh sách nhân viên, thêm 1 nút bấm "Đổi trạng thái". Khi bấm nút, gọi hàm từ `NhanVienService` vừa viết.

---

## 5. Các "Bí kíp" để code nhanh trong dự án này
- **LINQ:** Hãy học cách dùng các hàm `.Where()`, `.Select()`, `.Include()`, `.FirstOrDefault()`. Đó là cách bạn lấy dữ liệu.
- **Dependency Injection (DI):** Bạn sẽ thấy Service/Repository được truyền vào Constructor (hàm khởi tạo). Đừng lo lắng, hệ thống tự động làm việc này, bạn chỉ cần khai báo và sử dụng.
- **Async/Await:** Mọi thao tác Database đều là bất đồng bộ. Luôn dùng `async Task` và `await` để app không bị "đơ" khi đang tải dữ liệu.

---
*Hy vọng bản hướng dẫn này giúp bạn tự tin hơn khi bắt đầu làm việc. Đừng ngần ngại hỏi tôi nếu bạn thấy một đoạn code nào đó quá khó hiểu!*
