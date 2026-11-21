# T18 — Web công, phép và hàng đợi duyệt

14/11/2026. **READY_FOR_REVIEW**. T17 đã được nghiệm thu kỹ thuật theo ủy quyền riêng trước khi mở T18; chưa tự nghiệm thu T18 hoặc làm T19.

## Hành vi mới

- `/attendance`: Employee vào/ra ca theo giờ máy chủ và xem lịch sử. HR đọc danh sách công có phân trang/lọc nhân viên, ngày và trạng thái; thấy thiếu giờ ra, điều chỉnh có lý do/version, xác nhận OT riêng. Payroll giữ quyền chỉ đọc theo policy backend hiện có.
- `/leave`: đọc quỹ theo năm, gửi ngày nguyên cùng năm, hủy Pending. HR duyệt/từ chối theo reason/rowVersion; nhận trạng thái thực từ server khi phiên khác đã xử lý. Xem lịch làm việc backend cho khoảng đơn, phân biệt ngày nghỉ/lễ với ngày tính phép.
- API đọc mới `GET /attendance` giữ scope của API đọc từng ngày. `GET /work-calendars/months/{year}/{month}/editability` chỉ trả trạng thái đóng tháng, không lộ bảng lương/số tiền. Không đổi công thức công/phép/OT, quyền hoặc schema DB.
- UI khóa thao tác với kỳ Published/Locked, không tự duyệt/tự điều chỉnh. Server tiếp tục kiểm lại khi trạng thái hoặc quyền đổi sau lượt đọc.
- Mutation mang UUIDv4 Idempotency-Key. Thử lại thủ công cùng action/target/version/nội dung dùng lại key; không auto retry. Cache liên quan được invalidate sau thành công; lỗi giữ bản nhập, không hiện thành công giả.
- Nhập giờ sửa theo UTC+07:00, giữ nguyên timestamp server có độ chính xác cao nếu trường không thay đổi. Phút/ngày hiển thị gọn bằng xử lý chuỗi, không chuyển sang float để tính hoặc thay request.
- Nhãn nhân viên lấy từ các ID của trang hiện tại qua bulk API có kiểm scope. Các form có ID riêng để nhãn không trỏ nhầm trường cùng tên ở form khác.

## Kiểm chứng

| Kiểm tra | Kết quả / bằng chứng trong `docs/verification/t18/` |
|---|---|
| Backend build | PASS, 0 warning/0 error; `backend-build.log`, `browser-build-resume.log` |
| SQL hồi quy | **395/395 PASS**, 0 fail/skip; `t18.trx`, `sql-tests.log`, `sql-regression.json` |
| Web unit/component | **27/27 PASS**; `web-tests-final.json` |
| Typecheck/lint/build cuối | PASS; `typecheck-accepted.log`, `lint-accepted.log`, `build-accepted.log` |
| Browser workflow | **6/6 PASS trong cùng lượt sạch**, 0 skip/retry; `playwright.json`, `playwright-resume.log` |
| Contract | Sinh client + 2 contract tests + Kestrel smoke thực chạy; OpenAPI live binary cuối khớp generated contract: `contract-live-final.json` |
| Dependency web | npm audit 0 vulnerabilities; `npm-audit.json`; không thêm dependency web |
| Visual cuối | `visual-final.json`, `visual-final.log`; desktop 1440×900, mobile 375×667/reduced motion, lịch đã tải và không tràn ngang trang |
| Đối soát DB/audit | `browser-sql-state.json`, `rollback-flush.json` và các assert trong `result.json` |
| Cleanup | SQL ledgers cleanupCompleted=true, host STOPPED, fixture/chứng chỉ tạm bị xóa |

395 ca SQL gồm 388 ca trước T17, 4 ca T17 và 3 ca T18 mới. Ca mới kiểm HR scope/Payroll policy/IDOR, lọc/phân trang tại SQL, phép chiếu lịch sử sau refactor giữ nguyên metrics, trạng thái kỳ đóng khớp gate ghi và không lộ dữ liệu payroll. Bộ cũ tiếp tục kiểm đua giao dịch công/phép, rowversion, idempotency và rollback.

Sáu ca browser thực tế dùng Edge profile riêng, Kestrel HTTPS, auth/cookie/CSRF thật và SQL Server fixture:

1. Hai HR mở cùng một đơn: một lần duyệt thành công, phiên cũ nhận 409, giữ reason và tải trạng thái Approved thật. Một đơn khác gửi khi còn đủ quỹ bị từ chối duyệt sau khi quỹ đã thay đổi.
2. Employee hủy Pending; HR khác từ chối đơn khác; UI và API cùng phản ánh Cancelled/Rejected.
3. HR có quyền Employee gửi đơn của mình: cả nút duyệt/từ chối đều bị khóa; gọi API approve trực tiếp nhận 403.
4. Fault audit trong assembly kiểm thử ném lỗi **sau SQL flush**: quan sát trong transaction là Approved/UsedDays=3, API trả 500, UI giữ bản nhập và không báo thành công. GET/đối soát sau rollback vẫn Pending/UsedDays=2.
5. Vào/ra ca với clock server được điều khiển trong test; sửa missing checkout và xác nhận 30 phút OT. Kỳ tháng 10 đã khóa không có nút sửa công hoặc duyệt đơn.
6. Lỗi trùng ngày và không đủ quỹ giữ form; đơn qua năm có lỗi rõ và không gửi. Lịch theo server sử dụng được ở 375px/reduced motion, trang không tràn ngang.

Đối soát cuối: quỹ Worker được seed 3 ngày riêng cho phép thử, **UsedDays=2, RemainingDays=1, đúng 1 debit và 1 audit leaves.approved**. Đơn rollback vẫn Pending và không có audit thành công mang marker rollback. OT tồn tại đúng 30 phút; ngày đã khóa giữ nguyên. Đây là dữ liệu giả cho kiểm thử, không sửa chính sách quỹ demo 12 ngày của sản phẩm.

## Lỗi đã tìm và cách sửa

- Form gửi đơn và modal quyết định cùng field reason có ID trùng, nhãn modal trỏ nhầm ô phía sau. Ca hủy đơn tái hiện lỗi thật. Đặt name riêng cho form công/phép và cả filter/editor/offboard nhân sự; chạy lại workflow hủy/duyệt xanh. Không sửa selector để bỏ qua lỗi nhãn.
- Assertion đầu tiên đòi decimal-string `2` trong khi SQL trả `2.00`: so cùng giá trị decimal mà không yêu cầu scale không được contract cam kết. Không đổi giá trị backend để test xanh.
- Đăng nhập lặp nhiều actor trên cùng IP chạm auth limiter 20/phút. Dùng lại cookie từ đăng nhập thật trong bộ nhớ test, không nới limiter hoặc bỏ CSRF.
- Hai unit test timeout khi chạy cùng bộ SQL/browser nặng; chạy riêng lại 27/27 xanh, không tăng timeout suite hoặc bỏ assert. Một thay đổi sau lượt unit xanh chỉ rút gọn số 0 dư khi hiển thị; đã typecheck/lint/build và kiểm tra live visual trên source cuối.
- Ảnh dài/ảnh phần tử được chụp khi chưa tải xong nhãn/lịch hoặc còn vị trí cuộn có thể tạo ảnh không đúng trạng thái mong muốn. Script visual cuối chờ dữ liệu, reset cuộn desktop và chụp viewport mobile thật; đã xem trực tiếp các ảnh cuối.
- Hai lượt seed harness lỗi độ dài mã nhân viên/CCCD đã tự dọn database. Sửa fixture theo schema, không nới giới hạn sản phẩm. Giữ log lỗi để không dùng lượt lỗi làm bằng chứng PASS.

## File và lệnh chính

Frontend: `web/src/features/attendance/`, `leave/`, các helper `commands`, `PeriodGuard`, `EmployeePicker`, `EmployeeLabels`, `display`; routes/layout/guard, Feedback, CSS và form names nhân sự. Backend: AttendanceController/AttendancePorts/AttendanceService và WorkCalendarsController/CalendarPorts/WorkCalendarService; OpenAPI/client sinh lại. Tests: `T18WebSqlTests.cs`, `HRM.T18BrowserReview`, `web/test/t18-commands.test.tsx`, `web/e2e/workflows.spec.ts`, `playwright.t18.config.ts`.

```powershell
# Từ repository root, dùng SQL Server kiểm thử cho phép tạo database GUID.
.\.dotnet\dotnet.exe build backend/HRM.Next.slnx -c Release --no-restore -m:1 -nr:false
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
.\.dotnet\dotnet.exe test backend/tests/HRM.Tests.Integration -c Release --no-build --logger trx
python -B scripts/contracts.py check --dotnet .dotnet/dotnet.exe --evidence docs/verification/t18-owner-contract-check
cd web
npm run typecheck
npm run lint
npm test
npm run build
```

Browser: theo phần T18 trong `web/README.md`. Harness phải mới cho mỗi lượt đầy đủ. Config chuyên T18 báo NOT_RUN/lỗi khi thiếu fixture, không coi skip là đạt. Đừng áp dụng harness vào HRM_System hoặc dữ liệu doanh nghiệp.

## Tự nghiệm thu 10–15 phút

1. Trong fixture/demo riêng, Employee vào/ra ca, xem lịch sử và gửi đơn đủ quỹ; xem lịch ngày nghỉ.
2. Hai phiên HR mở cùng đơn. Phiên đầu duyệt, phiên sau thử duyệt và tải quyết định thật sau lỗi. Kiểm quỹ chỉ trừ một lần.
3. Employee hủy đơn Pending khác; HR từ chối đơn khác. Gửi khoảng ngày trùng/qua năm và kiểm bản nhập không mất.
4. HR điều chỉnh ngày thiếu checkout có reason rồi xác nhận OT; kiểm giờ theo Việt Nam và kết quả từ server.
5. Xem kỳ đã khóa chỉ đọc, không có nút sửa/duyệt; dùng ca fault injection của harness để kiểm rollback, không bật fault này trong sản phẩm.

## Giới hạn

Chưa human UAT, remote CI, deploy hoặc mobile app. Không realtime giữa các trình duyệt; phiên khác cần tải lại để nhận cập nhật. Quỹ năm phải được cấp trước bằng nghiệp vụ/backend đã có, T18 không thêm màn hình quản trị cấp quỹ. Visual kiểm viewport nêu trên, chưa phải audit WCAG đầy đủ. Bundle còn cảnh báo chunk >500kB; chưa kiểm tải/performance production. Các lỗi read/mutation khác tiếp tục theo ProblemDetails hiện có; không hứa chống mọi lỗi mạng chỉ bằng UI. Chưa làm T19.
