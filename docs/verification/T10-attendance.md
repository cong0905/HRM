# T10 — Chấm công online và điều chỉnh có kiểm soát

Ngày 12/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền.** Codex và Antigravity cùng làm tại `D:\dotnet project`.

## Kết quả

- **Full gate PASS204/204**, không fail/skip: 174 ca trước và 30 lượt kiểm thử T10 mới.
- Build toàn bộ backend: 0 cảnh báo, 0 lỗi. Kiểm tra OpenAPI/client drift, TypeScript, Node client, Kestrel smoke và NuGet/npm audit đạt.
- 52 Python verifier tests đạt. Legacy đối chiếu đúng 8 lỗi baseline đã biết và 16 characterization tests đạt; không tuyên bố đã sửa 8 lỗi này.
- SQL Server thật, migration `20261112021157_OnlineAttendance` chạy trên DB GUID do fixture tạo; ledger xác nhận cleanup. Không sử dụng HRM_System hay gửi email thật.
- SHA-256 của toàn bộ đầu vào gate khớp bản cuối; source legacy `db/` giữ nguyên so với trước T10.

## Phân công thực tế

Anti dùng `agy.exe`, conversation `bd998461-ff94-49f1-a836-cd5287f3324d`, viết `AttendanceService.cs` và `AttendanceController.cs` theo hợp đồng do Codex chuẩn bị. Codex viết domain/DTO, persistence/migration/DI, khóa giao dịch với offboarding/publish, tests và client contracts; sau đó review và tích hợp.

SQL RED phát hiện 2 lỗi RowVersion: điều chỉnh không đổi giờ và xác nhận OT chưa buộc tăng version trong lần flush đầu. Anti sửa bằng cách đánh dấu UpdatedAtUtc modified; đồng thời sửa pagination và chặn employee ghi đè trạng thái HR đã chỉnh. Codex bổ sung Required header, kiểm tra DTO không cho giả userId, rollback sau SQL flush và kiểm thử cạnh tranh độc lập.

Lần Anti triển khai đầu exit0; một lần sửa trung gian timeout exit3 được giữ nguyên log, không tính là hoàn tất; lần sửa cuối exit0 (33.64 giây). Anti không tự chạy tests; Codex thực chạy kiểm chứng độc lập. Bằng chứng tại `docs/verification/t10-implementation/`: `execution.json`, `fix-execution.json`, `final-execution.json`, `anti*.stdout.log`, `anti-final-response.md`.

## Đã triển khai

1. Check-in/out dùng giờ server UTC và ngày nghiệp vụ UTC+7; không tin timestamp điện thoại. Pin phiên bản/định nghĩa lịch tại check-in; lịch đổi sau đó không tính lại ngày cũ.
2. Unique một summary/người/ngày. Events gốc, receipts, log điều chỉnh và OT append-only. Summary dùng SQL RowVersion.
3. UUIDv4 Idempotency-Key bắt buộc cho mọi mutation; scoped theo actor/operation, fingerprint nội dung chuẩn hóa. Retry trả response gốc; dùng lại key với nội dung khác trả409. Receipt, summary, event, audit trong một transaction.
4. Phút đủ điều kiện chỉ là giao của khoảng hiện diện với các khoảng làm việc, bỏ nghỉ trưa, giữ độ chính xác decimal. Thiếu checkout không tự có đủ ngày công. Trễ/về sớm tính phần giờ làm bị thiếu, không tính giờ nghỉ.
5. HR chỉ điều chỉnh nhân viên được phân công, bắt buộc lý do và RowVersion, không tự sửa công/duyệt OT của mình. Audit lưu trước/sau, actor, thời gian và lý do đã lọc dữ liệu nhạy cảm. Payroll đọc được projection công, không được sửa; SystemAdmin thuần không có quyền công ty mặc định.
6. OT là xác nhận riêng, một tổng phút/ngày, >0 và <=1440, ngày đã hoàn tất; không tự suy checkout muộn thành OT. Đã chốt OT thì workflow MVP chặn chỉnh công tiếp.
7. Dùng khóa SQL employee chung với offboard và khóa tháng chung với publish. Sau offboard không tạo công mới; HR vẫn sửa lịch sử hợp lệ đến ngày nghỉ việc nếu kỳ mở. Kỳ Published/Locked chặn ghi mới/điều chỉnh/OT; receipt cũ vẫn replay nếu người gọi còn đủ quyền.
8. HR xóa checkout có chủ đích thì ngày hiện tại Incomplete; employee không được ghi đè bằng checkout mới. Qua ngày thành MissingCheckout khi đọc. Đây là projection theo đồng hồ, không có GET ghi DB hoặc job ban đêm.

## API

Prefix `/api/v1/attendance`:

- GET `/me/today`, `/me/days/{day}`, `/me/history?page=&pageSize=`.
- GET `/employees/{employeeId}/days/{day}` theo scope/quyền.
- POST `/me/check-in`, `/me/check-out`.
- POST `/employees/{employeeId}/days/{day}/adjust`, `/overtime`.

Mutation yêu cầu Idempotency-Key; correction/OT còn yêu cầu RowVersion. DTO từ chối trường JSON ngoài hợp đồng; actor lấy từ session. Cookie mutation được middleware CSRF hiện có bảo vệ.

## Kiểm chứng trọng tâm

30 lượt T10 gồm 6 unit và 24 SQL integration: duplicate cùng/khác key, request mismatch/canonical JSON, checkout trước checkin, timestamp giả, thiếu/không hợp lệ key, forged userId, IDOR/scope, HR tự sửa, concurrent correction, no-op RowVersion, OT, rollback sau khi dữ liệu đã flush thật trong transaction, append-only cả sync/async, replay qua nửa đêm, pinned calendar, published month, offboard/checkin và publish/correction chạy đồng thời.

RED `docs/verification/t10-review/attendance-red.trx`: 23/25 đạt, 2 lỗi RowVersion. GREEN `attendance-green.trx`: 31/31 đạt, trong đó 30 lượt T10 và 1 ca dùng chung T09. Full gate cuối kiểm tra source sau cùng đạt204/204, không cộng ca dùng chung hai lần.

## Bằng chứng và chạy lại

- `docs/verification/runs/T10-final/result.json`: trạng thái, lệnh, matrices và source hashes.
- `integration/integration.trx`, `sql-evidence.json`: kết quả backend, migration và cleanup.
- `legacy/result.json`, `contracts/`: kiểm chứng legacy và OpenAPI/client.
- `docs/decisions/T10-collaboration.md`, `T10-delegated-technical-acceptance.md`: quyết định và căn cứ nghiệm thu.

Từ `D:\dotnet project`, chỉ định SQL test và thư mục bằng chứng mới:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t10.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T10-owner-rerun
```

## Giới hạn

Đã hoàn thành API/backend T10; chưa có UI web/mobile, GPS/geofence, face recognition, offline attendance hay bằng chứng địa điểm từ IP/HWID. Không hỗ trợ ca đêm hoặc amendment sau chốt OT. Công thức tính lương thật thuộc T13. VERIFIED là nghiệm thu kỹ thuật được ủy quyền, không ghi nhận chủ dự án đã tự review/UAT hay production đã sẵn sàng. Không commit/push/deploy; giữ nguyên các task khác.
