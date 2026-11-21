# T09 — quyết định được chủ dự án ủy quyền

Ngày 12/11/2026, chủ dự án yêu cầu: “tiếp tục đi quyết định thay t”.
Codex tiếp tục đúng T09 và tự chốt lựa chọn triển khai trong phạm vi MVP đã duyệt.
T08 đã được đối chiếu trực tiếp: source khớp gate PASS147 trước khi bắt đầu T09.
Đây là ủy quyền quyết định kỹ thuật/tiến độ, không phải lời khẳng định chủ dự án đã tự đọc mọi dòng code.

- Giữ UTC+7, ca 08:30–17:30 và nghỉ trưa 12:00–13:00 theo business-rules.
- Lịch có ngày hiệu lực, bất biến sau tạo; sửa bằng phiên bản mới. Không có PUT/PATCH/DELETE lịch.
- Cùng ngày bắt đầu chỉ có một phiên bản. Lịch thay đổi giữa tháng áp dụng từ đúng ngày đó.
- Snapshot tháng gắn duy nhất với một PayrollPeriod, giữ toàn bộ định nghĩa lịch và số phút mỗi ngày.
- Kỳ Published/Locked giữ snapshot bất biến; từ chối phiên bản hồi tố ảnh hưởng khoảng đã chốt.
- Cấu hình lịch toàn công ty: HR/SystemAdmin quản lý; Employee/HR/Payroll/SystemAdmin được đọc lịch.
  Đọc snapshot lương vẫn dùng quyền Payroll.Read; Employee không đọc snapshot của kỳ lương toàn công ty.
- Giữ giây trong decimal phút; không làm tròn giờ sớm; thiếu check-out cho 0 phút đủ điều kiện.
- Các ngày nghỉ là fixture demo đã chốt tại T00, không tự coi là lịch nghỉ pháp lý hiện hành.
- Không làm T10/chấm công, ca đêm, lịch nhiều nhà máy, SMTP, deploy hoặc commit/push trong lượt này.
