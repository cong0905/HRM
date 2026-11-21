# T17 — Nghiệm thu kỹ thuật theo ủy quyền

Ngày 14/11/2026. Chủ dự án chọn rõ: “Ủy quyền mình review và nghiệm thu kỹ thuật T17, rồi làm T18”. Quyền này áp dụng cho T17; T18 vẫn phải bàn giao riêng để review.

Codex đã đọc lại luồng PATCH/rowversion, scope HR, CRUD danh mục/transaction/audit, preview lịch, khóa kỳ và UI theo permission. Không thấy finding mới chặn nghiệm thu trong phạm vi này. Hash các file nguồn khớp hoàn toàn bằng chứng T17 đã bàn giao. Chạy lại SQL T17 4/4 và web 23/23 PASS, 0 skip; SQL fixture tự dọn. Đối chiếu bằng chứng cũ: 388 ca hồi quy trước T17 PASS, sáu kịch bản browser đã PASS qua các lượt và contract live khớp generated client. Không gọi kết quả tổng hợp là một lượt SQL 392/392 hoặc browser 6/6 trong cùng lần chạy.

Chuyển riêng T17 thành **VERIFIED kỹ thuật**, đủ điều kiện bắt đầu T18. Human UAT, remote CI, deploy và email thật vẫn NOT_RUN. Giới hạn bundle lớn và phạm vi browser/accessibility giữ như báo cáo T17.

Bằng chứng mới: `docs/verification/t17-acceptance/result.json`, `t17.trx`, `sql.json`, `web-tests.json`. Baseline nguồn được đóng băng trong acceptedSourceSha256 trước thay đổi T18; không ghi đè bằng chứng T17.
