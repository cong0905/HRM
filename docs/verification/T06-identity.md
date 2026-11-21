# T06 — Identity, reset mật khẩu và quản lý phiên

Ngày 11/11/2026. Trạng thái **VERIFIED** ([signoff](../decisions/T06-signoff.md)). [Gate nghiệm thu cuối](runs/T06-acceptance/result.json) **PASS**:
57/57 integration (23 auth), không skip; 49/49 Python tests; backend và legacy build0 warnings/errors;
NuGet/npm không lỗ hổng, OpenAPI/generated client/type tests, 2 Node tests và Kestrel smoke đạt.
Legacy characterization8/runtime3/harness5 đạt; 8 bug regression vẫn fail đúng manifest.
T06 chưa được chủ dự án ký VERIFIED.
Phụ thuộc T05 đã VERIFIED theo [signoff](../decisions/T05-signoff.md) ở lượt review riêng trước thay đổi auth.
Phạm vi: Identity SQL store, auth API web/mobile, test `backend/tests/Auth`, migration, OpenAPI/client và CI.
Không chỉnh legacy authentication hoặc dùng lại reset token cũ.

## Thay đổi và đường chạy

| File/nhóm file | Hành vi |
|---|---|
| `backend/src/HRM.Infrastructure/Identity/HrmUser.cs` | IdentityUser<Guid>, IsActive; AccountSessions có UserId FK/stamp/nonce/expiry/revoked |
| `Identity/AccountService.cs`, `AccountMessages.cs` | Invitation, login, reset/change password, refresh CAS, revoke/logout; sender port và chế độ chưa cấu hình |
| `Persistence/HrmDbContext.cs`, migration `20261111103144_IdentitySessions` | Identity store, unique normalized username/email, session tables trong DB riêng |
| `backend/src/HRM.Api/Auth` và `Program.cs` | Cookie/bearer chuẩn, SQL validation mỗi request, CSRF, HTTPS, rate limit, auth routes |
| `backend/tests/Auth` (compile vào Integration project) | Real SQL auth matrix; test sender/log capture và ephemeral key ring chỉ trong test assembly |
| `contracts/openapi.v1.json`, `contracts/src/generated/api.ts`, type tests | Contract auth export từ runtime, generated client; không sửa tay generated file |
| `scripts/verify_t06.py`, verifier chung, Python negative tests, CI | Gate fail nếu thiếu/skip ca auth; Linux SQL gate phụ thuộc Windows legacy |
| [ADR-0009](../adr/0009-identity-sessions-reset.md), [backend README](../../backend/README.md), [contracts README](../../contracts/README.md) | Quyết định phiên, cách chạy, test và gọi auth |

Framework token được bảo vệ bằng purpose-separated ticket protectors của ASP.NET Core, không phải JWT.
DB chỉ lưu session/stamp/nonce, không raw token. Web cookie 8h tuyệt đối, access15 phút,
refresh7 ngày tuyệt đối, reset15 phút, invitation24h. Cookie Secure/HttpOnly/Strict; auth yêu cầu HTTPS.
Không có public registration/role escalation, account/password mặc định hay SMTP thật.

## Acceptance và bằng chứng

| Điều kiện | Kiểm chứng thực chạy |
|---|---|
| Username unique/policy/activation | SQL normalized unique; invitation inactive không hash, không login được trước activation; đúng token mới activate; reuse bị từ chối |
| Reset purpose/account/expiry/single use | Verify-email và activation token không reset được; token account khác/hết hạn/đã dùng trả400; mật khẩu cũ vẫn dùng được sau token sai/hết hạn |
| Reset concurrent | Hai HTTP request dùng cùng code: đúng một204, một400; login mật khẩu mới thành công; password và revoke cùng transaction |
| Cookie/bearer/refresh revoke SLA | Matrix sáu kiểu commit: password change, reset, disable, logout-all, stamp-only, inactive-only; cả ba credentials cũ đều401 sau commit |
| Logout semantics | Logout một session không ảnh hưởng session khác; cookie đã copy trước logout bị từ chối khi replay; logout-all thu hồi tất cả |
| Refresh/concurrency/replay | Một200/một401 cho cùng refresh đồng thời; stale replay401, phiên thắng còn dùng được; access/refresh không đổi chỗ; hạn7 ngày không kéo dài khi refresh |
| Refresh đua disable | Không500/deadlock ở kịch bản test; nếu token được phát trước disable commit, token đó vẫn bị401 ở request sau commit |
| Cookie/CSRF/HTTPS | Secure/HttpOnly/Strict; web login/logout thiếu hoặc sai token bị400; HTTP mobile login403 không phát token; header X-XSRF-TOKEN đúng mới qua |
| Expiry | TimeProvider tiến16 phút làm access401, refresh còn hợp lệ; cookie qua8h401; refresh quá7 ngày401 |
| Lockout/rate/enumeration | 5 sai làm lockout, password đúng lúc locked vẫn401; 20 auth request/IP/phút rồi429/Retry-After; missing/locked/invalid cùng mã auth.invalid |
| Email và log | Not-configured trả202 giống nhau cho email có/không tồn tại, ghi rõ chưa gửi; test-only sender được gắn nhãn; response/log không có code/password |
| Không tự nâng quyền | Không có public register; không tạo attacker/admin; account đăng nhập chưa có business permission vẫn403 departments |
| Migration dữ liệu | Upgrade từ InitialFoundation có Department tồn tại và Down về InitialFoundation vẫn giữ Department; migrate lại latest thành công |
| Composable offboarding | Revoke/disable trong UoW caller rồi lỗi → rollback cả tài khoản/session, token cũ còn hợp lệ; không tự commit nửa workflow |
| Contract/build/CI | Runtime schema/generated TS khớp, auth payload type checks, client HTTP smoke; locked restore/build/audit và legacy gate cùng entrypoint |

Lượt SQL refined trước gate: [55/55 TRX](t06/refined/integration.trx).
Full gate trước tăng assertion negative: [T06-final/result.json](runs/T06-final/result.json), PASS 57/57.
Bằng chứng chính là gate nghiệm thu cuối bên dưới, được chạy lại sau các assertion cuối cùng.

## Chạy và đọc bằng chứng cuối

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t06.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T06-acceptance
```

Env dùng lại fixture SQL nền tảng; database có prefix `HRM_Next_Test_T05_<GUID>` nhưng chứa cả T05 và T06.
Fixture chỉ xóa tên GUID chính nó tạo, không nhận tên database cần xóa từ caller. Chạy lại cần output mới
hoặc bỏ `--output` để sinh tên tự động; không ghi đè bằng chứng cũ.

| Chứng cứ | Vị trí |
|---|---|
| Full gate + source hashes + required auth matrix | [result.json](runs/T06-acceptance/result.json) |
| 57 integration cases (34 nền tảng +23 auth), không skip | [integration.trx](runs/T06-acceptance/integration/integration.trx) |
| SQL version, owned DB, applied migrations, cleanup | [SQL ledger](runs/T06-acceptance/sql-evidence.json) |
| Đọc metadata độc lập sau cleanup: database test còn0 | [SQL check](t06/sql-cleanup-check.log) |
| Build backend | [stdout](runs/T06-acceptance/build.stdout.log) |
| NuGet và npm audit | [NuGet](runs/T06-acceptance/vulnerable.json), [npm](runs/T06-acceptance/npm-audit.json) |
| OpenAPI/client/type tests + Node + Kestrel | [log](runs/T06-acceptance/contract-drift-and-client.stdout.log) |
| Legacy toàn bộ gate | [result](runs/T06-acceptance/legacy/result.json) |
| Python verifier negative tests | [log](runs/T06-acceptance/legacy/python-verifiers.stderr.log) |

Red/green: [red TRX](t06/red/auth-red.trx) phát hiện reset lifetime30 phút sai ADR15 phút ở lượt đầu;
đã sửa15 phút/refresh7 ngày/header theo ADR trước lượt xanh. Client smoke cũng phát hiện mã lỗi401
bị đổi ở lượt đầu; đã giữ mã T05 thay vì sửa expected result cho qua. Lượt lỗi giữ tại
`t06-contracts-update.log`; lượt client xanh tại `t06-contracts-check.log` và gate cuối.

Môi trường: Windows, .NET SDK10.0.404/runtime10.0.15, SQL Server16.00.1000, Node24.14.0/npm11.9,
Python3.12.10. Checkout có thay đổi local chưa commit; hash source trong result là mốc đối chiếu.
SQL auth tests dùng handler Identity thật và SQL Server thật; test sender/keyring không gửi email hoặc
ghi key sản xuất. Test reset expiry dùng lifespan âm test-only để token nằm trong quá khứ, không chờ15 phút;
đồng thời test riêng xác nhận configuration production15 phút. Test expiry cookie/bearer/session dùng clock DI.

## Nghiệm thu lại trong 10–15 phút và giới hạn

1. Chạy full gate mới theo lệnh trên, xem đủ57/57 và `auth_matrix` không thiếu case.
2. Đọc ba nhóm test: reset purpose/concurrency, revoke sáu tình huống và refresh race/replay;
   mọi negative test còn kiểm password/session state, không chỉ status code.
3. Theo backend README, chỉ migrate DB HRM_Next_Dev riêng. Chạy auth bằng HTTPS; GET csrf rồi web login
   gửi header, sau login lấy csrf mới. Không tạo default admin hoặc bypass CSRF để thử.
4. Account provisioning hiện là server-side port dành cho T08, chưa có UI/public admin provisioning.
   Để kiểm chứng ngay dùng test fixture SQL được gate tạo/dọn; không tự tạo account trong DB legacy.
5. Forgot-password hiện thông báo chưa có email; không có raw token/debug endpoint công khai.
   T22 phải triển khai outbox/SMTP và delivery status thật. Không gọi là gửi email thành công.

T07 còn permission/scope dữ liệu, T08 Employee FK/offboarding UI, T20 mobile secure storage,
T22 outbox và T25 deployment/keyring/proxy/TLS. Không thực hiện các task này trong T06.
Lưu ý kế hoạch có sẵn: một số mục T07 trở đi trong `backlog.json` ghi IN_PROGRESS trong khi thẻ nhiệm vụ
vẫn TODO. Lượt này chỉ đổi trạng thái T05/T06, giữ các mục ngoài phạm vi; không lấy nhãn JSON đó làm
bằng chứng đã triển khai hay nghiệm thu các task sau.
8 lỗi regression legacy vẫn fail đúng manifest; không tuyên bố đã sửa toàn legacy.
Chưa chạy GitHub CI từ xa, UI/browser/device UAT hoặc production; không có auth UI ở T06.
Đã chạy `graphify update .` sau sửa code; [log cập nhật](t06/graphify-update.log).
Graph là chỉ dẫn cấu trúc code, không thay bằng chứng runtime; doc semantic graph không được dùng để nghiệm thu.
Không commit/push/deploy.
