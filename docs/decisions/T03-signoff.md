# Quyết định nghiệm thu T03 — 11/11/2026

**Quyết định: VERIFIED trong phạm vi T03.**

Người đánh giá và quyết định: Codex, theo ủy quyền trực tiếp của chủ dự án trong chat ngày 11/11/2026: “m hãy em và quyết định thay t”. Hiểu theo ngữ cảnh là yêu cầu xem và quyết định nghiệm thu T03 thay chủ dự án. Ủy quyền này áp dụng cho quyết định T03; không tự mở rộng thành quyền nghiệm thu các task khác hoặc triển khai production. Không ghi nhận chủ dự án đã tự chạy test hay tự kiểm tra source.

## Cơ sở quyết định

1. Bản đồ `docs/legacy-transfer-map.md` bao phủ sáu nhóm nghiệp vụ, gán trách nhiệm cho F01–F14 và tách rõ phạm vi legacy, schema EF T02, backend mới chưa triển khai. F11 đã được đối chiếu lại source hiện tại.
2. Có characterization tests gọi service/repository thật: phân biệt tình huống bỏ trường và chủ ý gán null qua hành vi thực tế, đọc mapping, cùng hai nhánh tracking. Test đọc lại sau khi clear tracker để xác minh dữ liệu đã lưu.
3. Kết quả Codex đã chạy trực tiếp: **8/8 characterization PASS**, **5/5 HarnessCheck PASS**, **8/8 RegressionBug FAIL đúng assertion trong manifest**; bộ thẩm định baseline exit 0. Đây là nghiệm thu task khoanh vùng, không phải khẳng định toàn hệ thống đã hết lỗi.
4. Trước quyết định, Codex đã kiểm lại SHA-256 của cả hai file test và transfer map: khớp bằng chứng lúc kiểm thử. Ba TRX có counters khớp summary; run-evidence xác nhận SUCCESS_BASELINE_MATCHED. Không có thay đổi mới ở các artifact này đòi hỏi chạy lại test.
5. Graph code đã được cập nhật thành công sau thay đổi test. Điều kiện re-scope/hotfix trước khi sử dụng legacy với dữ liệu thật đã được ghi rõ.

## Ranh giới nghiệm thu

- Chỉ nghiệm thu bản đồ chuyển nghiệp vụ, test đặc trưng và hồ sơ T03. Các lỗi nghiệp vụ legacy còn lại tiếp tục thuộc task phụ trách; không coi là đã sửa nhờ tài liệu.
- EF InMemory không chứng minh transaction/concurrency trên SQL Server. Không nghiệm thu thêm database thật, UI, API hoặc triển khai production.
- Quyết định dựa trên kết quả thực thi và đối chiếu của Codex trong chat, không dựa vào ACCEPT của CLI reviewer: lượt Anti CLI trước đó bị timeout.

## Bằng chứng

- [Bản đồ chuyển nghiệp vụ](../legacy-transfer-map.md)
- [Lệnh và kết quả kiểm tra](../verification/T03-codex-checks.md)
- [Summary và hash artifact tại lúc test](../verification/runs/T03-completion-summary.json) — trường READY_FOR_REVIEW trong summary là trạng thái tại thời điểm thu thập bằng chứng, trước quyết định này.
- [Baseline execution evidence](../verification/runs/T03-completion-baseline/run-evidence.json)

Đã đồng bộ VERIFIED trong thẻ T03, backlog Markdown và backlog JSON. Việc bắt đầu hoặc nghiệm thu các task tiếp theo là quyết định riêng.
