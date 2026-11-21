# Bộ prompt giao việc và kiểm duyệt AI

Bạn là người quyết định yêu cầu, xác nhận kết quả và chịu trách nhiệm giải thích khi bảo vệ. AI triển khai code và hỗ trợ kiểm tra; lời AI báo hoàn thành không tự thay cho nghiệm thu. Mỗi lần giao **một ID task**. Không giao câu “hãy hoàn thành cả dự án” rồi để AI tự mở rộng phạm vi.

## Prompt triển khai dùng lại

Copy đoạn dưới và thay TASK_ID bằng một ID có phụ thuộc đã đạt. Kèm folder kế hoạch hoặc đưa nội dung các file liên quan cho AI nếu công cụ không truy cập được filesystem.

```text
Bạn đang triển khai HRM cho đồ án 8 tuần. Tôi là người kiểm duyệt; bạn viết code.
Chỉ thực hiện TASK_ID trong bộ HRM_KeHoach_DoAn_8Tuan.

Đọc trước:
1. AGENTS.md và quy tắc repository đang áp dụng.
2. 00_DOC_TRUOC.md, 02_DAC_TA_MVP.md.
3. tasks/TASK_ID.md, task phụ thuộc và mục liên quan trong 06_DOI_CHIEU_NGUON.md.
4. Trạng thái Git hiện tại, docs/business-rules và ADR đã được tôi chốt.

Đối chiếu commit và code hiện tại: baseline audit có thể đã thay đổi.
Nếu có graphify-out/graph.json hợp lệ, query Graphify trước các tìm kiếm rộng.
Nếu chưa có graph, dùng skill Graphify để dựng theo quy tắc dự án.
Graph là chỉ dẫn tìm code, không thay việc đọc source và chạy test.

Trước sửa: nêu phạm vi file, acceptance, test cần chạy và blocker thực sự.
Không dừng hỏi lại các việc đã được task cho phép. Chỉ hỏi nếu thiếu quyết định
nghiệp vụ có ảnh hưởng đến tiền, quyền hoặc dữ liệu thật; tiếp tục phần độc lập.
Giữ nguyên thay đổi có sẵn; không xóa/reset code người khác.

Thực hiện thay đổi nhỏ, bám contract. Với bug tiền/quyền phải có test tái hiện
lỗi trước sửa và test xanh sau sửa. Transaction/concurrency dùng SQL Server test,
không lấy in-memory/fake thay bằng chứng SQL.
Không hard-code secret; không tự nâng role, nới validation hoặc bỏ test để pass.
Không dùng mock ở flow nghiệm thu rồi gọi là API thật.
Không sửa trực tiếp generated client; cập nhật contract và sinh lại.
Không đổi công thức tiền/công/phép ngoài đặc tả; đề xuất ADR nếu cần thay.
Không kết nối DB thật, gửi mail người thật, mua dịch vụ, deploy production hoặc
submit store trong task nếu chưa được tôi giao phạm vi đó.
Sau sửa code cập nhật Graphify theo quy tắc dự án và kiểm diff ngoài ý muốn.

Kết thúc: báo trạng thái READY_FOR_REVIEW hoặc BLOCKED, không tự VERIFIED.
Liệt kê file đổi, hành vi mới, test thực chạy, lệnh/kết quả, test chưa chạy,
rủi ro còn lại và hướng dẫn để tôi tự nghiệm thu trong 10–15 phút.
Nếu một task quá lớn, đề xuất chia subtask có acceptance, vẫn giữ phạm vi ban đầu.
Không tự chuyển sang TASK_ID tiếp theo.
```

## Prompt khởi động cụ thể

```text
Thực hiện T00: đọc repo và bộ kế hoạch HRM_KeHoach_DoAn_8Tuan, lập docs/scope.md,
docs/business-rules.md và ADR. Thời hạn của tôi là 2 tháng; một mình tôi kiểm duyệt,
AI code; mobile React Native, Android nghiệm thu trước. Chưa sửa code sản phẩm.
Chuẩn bị 10 ví dụ công/phép/lương có đầu vào và kết quả tính tay. Các chính sách
12 ngày phép, giờ làm, hệ số OT và thuế/bảo hiểm chỉ là dữ liệu demo đề xuất,
không tự coi là quy định doanh nghiệp. Trình các quyết định còn OPEN để tôi duyệt.
```

Sau khi bạn duyệt T00, dùng prompt chung với T01. Không chạy T01 trong cùng lệnh nếu yêu cầu T00 chưa được quyết định.

## Prompt review tách khỏi lượt viết

```text
Review diff của TASK_ID theo đặc tả và thẻ nhiệm vụ, chưa sửa code ở lượt review.
Tìm lỗi có thể tái hiện, ưu tiên: authorization/IDOR, mất dữ liệu, tiền sai,
transaction/concurrency, token lifecycle, migration, thiếu test quan trọng.
Đọc code thực, không chỉ tin báo cáo của lượt triển khai.
Mỗi finding: mức ưu tiên, file/dòng, tình huống đầu vào, hậu quả và test cần thêm.
Nêu rõ những điều chưa kiểm chứng. Không tạo lỗi giả để có đủ số lượng findings.
Nếu không có finding, vẫn ghi phạm vi đã xem và test chưa chạy; không tuyên bố
production-ready chỉ vì review không tìm thấy lỗi.
```

## Mẫu biên bản một task

```text
ID / commit / ngày / môi trường:
Trạng thái: READY_FOR_REVIEW | BLOCKED | VERIFIED (chủ dự án xác nhận)
Phụ thuộc đã đạt:
Hành vi trước → sau:
File và migration đổi:
Acceptance: từng mục PASS/FAIL/NOT RUN + evidence
Lệnh kiểm thử thực chạy và kết quả:
Kiểm tra UI thực hiện ở browser/device nào:
Giới hạn, lỗi còn lại, dữ liệu chưa kiểm:
Cách người kiểm duyệt tái hiện:
Quyết định của chủ dự án và lý do:
```

## 5 câu hỏi bạn phải trả lời trước khi nhận task

1. Tôi tự chạy được hành vi mới và một tình huống thất bại chưa?
2. Có test thật đúng lỗi/nghiệp vụ, hay chỉ ảnh/log AI tự mô tả?
3. Thay đổi có làm mất dữ liệu, đổi lịch sử lương, nới quyền hoặc vô hiệu kiểm tra không?
4. Tôi giải thích được luồng request, transaction và dữ liệu đầu vào/đầu ra chưa?
5. Có điều gì ghi NOT RUN mà lại bị dùng để kết luận hoàn thành không?

Nếu chưa trả lời được, task tiếp tục ở READY_FOR_REVIEW; yêu cầu AI hướng dẫn và sửa phần thiếu. Bạn không cần hiểu mọi dòng CSS, nhưng phải hiểu phần quyền, tiền, dữ liệu và cách kiểm thử chúng.
