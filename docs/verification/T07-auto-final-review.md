# T07 — Anti sửa trực tiếp, Codex kiểm chứng độc lập

Ngày 11/11/2026. Phạm vi: sửa fallback audit cuối và giữ toàn bộ T05/T06/T07 đã đạt.
Chủ dự án yêu cầu trực tiếp: **“bảo anti làm luôn đi”**. Trạng thái: **READY_FOR_REVIEW**;
full gate Codex độc lập **PASS126/126** (84 integration +42 unit cases), không skip,
52 Python verifiers đạt; backend/legacy build0 warnings/errors, NuGet/npm audit sạch,
contract/generated client/type tests/Node/Kestrel đều đạt. Không tự VERIFIED/T08, không commit/push/deploy.

## Giao việc thực tế

- Task gửi qua Antigravity CLI: [T07-final-fallback-direct.md](../ai-handoff/T07-final-fallback-direct.md).
- Conversation ID: `363b82f7-b82e-4e7e-9aa5-fe193a25dcfe`.
- Hai lượt đầu bị giới hạn command/headless và build output trong sandbox; chưa sửa source.
  Không dùng skip-permissions, không né lệnh liệt kê thư mục đã bị từ chối.
- Tiếp tục cùng conversation bằng native file edits, không terminal/discovery: Anti đã sửa đúng hai file
  `backend/src/HRM.Application/Audit/AuditMasker.cs` và `backend/tests/Authorization/AuditMaskerTests.cs`.
- [Response thực tế của Anti](t07-auto-final/anti-response.md), [execution](t07-auto-final/edit-only-execution.json),
  [diff source](t07-auto-final/source.diff). Anti không chạy được test trong lượt edit-only; Codex chạy riêng.
  Phần RED/GREEN logic trong response Anti không được coi là log test thực chạy.

## Sửa gì

`StandaloneSecretTokenPattern.Replace` xóa label client_secret/api_key khi không có operator, trước khi
fallback xác định được value đã che. Anti đã bỏ bước xóa label đó và đồng bộ CredentialLabelPattern
với các alias của assignment parser. Nhãn chưa parse được đi tới fail-closed redaction toàn mệnh đề.
Giữ JSON/embedded JSON, assignment separators, masking PII và metadata actor/action/trace.
Thêm7 theory executions cho label không có operator và các alias liên quan; không thay expected cũ.

So với source gate T07-acceptance-v6, chỉ hai verification inputs trên thay đổi; không schema/package/
HR scope/payroll/auth changes. Các tài liệu, CLI log và probe là artefact bàn giao riêng.

## SQL RED/GREEN thực chạy bởi Codex

- [RED result](t07-review-v6/probe-red-result.json), [RED log](t07-review-v6/probe-red.log),
  [RED SQL ledger](t07-review-v6/sql-red.json): HR có scope PATCH200, admin audit200 vẫn thấy hai giá trị;
  `credential_persisted_in_audit=true`.
- [GREEN result](t07-review-v6/probe-green-result.json), [GREEN log](t07-review-v6/probe-green.log),
  [GREEN SQL ledger](t07-review-v6/sql-green.json): cùng điều kiện hợp lệ, hai giá trị không còn ở DB/API,
  `credential_persisted_in_audit=false`.
- Ca đơn, aliases, arrow và JSON credential đều không lộ value; không dùng mutation403 làm chứng cứ masking.
- HR unassigned403 → admin assign204 → assigned200; admin đọc PII403; revoke204 → HR403.
- Missing calculator approve/publish409; concurrent stale calculate409; period và payslip giữ Published.
- Chỉ auth sản phẩm/TestServer và SQL Server thật; synthetic accounts/credentials, DB GUID fixture tự tạo/xóa.
  Không HRM_System, email thật hoặc production. Không chỉnh source của probe để che finding.

## Full gate cuối

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t07.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T07-auto-final
```

[Kết quả gate](runs/T07-auto-final/result.json), [TRX](runs/T07-auto-final/integration/integration.trx),
[contract/client log](runs/T07-auto-final/contract-drift-and-client.stdout.log),
[legacy result](runs/T07-auto-final/legacy/result.json). Chạy lại dùng output mới hoặc bỏ --output.
Đã kiểm status PASS, toàn bộ executions passed/no skip và input SHA còn khớp sau gate.
[Metadata cleanup độc lập](t07-auto-final/sql-cleanup-check.log) xác nhận cả DB full gate và GREEN probe
còn0 trong sys.databases. [Graphify update log](t07-auto-final/graphify-update.log) đã hoàn tất.

Các finding đã tái hiện từ các lượt review trước và ca cuối không operator đều không còn tái hiện trong
probe hợp lệ này. Không có finding chặn còn mở trong phạm vi review đã nêu; giữ trạng thái AI bàn giao,
chủ dự án quyết định nghiệm thu VERIFIED.

Không coi finite tests là chứng minh mọi biến thể encoding/secret vô danh. Không chạy UI/device UAT,
remote GitHub CI hoặc production. 8 lỗi regression legacy vẫn là lỗi đã biết theo manifest.
Graphify code graph được cập nhật bởi Codex sau source/test edits; graph không thay runtime evidence.
