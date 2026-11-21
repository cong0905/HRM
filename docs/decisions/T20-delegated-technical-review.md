# T20 — ủy quyền kiểm tra và nghiệm thu kỹ thuật

Ngày 15/11/2026 (Asia/Saigon), chủ dự án yêu cầu Codex: “khi nào làm xong m sẽ kiểm tra và duyệt giúp t nhé”, sau khi giao Anti triển khai T20.

Codex được theo dõi bàn giao, kiểm tra độc lập, yêu cầu/sửa lỗi và quyết định nghiệm thu kỹ thuật **riêng T20** khi đủ bằng chứng. Không cần xin lại quyền nghiệm thu đã được giao. Chỉ đạo này không nâng trạng thái T18 hoặc giao T21.

Biên bản này ghi quyền kiểm tra, chưa phải quyết định VERIFIED. Tiêu chí bắt buộc gồm chạy Android, khôi phục/thu hồi phiên, chống refresh loop và bảo vệ credential. Unit tests hoặc lời tự báo WORK_COMPLETE của Anti không thay thế bằng chứng runtime.

Không diễn giải nghiệm thu kỹ thuật thành human UAT, iOS đã chạy, remote CI, staging/deploy sản phẩm hoặc phát hành Play Store. APK fixture nếu tạo được chỉ dành kiểm thử, không phải bản phát hành có signing key sản phẩm.
