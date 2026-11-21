# T19 — Nghiệm thu kỹ thuật theo ủy quyền

Ngày 15/11/2026. Sau khi được báo T19 đang READY_FOR_REVIEW và chờ nghiệm thu, chủ dự án yêu cầu rõ: **“nghiệm thu giúp t”**. Codex nhận ủy quyền quyết định nghiệm thu kỹ thuật riêng T19.

## Quyết định

Chuyển **T19 thành VERIFIED về kỹ thuật**. Không có finding chặn nghiệm thu trong phạm vi web lương/dashboard được giao. Các file trong manifest khớp SHA-256 tại thời điểm nghiệm thu; không có thay đổi mã thực thi sau lượt kiểm chứng cuối, nên không chạy lại các bộ test chỉ để đổi trạng thái tài liệu.

## Cơ sở nghiệm thu

- SQL integration: 398/398 PASS, 0 fail/skip, ngày 14/11. Backend nghiệp vụ/controller giữ nguyên trong vòng sửa UI 15/11.
- Web: 33/33 PASS; Playwright: 6/6 PASS trong cùng một lượt fixture SQL sạch, ngày 15/11. Build/typecheck/lint PASS. Ảnh desktop 1440px và mobile 375px đã kiểm tra trực quan.
- Đủ ba tiêu chí task: dữ liệu thiếu/lỗi không được công bố; GET/reload không đổi số; tăng lương tháng sau không làm đổi phiếu đã phát hành; nhân viên chỉ đọc phiếu Published của chính mình; tài khoản riêng đúng vai trò.
- Luồng kiểm tính tay công + phép, điều chỉnh, tính lại, đối chiếu run, stale inputs/RowVersion, đối chiếu chủ động trước duyệt, tách người tính và người duyệt/công bố, CSV/403, IDOR và khóa đều có bằng chứng.
- Đối soát SQL: hai phiếu Published tháng 2026-04 tổng Net 12.001.102 VND; kỳ Locked và ActiveBatchId = ApprovedBatchId = PublishedBatchId. Snapshot tài chính bất biến, metadata periodStatus được phép chuyển Published → Locked.
- Database GUID, file credential/cert tạm và cổng test 3001 đã được dọn.

## Ranh giới ủy quyền và giới hạn

T18 có bằng chứng kỹ thuật và việc triển khai T19 khi T18 READY_FOR_REVIEW đã được chủ dự án cho phép riêng trước đó; quyết định này không tự nâng T18 thành VERIFIED. T14/T15/T16 đã được nghiệm thu như hồ sơ phụ thuộc. Không tự bắt đầu T20.

Đây là nghiệm thu kỹ thuật theo ủy quyền, không ghi nhận rằng chủ dự án đã tự human UAT. Remote CI, deploy, thử tải, mobile native và đồng bộ database desktop/web chưa thực hiện. Cảnh báo bundle >500 kB, Graphify parser trích xuất một phần MyPayslips.tsx và whitespace legacy ngoài phạm vi giữ đúng báo cáo kiểm chứng.

## Bằng chứng được giữ lại

- `docs/verification/T19-web-payroll-review.md` — báo cáo kiểm chứng và giới hạn.
- `docs/verification/t19-acceptance/tested-result.json` — bản sao nguyên vẹn manifest READY_FOR_REVIEW trước quyết định này, giữ hash nguồn đã kiểm thử.
- `docs/verification/t19-acceptance/result.json` — quyết định nghiệm thu máy đọc, ủy quyền và acceptedSourceSha256.
- `docs/verification/t19/` — TRX, Vitest, Playwright, đối soát SQL và ảnh. Chỉ cập nhật metadata trạng thái; không sửa kết quả test để tạo PASS.
