# T13 — Công thức lương demo, salary history và snapshot

Ngày13/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền.** Codex và Antigravity cùng triển khai tại `D:\dotnet project`.

## Kết quả cuối

- **Full gate PASS312/312**,0 failed/skip:255 lượt cũ +57 lượt T13 mới (**33 unit,24 SQL integration**). Giữ đầy đủ matrices T05–T12, gồm invariant_matrix16,leave35,attendance30.
- Full build0 cảnh báo/0 lỗi. NuGet/npm audit, OpenAPI/client drift, TypeScript negative type tests,2 Node client tests và Kestrel smoke đạt.
- 60 Python verifier tests đạt (56 cũ+4 gate T13). Thiếu SQL đã kiểm chứng NOT_RUN/0 executed/exit2; không thay bằng InMemory/skip.
- SQL Server thật,11 migrations trên DB GUID fixture; mọi ledger cleanupCompleted=true và truy vấn metadata xác nhận0 DB sở hữu còn lại.
- Legacy gate khớp8 lỗi baseline đã biết và16 characterization tests đạt; không tuyên bố đã sửa8 lỗi đó. Source legacy db/ và assertion test cũ giữ nguyên; chỉ bổ sung cleanup/compile include trong fixture/project test.
- Source SHA-256 sau cùng khớp gate, gồm golden JSON và quyết định nghiệp vụ. Graphify và CI workflow đã cập nhật; **remote GitHub CI chưa chạy**.

## Codex và Anti đã làm gì

Anti thực viết `backend/src/HRM.Domain/Payroll/PayrollCalculator.cs` qua agy CLI, conversation `a5b5270c-3939-477a-b2f7-3bd9b5dae1e2`. Lần đầu exit0 (328.84s); lần sửa precision exit0 (85.28s). Log write/edit xác nhận chỉ calculator nằm trong phạm vi Anti sửa; model hợp đồng được giữ nguyên. Anti báo Tests NOT RUN; Codex thực chạy toàn bộ build/tests độc lập.

Codex làm model/ports, salary history/manual APIs, source locks, DbContext/migration/DI, snapshot/payslip integration, golden fixtures/bảng tính hữu tỉ và SQL tests. Không hai bên cùng sửa một file đang được giao.

## Lỗi đã tái hiện và sửa

1. **Precision:** 32 ca calculator đầu đạt, nhưng thêm base0.02/tháng22 ngày/trừ đúng0.02 thì rawNet thành -2e-28 và chặn nhầm Net=0. Anti sửa bằng cộng tử số theo ngày/đoạn rồi chia mẫu số tháng một lần; không epsilon/float hoặc làm tròn sớm. Ca RED nằm trong calculator-precision.trx; toàn bộ33 unit sau sửa đạt trong gate cuối.
2. **Calculator identity:** người tính qua endpoint snapshot sau một calculation trước đó có thể tự approve vì CalculatedBy chưa đổi (RED trả204 thay vì403). Codex sửa capture phải cập nhật CalculatedBy/CalculatedAtUtc/Calculated và xóa approval metadata cũ; người vừa capture bị403, người có quyền khác được duyệt.
3. Một lượt ghép regression lộ fixture T13 để lại salary sources, làm test metadata-only cũ chạy nhầm nhánh. Đã thêm Dispose/reset dữ liệu T13 sau mỗi test, giữ nguyên assertion cũ. Lượt payroll-ready đạt58/58 gồm57 ca T13+1 race cũ; hai validation cuối đạt2/2. Full gate kiểm tra lại toàn bộ source sau cùng.

## Công thức và dữ liệu

- Denominator là tổng phút chuẩn lịch công ty cả tháng đã pin, không cố định26 ngày và không dùng riêng ngày employee làm việc. Salary interval inclusive theo ngày; numerator salaryBase×creditedMinutes được cộng trước rồi chia một lần.
- Employee có employment giao tháng được tính, kể cả đã offboard/IsActive=false. Ngày ngoài hire..termination hưởng0. SQL đã xác nhận nhân viên nghỉ14/04 vẫn nhận phần10/20 ngày chuẩn.
- Missing checkout hưởng0 và chặn publish; thiếu hire/salary/calendar fail-closed. Paid leave chỉ lấy Approved. Công+phép dirty input cap theo chuẩn ngày (có test420 phút), không tự sinh OT; chỉ entry OT đã xác nhận được tính theo policy1.5/2/3.
- Gross=standard+OT+allowance+bonus. Net=gross-otherDeduction-demoInsurance-demoTax, mỗi khoản trừ một lần. KPI tắt/neutral khi thiếu hoặc có score. Không tự tính PIT/BHXH theo luật.
- Decimal giữ raw values; display VND round cuối AwayFromZero. RoundingAdjustment bảo đảm tổng signed lines bằng Net. Raw net âm vẫn chặn dù display=0.
- Salary append tăng ngày, đóng mốc cũ ở D-1; không overwrite base cũ. SQL unique(employee,date), một interval mở, CHECK range/positive; employee lock serialize race. Lương hiện tại đổi không làm đổi snapshot/Payslip đã Published/Locked.
- Manual chỉ có Allowance/Bonus/OtherDeduction/DemoInsurance/DemoTax; có quyền/lý do, void bằng version rồi thay thế, giữ số gốc. Không ghi kỳ Published/Locked hoặc employee ngoài employment kỳ. Tiền API string, tối đa2 chữ số lẻ; vượt precision bị400, không SQL round âm thầm.

## Snapshot và API

Snapshot batch version tăng, lưu input/source/output JSON theo giá trị, policy version, raw/display amounts và SHA256. Nguồn gồm lịch đã pin, employment/version, salary decisions, attendance/version, approved leave/version, confirmed OT và active adjustments/version. Lỗi một employee/audit sau SQL flush rollback cả batch/slips/calendar/metadata. Snapshot/batch/receipt và Published payslips được guard qua EF sync/async.

New endpoints (prefix `/api/v1`):

- GET/POST `/employees/{employeeId}/salary-history`.
- GET/POST `/payroll-periods/{periodId}/adjustments`.
- POST `/payroll-adjustments/{id}/void`.
- GET/POST `/payroll-periods/{periodId}/snapshots` (GET có version tùy chọn).

Các POST mới bắt buộc UUIDv4 Idempotency-Key. Scoped actor/operation/key; replay response gốc, mismatch409. Salary write cần Payroll.HistoryManage; manual/capture cần Payroll.Calculate; đọc cần PayrollReadAll. HR/Admin/Employee thuần không được xem/sửa salary. Employee xem own Published payslip qua endpoint hiện có.

Calculate hiện có tự tạo snapshot khi có salary source/SnapshotRequired. Approve/publish chặn âm/incomplete/missing snapshot và input hash stale. SQL source/month locks phối hợp với salary/manual/attendance/leave. Capture trực tiếp cũng là calculation, có đúng creator để chống tự duyệt. GET snapshot/items đã có query-write observer xác nhận không SaveChanges/INSERT/UPDATE/DELETE.

## Kiểm chuẩn10 fixture T00

8 fixture có số tiền được kiểm calculator + Python Fraction độc lập (không gọi product calculator/C# CalendarMath). Hai fixture05/06 là kiểm tra thiếu quỹ và hai thứ tự duyệt, đã chạy SQL đúng dữ liệu T00; không bịa số lương cho chúng.

| T00 | Kết quả trọng tâm |
|---|---|
|01|Net18.400.000|
|02|Sau HR correction Net14.320.000; trước correction0 phút ngày thiếu checkout/block|
|03|OT800.000; Net14.920.000|
|04|4 ngày phép qua weekend; Net15.810.000|
|05|Quỹ1/nghỉ3 ngày:422, không tạo đơn/đổi quỹ|
|06|Quỹ2, đơn2+1: kiểm cả hai thứ tự thắng; loser409, không âm|
|07|10 ngày rate cũ+12 ngày rate mới: standard15.600.000; Net14.262.000|
|08|Không KPI vẫn Net13.425.000|
|09|Snapshot locked vẫn base12.000.000/Net10.740.000 sau tăng lương kỳ sau|
|10|Net-1.000.000 bị chặn; void/replace deduction để Net0 rồi approve/publish|

Fixture09 chỉ được T00 cho base và locked Net; test nhập rõ OtherDeduction1.260.000 bằng hiệu hai số, không coi đây là tỷ lệ insurance/tax được T00 quy định. Ghi chú này có trong JSON/CSV.

## Files và evidence

- Domain `backend/src/HRM.Domain/Payroll/PayrollCalculationModel.cs`, `PayrollCalculator.cs`, `PayrollStorage.cs`.
- Application `PayrollCalculationPorts.cs`; Infrastructure `PayrollCalculationService.cs`, `PayrollDataLocks.cs`, DbContext/DI.
- API `PayrollInputsController.cs`, integration `PayrollPeriodsController.cs`, exception mapping.
- Migration `20261113100502_SalaryHistoryAndCalculationSnapshots` tại `backend/src/HRM.Infrastructure/Persistence/Migrations/`; sinh bằng design-only connection, apply chỉ qua GUID test fixture. Không chạy database update lên Dev/HRM_System.
- Tests `backend/tests/Payroll/PayrollCalculatorTests.cs`, `T13PayrollSqlTests.cs`; gate `scripts/verify_t13.py` và verifier/type/CI updates.
- `docs/fixtures/payroll-golden.demo.json`, `docs/decisions/T13-calculation.md`, `docs/business-rules.md`.
- `docs/verification/runs/T13-final/result.json`, `integration/integration.trx`, `sql-evidence.json`, `legacy/result.json`, `contracts/`.
- `docs/verification/t13-implementation/`: native Anti logs, exit codes và calculator hash; `t13-review/`: precision/identity RED→GREEN, SQL, independent-math, cleanup metadata và graph update.

Chạy lại từ `D:\dotnet project`, dùng output mới:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t13.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T13-owner-rerun
```

## Giới hạn/T14

Đây là engine chính sách demo, không phải bộ tính thuế/bảo hiểm hợp pháp tự động. Không có UI hoặc production UAT/load test/deploy/remote CI.

T14 chưa hoàn tất: calculate cũ chưa có durable command receipt, có thể tạo version mới khi gọi lại; typed snapshot mới có key/replay. Các record metadata-only T07/T09 không có salary source giữ compatibility, không coi là tính lương thật. Khi Approved bị stale, guard hiện chặn publication; workflow mở lại/tính lại Approved và lifecycle/run idempotency đầy đủ còn thuộc T14. Không tự đổi T14 VERIFIED từ kết quả T13 này.

Không tuyên bố có SQL trigger chặn mọi raw SQL hoặc exclusion constraint ngăn mọi salary overlap trực tiếp; ràng buộc ngày/một interval mở và API locks/order cùng bảo vệ luồng được hỗ trợ. Snapshot lịch sử giữ nguyên, không tự sửa dữ liệu cũ.

VERIFIED là nghiệm thu kỹ thuật được chủ dự án ủy quyền, không ghi nhận chủ dự án đã tự đọc code/UAT. Không commit/push/deploy/mail thật; chỉ thay trạng thái T13.
