# T08 — Hồ sơ nhân viên và vòng đời tài khoản

Ngày 12/11/2026. Trạng thái: **READY_FOR_REVIEW**. Chủ dự án nghiệm thu và quyết định VERIFIED.
Dự án: `D:\dotnet project`. T07 đã đạt kỹ thuật ở lần kiểm chứng trước; yêu cầu ngày 12/11 là tiếp tục T08. Không tự đổi nghiệm thu T07 hoặc chuyển T09.

## Kết quả đã kiểm chứng

- **Full gate PASS, 147/147 test**, không fail/skip. Ma trận T08 bắt buộc 21 ca; giữ toàn bộ 126 ca trước T08.
- Backend build: **0 cảnh báo, 0 lỗi**. Kiểm tra dependency NuGet/npm, OpenAPI drift, TypeScript type tests, Node client và Kestrel smoke đều đạt.
- Legacy gate PASS theo manifest hiện có: 8 lỗi baseline được đối chiếu đúng, 16 characterization tests đạt; không có tuyên bố đã sửa 8 lỗi này.
- SQL Server thật, database GUID riêng do fixture tạo và xóa. Ledger ghi `cleanupCompleted=true`. Không dùng database HRM_System hoặc mail người thật.
- SHA-256 của toàn bộ verification inputs khớp phiên bản sau gate.

## Hành vi sau thay đổi

1. Tạo hồ sơ cùng account Identity inactive, role cố định Employee, bản ghi outbox và audit trong một transaction. Lỗi Identity hoặc audit rollback; không để hồ sơ/account mồ côi.
2. Mã `NV{yyyy}{NNNN}` dùng bộ đếm theo năm với khóa SQL `UPDLOCK,HOLDLOCK`, unique index và giới hạn 9999. Năm tiếp theo có dải riêng; mã bất biến. Không dùng mã ngẫu nhiên hay MAX+1 thiếu khóa.
3. PATCH chỉ sửa trường được gửi; bỏ trường giữ dữ liệu cũ. `maritalStatus: null` xóa trường tùy chọn; null cho tên/số điện thoại/địa chỉ/ngày bắt buộc bị từ chối. Server và OpenAPI yêu cầu rowVersion. Hai request thật đồng thời: một 200, một 409; chỉ một audit mutation được lưu.
4. HR chỉ đọc/sửa/offboard nhân viên được SystemAdmin phân công. HR tạo mới không tự được thêm scope. DTO không cho sửa role, lương, employeeCode, userId hay trạng thái thông qua PATCH.
5. Offboarding lưu ngày thực tế và DaNghiViec, khóa account, đổi security stamp, thu hồi session cùng transaction. Cookie/bearer/refresh/login bị chặn sau commit. Tài khoản đã nghỉ không thể kích hoạt lại bằng token cũ hoặc token mới. Giữ payslip và leave history, không tự hủy đơn Pending; không duyệt ngày nghỉ sau thôi việc.
6. Migration nâng cấp từ schema T07 đã được thử với hồ sơ cũ: giữ ID, địa chỉ, liên kết user; backfill email/trạng thái/mã, tạo rowversion và tiếp tục bộ đếm đúng. Năm tạo mã dùng ngày Việt Nam UTC+7.
7. Outbox chỉ lưu metadata, không lưu/trả/log activation token. Sender test gửi sau commit. Sender chưa cấu hình báo `NotConfiguredPendingDelivery`; sender test lỗi báo `DeliveryFailedPendingRetry`, giữ workflow đã commit và outbox Pending. Không báo đã gửi mail thật.

## Đường dẫn thay đổi chính

- `backend/src/HRM.Api/Controllers/EmployeesController.cs`, `PersonalController.cs`, `LeaveRequestsController.cs` và `backend/src/HRM.Api/Program.cs`.
- `backend/src/HRM.Application/Personnel/EmployeePorts.cs`, `EmployeeLifecycleGuard.cs`.
- `backend/src/HRM.Domain/Personnel/EmployeeProfile.cs`, `EmployeeCodeCounter.cs`, `ActivationOutboxMessage.cs`.
- `backend/src/HRM.Infrastructure/Identity/AccountService.cs`, `Persistence/HrmDbContext.cs`.
- Migration `20261112005638_EmployeeLifecycleAndOutbox` và model snapshot do EF sinh lại; thêm SQL backfill trước unique index.
- `backend/tests/Authorization/T08EmployeeLifecycleSqlTests.cs`, `T08ReviewSqlTests.cs`; các test PATCH cũ được bổ sung rowVersion, không bỏ assert quyền/audit.
- `scripts/verify_t08.py`, `scripts/verify_t05.py`; OpenAPI và generated TypeScript client, type tests.

## Lệnh và bằng chứng

Chạy từ `D:\dotnet project`:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t08.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T08-owner-review
```

Dùng thư mục output mới cho mỗi lần chạy. Lệnh tự tạo/xóa database test riêng.

Bằng chứng cuối trong `docs/verification/runs/T08-final-v2/`:

- `result.json`: trạng thái gate, lệnh thực chạy, ma trận auth/phân quyền/T08 và input hashes.
- `integration/integration.trx`: 147 kết quả.
- `sql-evidence.json`: migrations, phiên bản SQL và cleanup.
- `build.stdout.log`, `contract-drift-and-client.stdout.log`, `legacy/result.json`.

Mốc RED trước T08: `docs/verification/t08-review/red-result.json`: GET chưa có rowVersion, hai PATCH hợp lệ đều 200 và lần sau ghi đè; POST create 405, offboard 404. Database test đã xóa.
Ca GREEN tương ứng nằm ở `Employee_Patch_TrueConcurrentRequests_OneSuccessOneConflict` và các ca T08 trong TRX cuối.

## Phạm vi còn lại

- Chưa có API ghi chấm công trong dự án hiện tại. Đã có guard vòng đời để T10 sử dụng; không tuyên bố kiểm chứng được đường chấm công chưa tồn tại.
- SMTP/worker gửi lại outbox hoàn chỉnh thuộc T22. Trạng thái Pending có thật, chưa phải email đã gửi.
- Danh mục Position chưa có, nên API từ chối PositionId khác null thay vì nhận ID không kiểm chứng. Department dùng danh mục sẵn có.
- T08 là backend/API; chưa làm giao diện web/mobile, import Excel hoặc salary history T13.

## Chủ dự án kiểm duyệt trong 10–15 phút

1. Mở `T08ReviewSqlTests.cs` và đọc ba ca: cập nhật đồng thời, thu hồi cookie/bearer/refresh và migration dữ liệu cũ.
2. Chạy filter sau để thử riêng T08 (SQL fixture tự dọn):

```powershell
.dotnet/dotnet.exe test backend/tests/HRM.Tests.Integration -c Release --no-restore --filter FullyQualifiedName~Employee_
```

3. Đối chiếu assertion giữ địa chỉ/hôn nhân khi bỏ trường, rollback account failure và giữ payslip/leave history. Sau khi chấp nhận, chủ dự án mới chuyển VERIFIED.
