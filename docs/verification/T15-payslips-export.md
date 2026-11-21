# T15 — Phiếu lương, xuất CSV và đối soát

Ngày 13/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền.** Codex triển khai/kiểm chứng tại `D:\dotnet project` sau T14.

## Kết quả thực chạy

- **Full gate PASS 383/383**, 0 failed, 0 skipped: 347 lượt hồi quy + **36 lượt T15 mới (12 unit CSV, 24 SQL integration)**. Giữ đầy đủ matrices T05–T14, workflow35, payroll57, invariants16.
- .NET 10.0.404, Release/locked restore/no-incremental build đạt; NuGet/npm audits, OpenAPI/client drift, type tests, Node tests và Kestrel HTTP smoke đạt.
- **68 Python verifier tests PASS**. Thiếu SQL: NOT_RUN, executed0, exit2; không dùng InMemory hoặc skip để thay bằng chứng SQL.
- SQL Server thật trên GUID fixture, vẫn **12 migrations**, T15 không thêm migration. 5 DB của các lượt review/final đều cleanupCompleted=true; truy vấn read-only theo đúng tên xác nhận **0 database còn lại**.
- Source SHA-256 sau cùng khớp full gate. Source `db/` và toàn bộ file test cũ giữ nguyên; chỉ thêm tests mới, không đổi assertion/fixture hồi quy. Legacy gate khớp 8 lỗi baseline đã biết, không tuyên bố đã sửa 8 lỗi đó.
- Graphify đã cập nhật, CI workflow trỏ gate T15; **remote GitHub CI chưa chạy**. Không claim Anti đã tham gia T15.

## Trước → sau

Trước T15, CSV chỉ có ID/tổng tiền, không giới hạn dòng, không đối chiếu snapshot; không có chi tiết giải thích khoản tiền hoặc tổng đối soát toàn kỳ. Sau T15:

1. Chi tiết payslip có kỳ/trạng thái, snapshot/batch/version, policy demo, lịch, ngày công/phép/OT, các signed money lines và rounding từ chính snapshot đã lưu. Không tính lại từ lương hiện tại, không trả SourceJson/CCCD/ngân hàng/địa chỉ.
2. Payroll đọc preview Calculated/Approved bằng quyền read_all. Owner chỉ đọc bản Published của chính mình, kể cả kỳ Locked; draft hoặc ID khác chủ trả404. Employee/HR/SystemAdmin không tự có quyền payroll/export.
3. Summary COUNT/SUM toàn kỳ tại SQL, kiểm count/employee/snapshot/Net với đúng run. Nhiều nhân viên, rounding và tổng CSV đã đối chiếu cùng snapshot. Không cộng riêng page, không giả đối soát metadata-only legacy thiếu run.
4. CSV chỉ Published/Locked, permission export riêng, maxRows mặc định500/tối đa1000; vượt giới hạn trả409, không file bị cắt mất người. CSV có thành phần cộng/trừ, Gross/Net và version/snapshot; số dùng InvariantCulture. Tên/mã là nhãn hiện tại, tiền vẫn snapshot lịch sử.
5. UTF-8 BOM/charset utf-8/CRLF, quote và escape comma/quote/newline; prefix apostrophe cho text có =,+,-,@ kể cả sau whitespace/control/Unicode format. Tên tiếng Việt nhiều dòng được parse lại thành đúng một cell.
6. Audit tải xuống lưu actor/period/run/row count/format/trace, không lưu file, danh sách tên hay số tiền. Commit audit thành công trước khi trả file; lỗi sau SQL flush rollback audit và trả ProblemDetails. Audit không chứng minh browser đã nhận đủ byte.
7. Detail/summary dùng shared source applock trong transaction để đồng bộ với calculate/publish; không SaveChanges/DML. Interceptor SQL xác nhận read-only. Export GET ghi audit có chủ đích. Race publish/export chỉ trả nguyên bản đã Published hoặc409.

## Review và lỗi đã tái hiện

- **Export sai tiền:** tạo run thật, approve/publish, fault injection đổi Payslip.Amount=1 qua bulk SQL trong fixture (bypass guard chỉ để kiểm tra). CSV cũ trả200 thay vì409: `export-integrity-red.trx`. Thêm kiểm đối soát run/snapshot và output lines; `export-integrity-green.trx` PASS1/1. Full gate cuối chạy lại trên mã đã sửa.
- Lượt T15 riêng ban đầu đạt30/36, 6 lỗi fixture do cố đổi employee code bằng SaveChanges (guard T08 chặn đúng). Đã để guard nguyên và sửa fixture: chỉ seed legacy code không tin cậy bằng bulk SQL trong test; tên/CCCD marker vẫn qua context. Lượt `payslip-ready.trx` đạt36/36. Đây là sửa fixture, không nới validation sản phẩm.
- Kiểm chứng includes: owner/IDOR/unpublished; HR/Employee/Admin/multi-role; 2 employees và rounding0.50; row bound không trả partial file; signed lines/net; formula4 ký tự; tab/CR/LF/BOM/quote unit vectors; UTF-8 nhiều dòng; metadata-only/missing resources; salary/name đổi sau Locked; audit fault sau flush; read-only queries; publish/export race.

## API

Prefix `/api/v1`:

- `GET /payslips/{id}` — payroll.read_all, preview hoặc Published detail.
- `GET /me/payslips/{id}` — payroll.read_own, server lọc current user và Published state.
- `GET /payroll-periods/{id}/reconciliation` — payroll.read_all, tổng cả kỳ và Difference.
- `GET /payroll-periods/{id}/export?maxRows=500` — payroll.export, Published/Locked, tối đa1000, audited UTF-8 CSV.

List endpoints phân trang hiện có được giữ nguyên; tổng đối soát không lệ thuộc page đang xem. Decimal JSON vẫn string để TypeScript không mất chính xác.

CSV headers: PayslipId, EmployeeCode, FullName, PeriodCode, RunVersion, StandardPay, OvertimePay, Allowance, Bonus, OtherDeduction, DemoInsurance, DemoTax, RoundingAdjustment, Gross, Net, Status, SnapshotId. Các khoản trừ mang dấu âm; tổng signed lines=Net. Filename chỉ GUID kỳ và run version.

## File thay đổi và evidence

- `backend/src/HRM.Application/Payroll/PayrollReadPorts.cs`: contracts/service interface.
- `backend/src/HRM.Infrastructure/Payroll/PayrollReadService.cs`: snapshot read/reconciliation/CSV/audit.
- `backend/src/HRM.Api/Controllers/PayslipQueriesController.cs`: detail/own/summary; `PayrollPeriodsController.cs`: adapter export bounded; `DependencyInjection.cs`: đăng ký read service.
- `backend/tests/Payroll/T15PayslipSqlTests.cs`, `T15ExportIntegritySqlTests.cs`, `PayrollCsvTests.cs`.
- `scripts/verify_t15.py`, gate T05 integration, 4 Python verifier tests; OpenAPI/TS client/type tests; CI workflow.
- `docs/decisions/T15-payslips.md`, `docs/business-rules.md`.
- `docs/verification/runs/T15-final/result.json`, `integration/integration.trx`, `sql-evidence.json`, `contracts/`, `legacy/`.
- `docs/verification/t15-review/`: RED/GREEN, targeted/ready TRX, SQL cleanup ledgers/check, no-sql, Graphify update và completion-check.

Chạy lại full gate từ `D:\dotnet project`, dùng output mới:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t15.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T15-owner-rerun
```

Đọc nhanh để nghiệm thu: đối chiếu detail.Calculation.Lines với Net; summary.PayslipNet với CSV Net; gọi /me bằng chủ và user khác; kiểm CSV tiếng Việt/tên bắt đầu công thức; thử maxRows=0 và giới hạn nhỏ hơn số người; xem audit chỉ có metadata.

## Giới hạn

Không PDF, ký số, email đính kèm thật, chuyển khoản, UI/UAT/production load/deploy/remote CI trong task này. Lương vẫn policy demo, không tự tính PIT/BHXH pháp lý. Export lớn hơn1000 người cần workflow mở rộng; hiện fail-closed, không chia file âm thầm. Không có trigger ngăn mọi raw SQL của admin; fault injections chỉ trong test-owned DB.

Chỉ T15 chuyển VERIFIED theo ủy quyền; không coi chủ dự án đã tự review/UAT, không chuyển trạng thái task khác hoặc claim T16 hoàn tất. Không commit/push/deploy/mail thật hoặc dùng HRM_System.
