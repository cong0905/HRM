# T09 — Lịch làm việc, ngày nghiệp vụ và giờ server

Ngày 12/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền** bằng yêu cầu “tiếp tục đi quyết định thay t”.
Không ghi nhận người dùng đã tự đọc toàn bộ code; không coi kiểm thử backend là UAT hay production readiness.
Dự án đã sửa trực tiếp tại `D:\dotnet project`.

## Kết quả

- **Full gate PASS174/174**, không fail/skip: 27 ca mới T09 và nguyên bộ 147 ca trước đó.
- Backend full build: **0 cảnh báo, 0 lỗi**. NuGet/npm audit sạch; OpenAPI/client không drift; TypeScript type tests, 2 Node client tests và Kestrel smoke đạt.
- 52 Python verifier tests đạt. Legacy gate đối chiếu đúng 8 lỗi baseline đã biết và 16 characterization tests đạt; không tuyên bố đã sửa các lỗi baseline.
- SQL Server thật, database GUID riêng fixture tự tạo/xóa; ledger `cleanupCompleted=true`. Không dùng HRM_System, email thật hoặc deploy.
- Input SHA-256 khớp phiên bản sau gate, gồm business-rules và fixture JSON demo.

## Các quyết định đã chốt

1. UTC instant tách khỏi `DateOnly` nghiệp vụ Việt Nam UTC+7 (`Asia/Ho_Chi_Minh`); đồng hồ lấy từ `TimeProvider` được inject, không tin giờ client.
2. Lịch tiêu chuẩn 08:30–17:30, trưa 12:00–13:00. Giữ độ chính xác giây trong decimal phút, không làm tròn giờ trung gian. Ví dụ 10:00–19:00 chỉ tính 390 phút chuẩn; thời gian muộn không tự sinh OT.
3. Lịch chỉ thêm phiên bản mới, có ngày hiệu lực duy nhất; hỗ trợ thay đổi giữa tháng. Cấm ca qua đêm, giờ nghỉ giao nhau/ngoài ca và timezone ngoài phạm vi.
4. HR/SystemAdmin quản lý lịch công ty; các role nghiệp vụ được đọc lịch. Snapshot của kỳ lương vẫn cần quyền Payroll, SystemAdmin không được đọc mặc định.
5. Snapshot gắn duy nhất với PayrollPeriod, giữ nguyên định nghĩa lịch và số phút từng ngày. Hai request capture đồng thời cùng dùng một snapshot và một audit.
6. Lịch/snapshot bất biến qua cả EF sync/async. Phiên bản mới hồi tố ảnh hưởng kỳ Published/Locked bị từ chối; lịch kỳ sau không đổi snapshot kỳ trước.
7. Dùng đúng fixture demo T00: tháng 4 = 20 ngày/9600 phút, tháng 5 = 20/9600, tháng 6 = 22/10560; phương án tháng 4 không ngày lễ = 22/10560. Không coi dữ liệu demo này là lịch nghỉ pháp lý hiện hành.

## Đã triển khai

- Domain `CalendarMath`, `WorkCalendarDefinition`, `WorkCalendarVersion`, `CalendarPeriodSnapshot`.
- Application `BusinessClock`, `IWorkCalendarService` và DTO tách giờ UTC/ngày nghiệp vụ.
- Infrastructure service dùng SQL application lock để serialize thay đổi lịch và capture; unique indexes, transaction và audit rollback.
- API `GET /api/v1/work-calendars/now`, `GET /days/{date}`, `GET /months/{year}/{month}`, `POST /api/v1/work-calendars`.
- `GET/POST /api/v1/work-calendars/payroll-periods/{id}/snapshot`, có quyền Payroll riêng.
- Workflow calculate hiện tại pin lịch khi mã kỳ là `yyyy-MM` và đã cấu hình lịch. Không đổi công thức lương; calculate hiện tại vẫn là workflow metadata từ T07, calculator thực thuộc T13. Nhãn demo cũ hoặc chưa có lịch không được coi là bằng chứng tính lương thật.
- Migration EF `20261112013422_VersionedWorkCalendars` thêm hai bảng và unique index, không sửa dữ liệu lương/hồ sơ cũ.
- Sinh lại OpenAPI + TypeScript client; kiểm tra compile-time kiểu tháng/phút chính xác và giữ toàn bộ type tests T08.

## Kiểm chứng trọng tâm

- 4 boundary UTC/ngày Việt Nam, qua cuối tháng và năm nhuận.
- 7 ví dụ giao ca/nghỉ trưa, gồm giữ 30 giây thành 0.5 phút; không tự tính công đầy đủ khi thiếu checkout.
- Fixture JSON thật được test theo tổng ngày/phút T00; cuối tuần trùng ngày lễ không bị trừ hai lần.
- Thay lịch giữa tháng 6: 10 ngày đầu × 480 + 12 ngày sau × 420 = 9840 phút.
- SQL concurrent create cùng ngày hiệu lực: 1×201, 1×409; không có audit/bản ghi trùng.
- SQL concurrent capture: hai response cùng nội dung, đúng một snapshot/audit.
- Kỳ tháng 4 calculate → approve → publish qua API thật/SQL thật; thêm lịch tháng 6 không đổi snapshot đã chốt. Hồi tố tháng 4 trả409.
- Lỗi audit rollback cả cấu hình lịch hoặc snapshot và trạng thái Calculated của kỳ.
- Cookie/auth/IDOR/audit/employee lifecycle cũ tiếp tục qua toàn bộ gate.

RED/GREEN: `docs/verification/t09-review/calendar-initial.trx` phát hiện lưu EF đồng bộ chưa chặn xóa lịch (24/25). Sau sửa cả sync/async, `calendar-green.trx` đạt27/27; full gate cuối PASS174.

## Bằng chứng và chạy lại

`docs/verification/runs/T09-final/result.json`: trạng thái, lệnh, matrices và source hashes.
`integration/integration.trx`: 174 kết quả; `sql-evidence.json`: migration/SQL version/cleanup.
`docs/decisions/T09-delegated-decisions.md`: quyết định triển khai.
`docs/decisions/T07-T09-delegated-technical-acceptance.md`: căn cứ chốt nghiệm thu được ủy quyền.
`docs/fixtures/work-calendars.demo.json`: dữ liệu demo; không tự apply vào database.

Từ `D:\dotnet project`, dùng output mới:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t09.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T09-owner-rerun
```

Chạy riêng lịch:

```powershell
.dotnet/dotnet.exe test backend/tests/HRM.Tests.Integration -c Release --no-restore --filter FullyQualifiedName~Calendar_
```

## Phạm vi tiếp theo

T10 dùng `BusinessClock`, `IWorkCalendarService.DayAsync` và `CalendarMath.EligibleMinutes` để làm check-in/out. Lượt này chưa làm API chấm công, giao diện web/mobile, ca đêm, lịch nhiều nhà máy hoặc công thức lương T13. Không đổi trạng thái có sẵn của các task khác.
