# Review T07 v5 — gate độc lập đạt, còn fallback audit với JSON credential trong Reason

Ngày 11/11/2026. Kết luận: **chưa chuyển VERIFIED**, giữ READY_FOR_REVIEW, chưa làm T08.

## Kiểm chứng độc lập đã hoàn tất

Chạy lại `scripts/verify_t07.py` với output mới:
[T07-independent-v5/result.json](runs/T07-independent-v5/result.json) **PASS105/105**
(83 integration +22 unit cases AuditMasker, gồm21 theory cases và1 nested JSON fact), không skip.
52 Python verifiers, contract/client/type tests/Node/Kestrel, NuGet/npm audit và legacy gate đạt;
build0 warnings/errors. 8 lỗi baseline legacy vẫn fail đúng manifest, không được coi là đã sửa.
Source hash khớp trước/sau probe; không thay product source, test suite hoặc expected.

Probe riêng xác nhận các ca cũ đã sửa đúng:
- HR unassigned403 → admin assign204 → assigned200; admin đọc PII403; revoke204 → HR403.
- PATCH200 với password/token, CurrentPassword/NewPassword và `client_secret => 'value'`:
  audit200 không còn giá trị trong các ca đó.
- Thiếu actor approve/publish409; stale calculate409 và period/payslip vẫn Published.

[Probe result](t07-review-v5/probe-result.json), [log](t07-review-v5/probe-run.log),
[SQL ledger](t07-review-v5/sql-evidence.json), [source](t07-review-v5/probe/Program.cs).
Auth thật/TestServer/SQL Server thật; chỉ synthetic accounts và DB GUID tự tạo/xóa.

## P1-V5-01 — Reason chứa credential JSON vẫn lộ giá trị

Nguồn: `backend/src/HRM.Application/Audit/AuditMasker.cs`, FreeTextAssignmentPattern,
DanglingAssignmentPattern và bước cuối FreeTextSecretPattern.Replace.

Đầu vào là JSON serialize với tên credential của API, đặt trong field Reason string hợp lệ:

```json
{"CurrentPassword":"BluebirdFixture!478","NewPassword":"Qz9Xv2Lc7Nm5Rt8Yp4"}
```

Các giá trị chỉ là fixture, không phải credentials thật. Regex assignment/dangling cần operator ngay sau
tên + whitespace, nên dấu nháy đóng key làm cả hai bỏ qua cặp gán. Bước fallback cuối chỉ thay chữ
CurrentPassword/NewPassword bằng [REDACTED], giữ nguyên value phía sau dấu `":"`.
Reader chạy lại sanitizer cũng không cứu được vì label đã bị xóa trước đó.

Tái hiện với HR được phân công qua API quản trị:
- `json_reason_mutation_status=200`;
- SystemAdmin audit200: `currentPasswordValueStillVisible=true`, `newPasswordValueStillVisible=true`;
- query SQL độc lập: `credential_persisted_in_audit=true`.

Đây là JSON nằm **bên trong string Reason**, khác với object Payload mà MaskNode đã đi qua property.
Test nested object/array hiện có chưa chứng minh được ca string chứa JSON này. Lỗi thuộc cùng cơ chế
fallback xóa nhãn/giữ value; không yêu cầu phát hiện mọi secret vô danh trong văn bản bất kỳ.

## Hướng sửa để dừng vá từng định dạng

- Có thể parse JSON credential text khi hợp lệ, nhưng **không chỉ thêm một nháy vào regex rồi chốt**.
- Sau khi xử lý các assignment được hỗ trợ, nếu vẫn phát hiện label credential mà không tách được value
  chắc chắn, che toàn bộ đoạn/reason bằng nội dung an toàn. Không dùng fallback chỉ xóa label.
- Audit giữ action/actor/trace và reason/descriptor an toàn; không cần giữ text gốc nhạy cảm để có audit hữu ích.
- Test theo invariant: credential có nhãn không còn value trong DB/API, gồm quoted JSON keys, JSON camelCase/
  PascalCase, text JSON lồng, field aliases, separator chuẩn, quote hỏng. Dùng value không chứa keyword.
- Nếu chọn loại bỏ nội dung chưa parse chắc, test xác nhận redacted marker và metadata audit còn đủ;
  không nới validation hoặc bỏ test để cho qua.
- Giữ tất cả ca HR scope, payroll concurrency/metadata và auth đã đạt; chạy full gate output mới sau sửa.

## Phạm vi

Không tuyên bố đã review mọi endpoint/biến thể encoding hoặc production. Remote CI/UI chưa chạy.
105 test xanh là kết quả thực chạy nhưng chưa bao phủ finding này. Không kết luận các sửa cũ không có tác dụng.
Probe v5 giữ nguyên ca v4 và thêm credential JSON, không chỉnh expected để che lỗi hoặc ghi đè kết quả v4.
