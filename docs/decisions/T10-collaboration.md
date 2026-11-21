# T10 — Codex và Antigravity cùng làm

Chủ dự án ngày 12/11/2026 yêu cầu Codex và Anti cùng làm, tiếp nối ủy quyền quyết định kỹ thuật đã có.
Anti chỉ viết AttendanceService.cs và AttendanceController.cs. Codex viết hợp đồng/domain, DbContext/migration/DI,
khóa giao dịch chung với offboarding/publish, test độc lập và chạy cổng nghiệm thu. Không cùng sửa một file khi Anti đang chạy.

Các quyết định MVP: một summary/người/ngày; original events, receipts và audit append-only; UUIDv4 key scoped theo
user/operation và fingerprint DTO; replay trả response gốc, không tạo event mới. Giờ client chỉ telemetry, không tính công.
HR chỉ sửa người được phân công, cấm tự sửa công/duyệt OT; lịch sử người nghỉ việc có thể sửa đến ngày nghỉ việc, nếu kỳ chưa đóng.
OT là xác nhận riêng, một tổng phút được HR xác nhận/ngày, không tự lấy giờ checkout muộn làm OT. Đã xác nhận OT thì cấm sửa công
qua workflow MVP; quy trình hồi tố/amendment chưa có. Trạng thái MissingCheckout được tính theo ngày server khi đọc, không có GET ghi DB/job ban đêm giả lập.
Không GPS/HWID/IP location, không T11, không mail thật/production/commit/push. T10 chỉ chốt nghiệm thu khi kiểm chứng thực chạy.

HR can deliberately mark checkout incomplete. That current-day view is Incomplete; on a later business date it is MissingCheckout. A fresh employee checkout cannot overwrite HR-corrected state; only HR can correct it again. Original successful receipt replay still returns its original response without mutation.
