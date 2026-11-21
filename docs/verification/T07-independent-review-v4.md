# Review T07 v4 — scope HR đạt ca kiểm tra, còn một lỗi audit assignment

Ngày 11/11/2026. Trạng thái: **READY_FOR_REVIEW, chưa đủ điều kiện VERIFIED**, chưa mở T08.
Đã đọc source, xác nhận gate Anti `T07-acceptance-v4` PASS83/hash khớp và chạy probe SQL/HTTP độc lập.
Không chạy lại toàn bộ83 test trong lượt này, không sửa code sản phẩm/test suite/expected.

## Các phần được xác nhận bằng SQL thật

- HR chưa được phân công GET employee403.
- SystemAdmin POST scopes204; cùng bearer HR hiện hữu GET employee200, không cần login lại.
- HR được phân công PATCH200; các ca password/token và CurrentPassword/NewPassword đều bị che trong audit200.
  Đây là ca mutation thực sự chạy, không lấy PATCH403 rồi audit rỗng làm bằng chứng che secret.
- Admin không mặc nhiên đọc PII: GET employee403.
- Admin DELETE scope204; cùng token HR GET employee403 ngay request tiếp theo.
- Missing calculator metadata: approve/publish409.
- Race payroll: calculate/approve/publish mới204; stale calculate409, period và payslip Published.

[Kết quả](t07-review-v4/probe-result.json), [log](t07-review-v4/probe-run.log),
[SQL ledger](t07-review-v4/sql-evidence.json), [source](t07-review-v4/probe/Program.cs).
Chỉ auth thật/TestServer/SQL Server thật với synthetic accounts và DB GUID tự tạo/xóa.
Giữ các probe/bằng chứng cũ, không ghi đè.

## P1-V4-01 — Dấu => khiến giá trị client_secret lọt qua audit

Nguồn: `backend/src/HRM.Application/Audit/AuditMasker.cs`, FreeTextAssignmentPattern:

```regex
(?:[:=]|=>|->)
```

Alternative `[:=]` đứng trước `=>`. Với `client_secret => 'BluebirdFixture!478'`, engine nhận `=` làm
separator, nhận `>` làm bare value rồi dừng trước khoảng trắng. Kết quả chỉ cặp nhãn/dấu bị thay,
giá trị trong nháy vẫn nguyên; các lần sanitize tiếp không còn nhãn để nhận diện giá trị đó.

Tái hiện với HR đã được gán scope hợp lệ, Reason:

```text
Support client_secret => 'BluebirdFixture!478'; access_token -> 'Qz9Xv2Lc7Nm5Rt8Yp4'
```

- PATCH200.
- SystemAdmin GET audit200: `clientSecretValueStillVisible=true`; dấu `->` bên cạnh đã che được.
- Query SQL độc lập: `credential_persisted_in_audit=true`.

Các giá trị trên chỉ là fixture, không phải credential thật. Lỗi vừa ảnh hưởng persistence vừa ảnh hưởng
response audit; defense-in-depth ở writer/reader không sửa được cặp đã bị che mất nhãn ở lượt đầu.

### Vì sao test hiện tại vẫn xanh

`Audit_CredentialAliases_VariantsAndNestedPayload_AreRedacted` dùng các value như `SecretVal999$`.
Fallback FreeTextSecretPattern lại khớp chính chữ Secret trong value, nên assertion không chứa toàn value
có thể xanh dù assignment parser đã sai. Cần value không chứa keyword password/token/secret để tránh
test được cứu bởi fallback ngoài cơ chế đang kiểm tra.

### Yêu cầu sửa và test

- Ưu tiên separator dài trước (ví dụ `=>|->|[:=]`), nhưng vẫn kiểm quote, whitespace và các alias thực tế.
- Nếu phát hiện credential label nhưng không parse chắc value, phải có fallback an toàn che cả đoạn/reason;
  không chỉ xóa nhãn và giữ nguyên giá trị nhạy cảm không nhãn.
- Parameterized tests dùng value không chứa keyword nhạy cảm; kiểm tất cả separator, nháy đơn/kép/bare,
  khoảng trắng, case, nested/array. Không hard-code marker để qua test.
- Integration dùng HR có scope, assert mutation200 trước khi kiểm audit; query cả SQL persisted và response.
  Giữ RED hiện tại và GREEN sau sửa, cùng tests scope/payroll/auth đã đạt.

## Phạm vi và bước tiếp

Chưa đánh giá mọi kiểu biểu diễn credential, toàn bộ scope combinations hoặc production. Không tuyên bố
regex nhận diện mọi bí mật vô danh. Finding này là định dạng có nhãn/=> được bản sửa tuyên bố hỗ trợ.
Quyết định HR chỉ nhân viên được phân công đã có trong `docs/decisions/T07-hr-scope.md`; không đổi lại.

Chạy lại probe như v3, thay project/ledger bằng `docs/verification/t07-review-v4/probe` và thư mục v4.
Sau sửa chạy full `scripts/verify_t07.py` với output mới, contract/client/audit dependency và Graphify.
Không đổi trạng thái VERIFIED hoặc làm T08 trước review.
