# T17 — Web nhân sự và cấu hình lịch

Ngày 14/11/2026. **READY_FOR_REVIEW**, chưa tự chuyển VERIFIED. Chỉ thực hiện T17.

## Hành vi đã triển khai

- `/personnel`: danh sách trong phạm vi HR, tìm họ tên/lọc phòng ban/trạng thái/phân trang tại server, tạo hồ sơ, đọc/sửa qua drawer và xác nhận nghỉ việc. Không tải toàn bộ nhân viên để lọc tại trình duyệt.
- PATCH chỉ gửi trường được phép và thật sự thay đổi cùng reason/rowVersion. Không gửi CCCD, userId, email, role hoặc lương từ trình sửa. Gặp 409 giữ nguyên bản nhập, tải bản mới để so sánh; người dùng chủ động chọn dùng bản mới rồi nhập lại.
- `/my-profile`: hồ sơ riêng từ `/me/profile`, không có trình sửa HR. Khi truy vấn lỗi không hiển thị dữ liệu cache cũ như dữ liệu vừa đọc thành công.
- `/organization`: CRUD phòng ban/chức vụ có reason, transaction và audit. Kiểm tra tên cũ tránh ghi đè; khóa tuần tự việc ghi danh mục; FK Restrict và kiểm tra tham chiếu chặn xóa mục đang dùng.
- `/calendar`: lịch thực tế theo tháng và version từng ngày; tạo định nghĩa ca/ngày làm/giờ nghỉ/ngày lễ/ngày áp dụng. Preview gọi CalendarMath thật trên server, không ghi DB. Đổi biểu mẫu sau preview bắt buộc xem trước lại. Không sửa/xóa lịch cũ; giữ cơ chế từ chối ngày ảnh hưởng kỳ lương đã công bố/khóa.
- `/admin`: danh sách tài khoản phân trang/tìm tên đăng nhập, đổi vai trò, khóa tài khoản, chống tự đổi quyền/tự khóa; cấp scope HR rõ ràng bằng API đã có. SystemAdmin không được tự động đọc nhân sự hoặc lương.
- `/audit`: nhật ký có quyền audit.read, lọc hành động và phân trang, không render payload HTML.
- Sau tạo hồ sơ, UI báo đúng trạng thái chưa kích hoạt/chưa gửi thư và cung cấp mã hồ sơ cho quản trị viên cấp scope. Người tạo không tự được cấp scope theo phòng ban hoặc hành vi tạo.

## File chính

- `web/src/features/personnel/`: personnel/profile, organization, accounts và audit.
- `web/src/features/calendar/CalendarPage.tsx`; `web/src/features/shared/`: API adapter typed từ generated client, xử lý lỗi/xung đột.
- `web/src/App.tsx`, layout và product.css: routes/menu theo permission, bộ lọc, bảng có vùng cuộn riêng và drawer thích ứng mobile.
- Backend: EmployeesController thêm status filter/kiểm tra chức vụ thật; AccountsController thêm danh sách projection tối thiểu; WorkCalendarsController thêm preview không lưu; OrganizationController thêm CRUD.
- `Position`, HrmDbContext và hai migration `PersonnelOrganization`/`OrganizationReferences`: danh mục chức vụ và FK Restrict. Không áp dụng vào database sản phẩm trong lượt này.
- OpenAPI và generated client được sinh qua runtime, không sửa thủ công generated code.
- `backend/tests/Authorization/T17PersonnelWebSqlTests.cs`, fixture reset, `backend/tests/HRM.T17BrowserReview/`, `web/test/t17-api.test.ts`, Playwright T17 và cập nhật kiểm thử route đã thay placeholder.

## Kiểm chứng thực chạy

| Kiểm tra | Kết quả và bằng chứng |
|---|---|
| Build backend cuối | PASS, 0 warning/0 error; `t17/final-build.log`, binary riêng `backend/.t17-final/bin` |
| Hồi quy trước T17 | **388/388 PASS** trong `t17/t17.trx` |
| SQL T17 cuối | **4/4 PASS**, 0 skip; `t17/t17-final.trx`, `sql-final-t17.log` |
| Contract | Sinh client + 2 contract tests + Kestrel smoke PASS; `contracts-update-retry/`. OpenAPI của HTTPS harness binary cuối khớp contract: `contract-live-final.json` |
| Web | Typecheck/lint/build PASS; `typecheck-accepted.log`, `lint-accepted.log`, `build-accepted.log` |
| Unit/component | **23/23 PASS**, 0 skip; `web-tests-accepted.json` |
| Dependency web | npm audit 0 vulnerabilities; `npm-audit.json`; không đổi package/lock web |
| Browser thật | **6 kịch bản đạt qua các lượt**, không route.fulfill; chi tiết bên dưới |
| Cleanup | SQL fixture ledger đều cleanupCompleted=true, hai file fixture tạm đã bị xóa; `result.json`, `sql-*.json`, host logs STOPPED |
| Graphify | Đã chạy `graphify update .`; `graph-update.log` và `graph-update-final.log` |

Không diễn giải các lượt trên thành một lượt 392/392 xanh: lượt SQL đầy đủ ban đầu có 390 PASS/2 FAIL, cả hai lỗi thuộc ca T17 mới; 388 ca cũ đều PASS. Một lỗi thật là sửa danh mục thiếu AsTracking nên API báo thành công mà không lưu; đã sửa. Một lỗi fixture là tạo hồ sơ rồi PATCH khi chưa cấp scope HR; đã bổ sung cấp scope rõ ràng. Bốn ca T17 được chạy lại trên binary cuối và đều PASS. Hai migration thật được áp dụng vào database GUID kiểm thử.

Browser cuối dùng Edge profile riêng, Kestrel HTTPS và SQL Server 16, chứng chỉ/tài khoản tạm `example.invalid`:

1. HR tạo → SystemAdmin cấp scope qua UI → HR sửa số điện thoại; kiểm request PATCH chỉ gồm phone/reason/rowVersion. Hai phiên HR tạo xung đột SQL rowversion thật; bản nhập còn nguyên, tải bản mới để so sánh rồi lưu lại.
2. Employee chỉ xem hồ sơ riêng; direct URL nhân sự/quản trị nhận trang 403. Kiểm viewport 375px và reduced motion.
3. Preview lịch có 20 ngày/9600 phút từ server, lưu phiên bản và đọc lại tháng thật khớp.
4. HR tạo/sửa/xóa danh mục thật; SystemAdmin đọc tài khoản, nút tự đổi vai trò bị khóa và xem audit tương ứng.
5. Danh sách/drawer nhân sự không làm trang tràn ngang ở 375px/landscape; Escape đóng drawer. Ảnh drawer cuối chụp viewport, tránh full-page screenshot ghép nền phía dưới overlay cố định.
6. HR nghỉ việc trên web; cookie cũ, bearer cũ và refresh token cũ nhận 401. Bearer được kiểm 200 trước khi nghỉ việc, dùng context riêng không có cookie.

`playwright-final.log` ghi 5 PASS và một lỗi fixture mobile do mang cookie web nên CSRF trả 400. Sau khi tách context mobile, `playwright-offboard-final.log` PASS; `playwright-mobile-final.log` PASS cho ảnh viewport cuối. Đây là kết quả tổng hợp các lượt, không phải một report 6 PASS trong cùng một lần chạy. Test không nới CSRF để sửa fixture.

Ảnh: `personnel-desktop.png`, `personnel-conflict.png`, `own-profile-mobile.png`, `personnel-drawer-mobile.png`, `calendar-desktop.png`, `audit-desktop.png` trong `docs/verification/t17/`. Đã xem trực tiếp ảnh desktop, profile mobile và drawer mobile. Dữ liệu trên ảnh hoàn toàn là fixture.

Một component test đổi từ placeholder sang bảng AntD đầy đủ có thời gian dựng jsdom sát 5 giây. Đặt riêng test này 15 giây, giữ toàn bộ assert; không tăng timeout toàn suite. Adapter getComputedStyle của jsdom bỏ pseudo-element mà môi trường này không hỗ trợ; cuộn/viewport thật được kiểm riêng bằng browser.

## Chạy lại

Từ repository root, chỉ dùng SQL Server kiểm thử có quyền tạo database GUID; fixture tự dọn:

```powershell
.\.dotnet\dotnet.exe build backend/HRM.Next.slnx -c Release --no-restore -m:1 -nr:false
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
.\.dotnet\dotnet.exe test backend/tests/HRM.Tests.Integration -c Release --no-build --filter T17_ --logger trx
python -B scripts/contracts.py check --dotnet .dotnet/dotnet.exe --evidence docs/verification/t17-owner-contract-check
cd web
npm run typecheck
npm run lint
npm test
npm run build
```

Browser: theo phần T17 trong `web/README.md`; harness `HRM.T17BrowserReview` và `playwright.t17.config.ts` yêu cầu fixture thật, không coi thiếu fixture/skip là đạt. Khi chạy lại toàn bộ ca browser cần harness mới vì ca cuối đã offboard tài khoản Employee thử.

## Tự nghiệm thu 10–15 phút

1. Với môi trường demo riêng đã áp dụng migration, HR tạo hồ sơ dữ liệu giả; kiểm thông báo chưa gửi email khi chưa cấu hình kênh.
2. SystemAdmin mở Quản trị, tìm HR và Cấp phạm vi bằng mã hồ sơ vừa tạo. HR tải lại danh sách, lọc/tìm và mở hồ sơ.
3. Mở cùng hồ sơ ở hai phiên HR. Phiên đầu sửa điện thoại; phiên sau sửa địa chỉ, lưu gặp 409, so sánh bản mới rồi xác nhận dùng bản mới và nhập lại. Kiểm các trường khác giữ nguyên.
4. Kiểm danh mục và lịch: preview → đổi giờ → nút lưu vô hiệu cho tới preview mới; lưu version và đọc tháng thực tế.
5. Employee đọc hồ sơ riêng; HR offboard fixture, kiểm phiên Employee bị từ chối. SystemAdmin xem nhật ký.

## Giới hạn

Chưa human UAT, remote CI, deploy, mobile app UI hoặc gửi email thật. Đã kiểm chính sách kỳ lương khóa qua bộ hồi quy SQL hiện có, chưa tạo tình huống kỳ khóa bằng UI T19. Bundle web vẫn có cảnh báo chunk >500 kB; chưa kiểm tải/performance production. Không coi bộ kiểm tra này là audit WCAG đầy đủ. Cấp scope hiện hỗ trợ thêm theo mã hồ sơ; UI thu hồi scope chi tiết chưa nằm trong phần yêu cầu T17 đã triển khai, endpoint backend cũ vẫn giữ nguyên. Các màn hình công/phép/lương và T18+ không được triển khai trong task này.
