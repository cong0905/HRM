# T14 — Vòng đời bảng lương nguyên tử và bất biến

Ngày 13/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền.** Codex triển khai và kiểm chứng tại `D:\dotnet project`.

## Kết quả đã thực chạy

- **Full gate PASS 347/347**, 0 failed, 0 skipped: 312 lượt hồi quy và **35 lượt SQL T14 mới**. Giữ đủ ma trận T05–T13; payroll_matrix 57, invariant_matrix 16.
- .NET 10.0.404, build Release/locked restore/no-incremental đạt. OpenAPI và TypeScript client đã sinh lại; drift, type tests, Node tests và Kestrel HTTP smoke đạt. NuGet/npm audit đạt theo gate.
- **64 Python verifier tests đạt**. Không cấu hình SQL: `NOT_RUN`, executed 0, exit 2; không thay SQL bằng InMemory hoặc bỏ qua test.
- SQL Server thật với **12 migrations** trên database GUID fixture. Cả 5 database dùng ở các lượt review/final đều `cleanupCompleted=true`; truy vấn read-only theo đúng tên xác nhận **0 database còn lại**.
- Legacy gate khớp 8 lỗi baseline đã biết và characterization tests đạt; không tuyên bố sửa 8 lỗi đó. Source `db/` và các test cũ giữ nguyên, ngoại trừ cleanup FK của fixture và helper gửi command T13 được nâng cấp key/version; assertion T13 giữ nguyên.
- Source SHA-256 cuối khớp full gate. Graphify và workflow CI đã cập nhật; **remote GitHub CI chưa chạy**.

## Hành vi đã triển khai

1. Tạo kỳ thật `yyyy-MM` ở Draft, luôn yêu cầu snapshot. Workflow Draft → Calculated → Approved → Published → Locked; mỗi calculation tạo immutable batch version mới. ActiveBatchId/ApprovedBatchId/PublishedBatchId gắn quyết định với đúng run.
2. Command tài chính bắt buộc UUIDv4 `Idempotency-Key`, RowVersion và RunVersion. Kiểm permission trước replay; receipt khóa theo actor/operation/key và fingerprint payload. Replay cùng key trả kết quả gốc, khác payload 409. Hai key tính cùng expected version chỉ một command commit.
3. Tính lại kỳ Approved được phép trong một transaction: xóa approval cũ, tạo snapshot/run và thay draft slips đồng thời. Lỗi một nhân viên hoặc audit sau SQL flush khôi phục nguyên run Approved trước đó; giữ snapshot lịch sử. Replay approve cũ không duyệt run mới.
4. Approve và publish đều kiểm fingerprint hiện tại: salary, attendance, approved leave, manual/OT, employment và lịch đã pin. Nguồn thay đổi trả 409, yêu cầu tính lại. Source/month/period locks cùng HOLDLOCK đọc employment giữ tính nhất quán đến commit. Người tính không tự approve/publish run của mình.
5. Publish đối chiếu từng slip với snapshot của run được duyệt theo snapshot ID, employee ID và Net; trạng thái, số tiền, receipt, audit commit cùng nhau. Lỗi audit rollback toàn bộ; mất phản hồi sau commit replay không công bố lần hai.
6. Published/Locked không tính lại. Lock đóng bản đã công bố; không dùng nguồn salary mới để thay bản lịch sử. EF guard chặn tracked mutation kỳ Locked và chỉ cho Published chuyển sang Locked với metadata khóa. Published payslips/snapshots tiếp tục bất biến.
7. GET state/items/own payslip chỉ đọc, có interceptor kiểm không SQL write. Nhân viên chỉ đọc phiếu Published của chính mình, kể cả khi kỳ chuyển Locked; người thiếu quyền/khác phạm vi bị chặn.

## Lỗi đã tái hiện và sửa

- Route dùng tên reserved MVC `{action}` ban đầu làm 34 ca mới trả 404. Đổi thành `{operation}`; lượt targeted đạt 35/35 (34 ca T14 khi đó + 1 ca workflow dùng chung cũ). Không cộng ca cũ đó vào số 35 ca T14 cuối.
- Fault writer đổi riêng `Payslip.Amount` về 1 trong khi snapshot đầu vào/đầu ra vẫn nguyên: ca RED trả 204 thay vì 409. Thêm đối chiếu snapshot/employee/Net tại publish; ca GREEN trả 409 và giữ Approved/Draft, không có publication audit. Full gate cuối chạy lại trên source đã sửa này.

## API và quyền

Prefix `/api/v1`:

- `POST /payroll-periods`: tạo kỳ, permission Payroll.Calculate, key bắt buộc, body year/month/reason; trả 201 và state.
- `GET /payroll-periods/{id}`: permission Payroll.ReadAll; trả state, RowVersion, RunVersion và batch pointers.
- `POST /payroll-periods/{id}/workflow/{operation}`: calculate/approve/publish/lock; permission tương ứng Payroll.Calculate/Approve/Publish; body reason/rowVersion/runVersion, key bắt buộc; thành công 204.
- Endpoint calculate/approve/publish cũ chuyển vào workflow khi có key hoặc nguồn lương thật. Reason-only metadata fixtures T07/T09 không có salary source giữ compatibility. Kỳ tạo qua API mới luôn yêu cầu snapshot, không thể dùng compatibility để công bố kỳ thật thiếu run.

Người có Payroll.Publish có thể khóa bản đã Published. Lock không thay tiền hoặc duyệt lại run. Không có mặc định SystemAdmin/HR được đọc hoặc sửa payroll.

## File và bằng chứng

- `backend/src/HRM.Application/Payroll/PayrollWorkflowPorts.cs`.
- `backend/src/HRM.Infrastructure/Payroll/PayrollWorkflowService.cs`; tích hợp CalculationService/source locks, DbContext/DI và period model.
- `backend/src/HRM.Api/Controllers/PayrollWorkflowController.cs`, adapter trong PayrollPeriodsController.
- Migration `backend/src/HRM.Infrastructure/Persistence/Migrations/20261113133038_VersionedPayrollWorkflow.cs`: pointers/lock metadata, FK restrict và backfill từ batch cuối cho record snapshot cũ. Sinh bằng design-only connection; apply qua GUID fixture.
- `backend/tests/Payroll/T14PayrollWorkflowSqlTests.cs`, `T14OutputIntegritySqlTests.cs`; `scripts/verify_t14.py`, verifier/type-contract/CI updates.
- `docs/decisions/T14-workflow.md`, `docs/business-rules.md` và generated OpenAPI/client.
- `docs/verification/runs/T14-final/result.json`, `integration/integration.trx`, `sql-evidence.json`, `legacy/`, `contracts/`.
- `docs/verification/t14-review/`: route-fix, output-integrity RED/GREEN, SQL ledgers, no-sql, cleanup-check, Graphify log và completion-check.

Chạy lại từ `D:\dotnet project` bằng output mới:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t14.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T14-owner-rerun
```

## Giới hạn

Đây là nghiệp vụ demo đã kiểm chứng kỹ thuật, chưa có UI/UAT/production load test/deploy hoặc remote CI. Không tự tính PIT/BHXH theo luật. EF guards không phải SQL trigger chống mọi lệnh raw SQL của quản trị viên. CSV và đối soát T15 chưa được coi là hoàn tất. Không tự sửa kỳ khóa, tính lại mọi lịch sử hoặc ghi database HRM_System.

Chỉ T14 được chuyển VERIFIED theo ủy quyền; giữ nguyên trạng thái task khác. Không ghi nhận Anti đã tham gia T14 hoặc chủ dự án đã tự review/UAT.
