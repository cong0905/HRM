# 📘 Hệ thống Quản lý Nhân sự (HRM System)

Chào mừng bạn đến với dự án Hệ thống Quản lý Nhân sự (HRM). Đây là một ứng dụng Desktop được xây dựng trên nền tảng **.NET 8 WinForms**, sử dụng kiến trúc **N-Tier (Nhiều tầng)** kết hợp với **Entity Framework Core**.

Tài liệu này được viết theo cách đơn giản nhất để bất kỳ thành viên mới nào (Newbie) cũng có thể hiểu cấu trúc, cách chạy và luồng hoạt động của code.

---

## 🚀 1. Hướng dẫn chạy dự án (Dành cho người mới)

Dự án này đã được thiết lập cơ chế **Auto-Migration** và **Auto-Seeding**. Nghĩa là bạn không cần cài đặt Database thủ công, ứng dụng sẽ tự lo mọi việc.

### Yêu cầu hệ thống:
*   Visual Studio 2022 (Có cài đặt Workload *.NET Desktop Development*).
*   SQL Server (hoặc SQL Server LocalDB đi kèm sẵn với Visual Studio).

### Các bước khởi chạy:
1.  Mở thư mục `src` và nhấp đúp vào file **`HRM.sln`** để mở dự án trong Visual Studio.
2.  Nhìn sang bảng **Solution Explorer** (thường ở bên phải màn hình).
3.  Tìm project có tên **`HRM.GUI`**, click chuột phải vào nó và chọn **"Set as Startup Project"** (Dự án này sẽ in đậm lên).
4.  Nhấn nút **Start (Mũi tên xanh)** hoặc phím **F5** trên bàn phím.
5.  Đợi vài giây để ứng dụng tự động kiểm tra và khởi tạo Database `HRM_System` (bao gồm cả việc nạp dữ liệu mẫu).
6.  Màn hình đăng nhập hiện lên, hãy dùng tài khoản mặc định:
    *   **Tên đăng nhập:** `admin`
    *   **Mật khẩu:** `admin123`

---

## 🏗️ 2. Cấu trúc dự án (Kiến trúc 5 Tầng)

Khác với các dự án nhỏ viết tất cả code vào một nơi, dự án này chia code thành 5 phần (5 project) riêng biệt. Điều này giúp dễ quản lý sửa lỗi và làm việc nhóm.

| Tên Project | Tầng (Layer) | Vai trò làm gì? |
| :--- | :--- | :--- |
| **`HRM.Domain`** | Models (Thực thể) | Nơi định nghĩa các "Đồ vật" trong hệ thống (như class `NhanVien`, `PhongBan`...). Tầng này không chứa logic, chỉ chứa các biến (Properties). |
| **`HRM.DAL`** | Data Access | Nơi làm việc trực tiếp với SQL Server. Gọi là tầng "Lấy/Ghi dữ liệu". Chứa `HrmDbContext` (để Entity Framework biên dịch C# sang SQL) và các `Repository` (như các kho hàng). |
| **`HRM.BLL`** | Business Logic | Não bộ của hệ thống. Nhận yêu cầu từ Giao diện, kiểm tra nghiệp vụ (VD: Người này có tồn tại không? Đủ tuổi chưa?), sau đó nhờ DAL lưu/lấy dữ liệu. Chứa các `Services`. |
| **`HRM.GUI`** | Giao diện | Nơi chứa các màn hình WinForms (`frmLogin`, `frmMain`...). Nơi người dùng thực sự click chuột và gõ phím. Cấm viết lệnh gọi Database trực tiếp ở đây! |
| **`HRM.Common`** | Tiện ích chung | Nơi chứa các công cụ dùng chung cho cả dự án. Ví dụ: DTOs (Cặp xách đựng dữ liệu), Helpers (Hàm mã hóa mật khẩu BCrypt). |

---

## 🔄 3. Luồng xử lý dữ liệu (Ví dụ: Chức năng Đăng nhập)

Để bạn dễ hình dung cách 5 tầng trên "nói chuyện" với nhau, hãy xem quy trình khi bạn bấm nút **Đăng Nhập**:

1.  **Giao diện (`HRM.GUI`)**: Bạn gõ `admin` và `admin123`. Form đăng nhập gom 2 chữ này vào một hộp có tên là `LoginDTO` rồi đưa cho não bộ (`BLL`).
2.  **Não bộ (`HRM.BLL` - `AuthService`)**: Nhận hộp `LoginDTO`. Não bộ nghĩ: *"Để kiểm tra, mình phải tìm trong kho xem có ai tên 'admin' không đã"*. Nó gọi xuống kho dữ liệu (`DAL`).
3.  **Kho dữ liệu (`HRM.DAL` - `TaiKhoanRepository`)**: Nhận lệnh từ não bộ, tự động viết một câu lệnh SQL ngầm: `SELECT * FROM TaiKhoan WHERE TenDangNhap = 'admin'`. Sau đó SQL Server trả về kết quả là anh A, có mã Hash mật khẩu là `$2a$11$xyz...` đưa lại cho Não bộ.
4.  **Não bộ (`HRM.BLL` - `AuthService`)**:
    *   Nhận dữ liệu từ kho, thấy anh A có tồn tại.
    *   Tuy nhiên, mật khẩu anh A lưu là mã Hash (`$2a...`), nhưng người dùng nhập là `admin123`.
    *   Não bộ nhờ tầng Tiện ích (`HRM.Common` - `PasswordHelper`) để đối chiếu. Giải mã khớp => Não bộ báo về cho Giao diện là Đăng nhập thành công!
5.  **Giao diện (`HRM.GUI`)**: Nhận tin vui, liền đóng màn hình Đăng nhập và mở màn hình chính lên (`frmMain`).

> **💡 Nguyên tắc bắt buộc nhớ:** Giao diện (`GUI`) không bao giờ được phép "đi ngang về tắt" xuống Kho dữ liệu (`DAL`). Giao diện gặp chuyện gì cũng phải gọi Não bộ (`BLL`) hỏi, Không được gọi `DbContext` hay viết câu SQL trực tiếp ở các nút bấm WinForms.

---

## 🛠️ 4. Cách thêm một tính năng mới (Mini Guide)

Khi leader giao cho bạn tạo tính năng "Quản lý Phương tiện", bạn sẽ làm theo các bước (từ dưới lên trên) như sau:

1.  **BƯỚC 1 (Domain):** Mở `HRM.Domain`, tạo class `PhuongTien` có BiểnSố, LoạiXe, ID_NhanVien...
2.  **BƯỚC 2 (DAL):** 
    *   Mở `HrmDbContext`, thêm dòng `public DbSet<PhuongTien> PhuongTien { get; set; }`.
    *   Tạo file `PhuongTienConfiguration` để cấu hình quy tắc (Độ dài chữ, Khóa chính, Khóa ngoại).
    *   Tạo `IPhuongTienRepository` và `PhuongTienRepository` nếu cần viết câu query SQL gì đặc biệt.
3.  **BƯỚC 3 (Migrations):**
    *   Mở Package Manager Console (Tools > NuGet... > Package Manager Console).
    *   Gõ: `Add-Migration ThemBangPhuongTien -Project HRM.DAL -StartupProject HRM.GUI`
    *   Gõ: `Update-Database`
4.  **BƯỚC 4 (BLL):** Tạo file `PhuongTienService` để viết logic kiểu như `KiemTraBienSoHopLe()`, `ThemMoiPhuongTien()`.
5.  **BƯỚC 5 (GUI):** Thiết kế Form `frmQuanLyPhuongTien`. Kéo một cái nút `Thêm`. Gọi xuống hàm `ThemMoiPhuongTien()` của `PhuongTienService` ở Bước 4. Chấm hết!

---

*Tài liệu này được soạn để cung cấp kiến thức nền tảng vững chắc cho bạn. Hãy đọc kỹ luồng xử lý dữ liệu và tuân thủ chặt chẽ kiến trúc dự án nhé. Chúc bạn code vui vẻ!* 🚀
