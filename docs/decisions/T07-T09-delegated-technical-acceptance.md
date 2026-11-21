# Nghiệm thu kỹ thuật T07–T09 theo ủy quyền

Ngày 12/11/2026, chủ dự án yêu cầu trực tiếp: **“tiếp tục đi quyết định thay t”**.
Codex diễn giải quyền này gồm chốt lựa chọn kỹ thuật và nghiệm thu kỹ thuật để tiến hành task kế tiếp.
Không ghi nhận chủ dự án đã tự đọc code hay làm UAT; đây là quyết định của Codex theo ủy quyền, dựa trên bằng chứng thực chạy.

- T07: đóng nghiệm thu kỹ thuật các luồng phân quyền/scope/audit, được chạy lại trong gate T09.
- T08: trước khi sửa T09, đã đối chiếu code khớp gate PASS147; toàn bộ 21 ca T08 tiếp tục đạt trong gate T09.
- T09: 27 ca lịch mới đạt, cùng tất cả các ca cũ. Full gate PASS174, không skip, source SHA-256 khớp sau gate.
- Backend build 0 warnings/errors; OpenAPI/generated client/type tests/Node/Kestrel và dependency audit đều đạt.
- Legacy vẫn có 8 lỗi baseline được manifest nhận diện đúng; 16 characterization tests đạt. Không tuyên bố đã sửa các lỗi baseline này.
- Các task T07/T08/T09 chuyển VERIFIED theo nghiệm thu kỹ thuật được ủy quyền, không phải production-ready.
- Không commit/push/deploy, không kết nối HRM_System, không gửi email thật. Không triển khai T10 trong lượt T09 này; giữ các trạng thái task khác có sẵn.

Bằng chứng: `docs/verification/runs/T09-final/result.json`, `integration/integration.trx`,
`sql-evidence.json`, `legacy/result.json` và `docs/verification/T09-calendar.md`.
