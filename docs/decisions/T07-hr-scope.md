# T07 — Quyết định phạm vi HR của chủ dự án

Ngày 11/11/2026. Trạng thái: **CONFIRMED bởi chủ dự án trong chat**.

Đã hỏi: “Bạn muốn HR chỉ quản lý nhân viên được phân công, hay mọi HR được quản lý toàn công ty?”
Chủ dự án chọn: **“Chỉ nhân viên được phân công (khuyến nghị)”**.

## Quy tắc áp dụng

- Role HR không tự cấp quyền xem/sửa toàn bộ nhân viên của công ty.
- HR chỉ truy cập bản ghi của nhân viên được phân công bằng dữ liệu phía server đáng tin cậy.
- Không có phân công: mặc định từ chối truy cập người khác. Quyền đọc thông tin của chính mình
  được xử lý riêng theo đặc tả; không suy ra quyền sửa người khác từ quyền đọc cá nhân.
- Không tự dùng phòng ban/chi nhánh hay hierarchy để suy diễn phạm vi. MVP dùng phân công nhân viên
  tường minh; mở rộng cơ cấu quản lý cần quyết định riêng.
- Phạm vi phải áp dụng cả read/update, danh sách, bulk/export và các nghiệp vụ HR liên quan; không chỉ UI.
- Thao tác gán/thu hồi phạm vi là thao tác nhạy cảm: cần quyền quản trị riêng phía server và audit,
  không cho HR tự cấp thêm phạm vi. Không làm SystemAdmin tự có quyền đọc PII.
- Thu hồi phân công phải làm request mới bị từ chối; tránh scope stale trong cookie/bearer.

Đây là quyết định của chủ dự án, thay phần “scope còn mở/chuyển T08” trong báo cáo Anti trước đó.
Cần triển khai/kiểm thử tại T07 trước nghiệm thu; không tự chuyển Verified chỉ vì đã có tài liệu quyết định.
