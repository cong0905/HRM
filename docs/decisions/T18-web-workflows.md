# T18 — Web công, phép và hàng đợi duyệt

Ngày 14/11/2026. Chủ dự án đã ủy quyền nghiệm thu kỹ thuật T17 rồi triển khai T18; T17 đã VERIFIED theo biên bản riêng. T18 vẫn bàn giao READY_FOR_REVIEW, không tự chuyển VERIFIED hoặc mở T19.

- Dùng thiết kế, React/AntD và generated client hiện có. Giao diện theo permission thật từ server; route công chấp nhận quyền đọc của bản thân hoặc đọc toàn bộ để HR không bị chặn bởi guard chỉ dành cho Employee.
- Thêm API đọc danh sách công phân trang/lọc tại SQL, giữ cùng chính sách scope của API đọc từng ngày: HR theo assignment, Payroll có quyền PayrollReadAll + AttendanceReadAll được đọc công ty như trước. Không đổi công thức tính công, OT, phép hoặc phân quyền hiện có.
- Thêm API đọc trạng thái chỉnh sửa của tháng dưới Calendar.Read. Chỉ trả năm/tháng/isClosed, không lộ bảng lương hoặc số tiền. Predicate Published/Locked và tiền tố mã kỳ khớp gate ghi hiện có. UI kiểm các tháng liên quan và khóa thao tác nếu tháng đã đóng; server vẫn quyết định khi có race sau lượt đọc.
- Các lệnh ghi mang UUIDv4 Idempotency-Key trong header, chỉ giữ trong bộ nhớ. Giữ key khi người dùng thử lại cùng thao tác/nội dung/version sau phản hồi không chắc chắn; đổi key theo target/action/nội dung/version, xóa receipt sau thành công. Không tự retry mutation hoặc dùng timestamp khách làm giờ chấm công.
- Mẫu sửa công giữ nguyên timestamp server có độ chính xác cao nếu ô đó không thay đổi; giờ sửa mới được gửi rõ offset +07:00. OT là số phút xác nhận riêng, không suy ra từ giờ ngoài ca.
- Đơn phép gửi nguyên ngày trong cùng năm. Quỹ lấy từ API, không trừ cục bộ khi Pending. Duyệt/hủy/từ chối kèm reason và rowVersion; refresh dữ liệu sau mutation, giữ bản nhập khi lỗi. Lỗi stale có nút tải quyết định thực từ server, không tự gửi lại quyết định cũ.
- UI chặn tự duyệt/tự điều chỉnh dựa employee profile đã đọc, đồng thời server kiểm lại. HR không có profile được xem là không có hồ sơ của chính mình sau phản hồi 404 rõ ràng, không tự phát minh điều kiện bắt buộc profile nếu server không yêu cầu.
- Nhãn nhân viên chỉ resolve những ID thuộc trang hiện tại qua bulk API có kiểm scope sẵn có, không tải toàn bộ danh bạ. Cache nhãn/picker được invalidate khi hồ sơ thay đổi.
- Mỗi form có tên/ID riêng, kể cả form lọc và drawer nhân sự, để nhãn không trỏ nhầm ô cùng tên ở form khác. Ca browser hủy đơn đã tái hiện lỗi này trước sửa.
- Lịch ngày nghỉ mô tả khoảng đơn được chọn và lịch làm việc backend của tháng đang xem. Không gọi các ngày nghỉ cuối tuần/lễ trong khoảng là số ngày đã trừ quỹ.
- Test-only harness dùng database GUID tự dọn, actor giả, cookie đăng nhập thật, clock có điều khiển trong assembly kiểm thử. Ca rollback cố ý ném lỗi sau SQL audit flush và đối chiếu trạng thái sau rollback; không giả API thành công, không sửa gate sản phẩm. Cookie thử được dùng lại trong bộ nhớ để giữ nguyên giới hạn auth 20 request/phút, không tắt limiter.
