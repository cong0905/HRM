# T05 — Backend và hợp đồng API nền tảng

**Cập nhật sau bàn giao:** T05 đã VERIFIED theo [biên bản review được ủy quyền](../decisions/T05-signoff.md).
Nội dung/bằng chứng dưới đây giữ snapshot triển khai ban đầu, trước phần auth T06.

- Ngày: 11/11/2026. Trạng thái: **READY_FOR_REVIEW**; AI hoàn thành triển khai và kiểm chứng,
  chủ dự án chưa ký VERIFIED. T02/T03/T04 đã VERIFIED trong backlog hiện tại.
- Checkout: `05254b306f1f882b118575f50c5c81b486f40c6c` cộng các thay đổi local chưa commit.
  Bằng chứng gắn với hash từng input trong `result.json`, không giả định HEAD đã chứa code mới.
- Môi trường thực chạy: Windows, SDK 10.0.404, runtime/EF 10.0.15; SQL Server 16.00.1000;
  Node v24.14.0, npm 11.9.0, Python 3.12.10. Kết quả này không chứng minh GitHub runner đã chạy.
- Kết quả cuối: [T05-final/result.json](runs/T05-final/result.json) **PASS**, input không thay đổi trong gate.
  Lượt đầu 30 test và lượt acceptance 31 test được giữ làm lịch sử; lượt cuối 34 test là bằng chứng chính.

## Hành vi trước → sau và file đổi

Trước T05 chỉ có ứng dụng WinForms và các test legacy. Sau T05 có API .NET 10 độc lập,
DbContext SQL scoped, transaction boundary dùng chung, query phân trang, OpenAPI và client TS.

| File / nhóm file | Nội dung |
|---|---|
| `backend/HRM.Next.slnx`, `backend/src/HRM.{Domain,Application,Infrastructure,Api}` | Bốn project, entity Department tối thiểu, port/UoW, SQL implementation, HTTP pipeline |
| `backend/src/HRM.Infrastructure/Persistence/Migrations` | Migration `20261111094103_InitialFoundation`, designer/snapshot; chỉ DB HRM Next |
| `backend/tests/HRM.Tests.Integration` | Fixture SQL tạo/xóa DB GUID, TestServer, test-only probes/identity, 34 integration/architecture cases |
| `Directory.Packages.props`, lockfiles mới | Thêm OpenAPI/Mvc.Testing 10.0.15, dùng CPM/pin .NET 10 hiện có |
| `contracts/` | OpenAPI runtime, generated TS, openapi-fetch client, lockfile, type/runtime tests và hướng dẫn |
| `scripts/contracts.py`, `scripts/verify_t05.py`, `scripts/tests/test_contract_drift.py` | Export/check/gate fail closed và test âm drift |
| `.github/workflows/ci.yml` | Giữ Windows legacy; thêm Linux SQL/contract gate phụ thuộc Windows |
| `backend/README.md`, `docs/adr/0008-*`, thẻ T05/backlog | Cách chạy, quyết định hạ tầng, evidence và trạng thái bàn giao |

Các sửa legacy có sẵn của T01–T04/cleanup được giữ nguyên; T05 không sửa công thức tiền,
quyền người thật, schema hoặc dữ liệu HRM_System. Không commit/push/deploy trong lượt này.

## Đối chiếu từng yêu cầu

| Yêu cầu T05 | Bằng chứng thực tế | Kết quả |
|---|---|---|
| Project boundaries, không trộn Domain/EF | `StartupAndArchitectureTests.ProjectDependencyBoundaries_AreEnforced`; Domain không package, Application chỉ Domain | PASS |
| Scoped DbContext, concurrent requests | 24 request song song SQL thật; context riêng giữa request, cùng context trong request; scope validation bật | PASS |
| Cancellation và TimeProvider | Hủy sau flush SQL rollback/clear tracker; fixed clock ở test info; CT đi từ controller/query tới EF | PASS |
| Unit of work, repository không tự commit | Add chưa nhìn thấy từ context độc lập; Execute commit; 2 INSERT flush rồi lỗi → 0 bản ghi | PASS |
| Tracked write / read không ghi | Rename tracked persist qua context khác; GET chỉ SELECT, 0 SaveCalls, tracker 0; kiểm trang 20+2 và max100 | PASS |
| DB riêng, không fallback/secret | Startup fail cho cấu hình trống, HRM_System/master, malformed; design-time cũng dùng env bắt buộc; không config secret trong project mới | PASS |
| ProblemDetails / lỗi an toàn | 400/401/403/409/422/500 có errorCode/traceId; SQL marker/stack không ra response; header giả vẫn 401 ở product | PASS |
| Health / migrations | SQL thật ready200; unavailable503/live200; Down về 0 làm ready503, migrate lại ready200 | PASS |
| Paging và conventions | Default20/max100, page/offset/sort lỗi trả400; decimal chính xác vượt JS integer, không nhận number/mất precision/thiếu amount; DateOnly/UTC | PASS |
| OpenAPI và generated TS | Runtime Kestrel export khớp schema, regenerate check khớp api.ts; type tests và 2 client tests đạt; client gọi HTTP thật | PASS |
| CI contract drift | Workflow YAML đã parse, Linux gate chạy cùng verifier sau Windows; test âm thêm route/đổi decimal type/schema rỗng đều bị từ chối | PASS tại local; remote CI NOT RUN |
| Build, audit và hồi quy | Backend/legacy full build 0 warnings/errors; NuGet/npm không vulnerability; legacy gate PASS theo manifest | PASS, xem giới hạn lỗi legacy |
| Graphify cập nhật | Chạy `graphify update .` sau thay đổi; graph chỉ dùng điều hướng, không thay test runtime | Xem `t05/graphify-update.log` |

SQL fixture chỉ tạo `HRM_Next_Test_T05_456979bc81a44bbc834ef1612d7eab3e` trong lượt cuối.
[SQL ledger](runs/T05-final/sql-evidence.json) ghi server version, migration đã áp dụng,
`createdByFixture=true`, `cleanupCompleted=true`. Fixture không nhận tên DB cần xóa từ người gọi.
Đọc lại metadata SQL Server sau gate xác nhận tên DB test này còn **0** bản ghi trong `sys.databases`:
[cleanup check độc lập](t05/sql-cleanup-check.log).
Migration test downgrade/upgrade chạy trên DB test này, không import dữ liệu legacy.

## Lệnh và bằng chứng

Full gate đã chạy từ root:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t05.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T05-final
```

Chạy lại dùng tên output mới hoặc bỏ `--output`; verifier từ chối ghi đè thư mục bằng chứng.

| Bộ kiểm tra | Kết quả / log |
|---|---|
| Locked restore + full build backend | 0 warnings, 0 errors: [build log](runs/T05-final/build.stdout.log) |
| Real SQL integration, startup, architecture | **34/34 Passed, 0 skipped**: [TRX](runs/T05-final/integration/integration.trx) |
| NuGet dependency/audit | [resolved](runs/T05-final/resolved.json), [vulnerable](runs/T05-final/vulnerable.json): không lỗ hổng |
| npm clean install/audit | [npm audit](runs/T05-final/npm-audit.json): total vulnerabilities 0 |
| OpenAPI/client drift + TS compile/type tests + 2 Node tests + Kestrel smoke | [log](runs/T05-final/contract-drift-and-client.stdout.log): PASS |
| Legacy full gate | [result](runs/T05-final/legacy/result.json): PASS; warning count 0 |
| Legacy characterization/runtime/harness | 8/8, 3/3, 5/5 đạt; TRX/log dưới `runs/T05-final/legacy/` |
| Legacy bug baseline | **8/8 vẫn Failed đúng assertion đã định danh**; [log manifest](runs/T05-final/legacy/baseline.stdout.log); không coi là đã sửa |
| Python verifier tests, gồm 4 drift-negative cases | **46/46**: [log](runs/T05-final/legacy/python-verifiers.stderr.log) |

CI Windows chạy `verify_t04.py`; Linux có container SQL 2022 mật khẩu sinh ngẫu nhiên, chạy
`verify_t05.py --ci-backend-only`. Job Linux phụ thuộc Windows PASS; scope backend-only được ghi rõ,
không thay bằng chứng full gate. Workflow đã kiểm tra cú pháp YAML; chưa push nên **remote CI NOT RUN**.
Image SQL dùng tag 2022-latest, CI in image ID; version thực được ghi trong ledger để biết phiên bản đã test.

## Chủ dự án kiểm tra trong 10–15 phút

1. Đọc kết quả PASS và ledger cleanup ở các link trên; đối chiếu 34/34 test, không skipped.
2. Chạy full gate với SQL Server test và một output mới theo lệnh trên (cần quyền tạo DB test).
3. Theo [backend README](../../backend/README.md), cấu hình `HRM_Next_Dev`, chạy migration rồi API.
   GET live/ready/info trả200; tắt SQL thì live vẫn200, ready503. Không cần sửa DB legacy.
4. GET departments không phiên trả401; không thêm header bypass. TestServer identity chỉ phục vụ kiểm chứng
   query thật; API sản phẩm chờ T06. OpenAPI trong Development không có `/_tests`.
5. Đọc [ADR-0008](../adr/0008-backend-foundation-contracts.md): giải thích một transaction hai bước
   rollback thế nào và vì sao tiền truyền string. [Contracts README](../../contracts/README.md) hướng dẫn sinh lại.

## Giới hạn còn lại

- Chưa có Identity, reset password/session hoặc scope bản ghi (T06/T07); chưa có UI/web/mobile hay API lương.
- 8 lỗi baseline legacy vẫn tồn tại đúng manifest; full gate ghi riêng, không tuyên bố tất cả legacy tests xanh.
- T02 legacy-SQL upgrade còn UNVERIFIED như backlog; T05 chỉ kiểm schema HRM Next trên DB test.
- Không kiểm DB production, email, staging, load, bảo mật toàn hệ thống hoặc UI UAT; không gọi là production-ready.
- Chủ dự án nghiệm thu và chuyển VERIFIED theo hợp đồng bàn giao; không tự mở task tiếp theo.
