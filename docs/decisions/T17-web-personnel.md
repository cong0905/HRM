# T17 — Web nhân sự và lịch làm việc

Ngày: 14/11/2026. Phạm vi: T17; không chuyển sang T18–T28.

- Dùng React/Ant Design/TanStack Query và generated client đã có. Giữ thiết kế `web/DESIGN.md`; không thêm thư viện hoặc dữ liệu mẫu vào ứng dụng.
- Nhân viên dùng `/my-profile`, không có danh sách nhân sự hoặc trình sửa HR. Server vẫn là nơi kiểm tra phạm vi từng bản ghi. UI không suy ra quyền từ tên vai trò.
- HR tạo hồ sơ không đồng nghĩa HR được xem hồ sơ. Giữ nguyên chính sách T07/T08: SystemAdmin cấp phạm vi rõ ràng qua API scope hiện có. UI hiển thị mã hồ sơ sau tạo, trang quản trị có thao tác cấp phạm vi; không tự gán theo phòng ban hoặc người tạo.
- Chức vụ dùng bảng mới `personnel.Positions`. Dùng quyền danh mục `departments.read/manage` đã cấp cho HR/SystemAdmin; không cấp quyền nhân sự hoặc lương cho SystemAdmin. FK Restrict giữ tham chiếu phòng ban/chức vụ, không xóa âm thầm thông tin trên hồ sơ.
- CRUD danh mục có transaction SQL, audit trong cùng transaction, khóa ứng dụng để tuần tự hóa việc đổi tên/xóa và kiểm tra tên cũ trước khi sửa. Tên là trường duy nhất được sửa của danh mục; không thay quy tắc rowversion của hồ sơ nhân viên.
- Danh sách nhân sự lọc họ tên/phòng ban/trạng thái và phân trang tại server. PATCH gửi whitelist trường thật sự thay đổi, reason và rowVersion. Gặp 409 giữ bản nhập, tải bản mới riêng để so sánh; chỉ thay biểu mẫu sau lựa chọn rõ ràng của người dùng.
- Calendar chỉ thêm phiên bản; không có endpoint sửa/xóa lịch cũ. `/work-calendars/preview` dùng CalendarMath thật, không ghi DB và cần quyền quản lý lịch. UI vô hiệu nút lưu khi biểu mẫu đổi sau preview; backend tiếp tục chặn khoảng áp dụng của kỳ lương đã công bố/khóa.
- Preview mô tả toàn tháng nếu dùng định nghĩa đề xuất. Lịch đang áp dụng đọc endpoint tháng thật và hiển thị version từng ngày; không hứa preview toàn tháng chính là lịch hỗn hợp trước ngày hiệu lực.
- Không gửi email thật, không dùng HRM_System, không deploy hoặc ghi dữ liệu sản phẩm trong kiểm thử. Harness tạo database GUID riêng và tài khoản `example.invalid` rồi tự dọn.
