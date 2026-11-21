# Review T07 v3 — hai lỗi payroll đã đóng, audit alias và scope HR còn thiếu

Ngày 11/11/2026. Kết luận: **chưa chuyển T07 VERIFIED**, giữ READY_FOR_REVIEW và chưa mở T08.
Gate do Anti chạy tại `runs/T07-acceptance-v3/result.json` PASS75, input hash khớp source hiện tại.
Lượt này đọc code và chạy diagnostic SQL/HTTP độc lập; không tuyên bố đã chạy lại toàn bộ75 test.

## Đã kiểm chứng đóng các ca cũ

- `password=<value>; token=<value>`: mutation200, SystemAdmin audit200 nhưng hai giá trị không còn trong response.
- Kỳ Calculated thiếu CalculatedBy: approve409, publish409.
- Race cũ: C calculate/B approve/B publish đều204; request calculate cũ A tiếp tục trả409.
  Context SQL độc lập đọc period Published, payslip Published, PublishedBy còn nguyên.
- Đã đọc mapping `.IsRowVersion()` và catch DbUpdateConcurrencyException ở các transition, guard metadata
  Calculate/Approve/Publish. Không coi việc đóng các ca này là chứng nhận toàn bộ mọi dạng dữ liệu đầu vào.

Bằng chứng: [probe result](t07-review-v3/probe-result.json), [log](t07-review-v3/probe-run.log),
[SQL ledger](t07-review-v3/sql-evidence.json), [source](t07-review-v3/probe/Program.cs).
Chỉ auth thật, HTTP TestServer và SQL GUID test; toàn bộ dữ liệu tổng hợp. Không sửa code sản phẩm hoặc
expected của test suite. Probe v3 giữ ca v2 và thêm alias/scope, không ghi đè kết quả v2.

## P1-V3-01 — Alias CurrentPassword/NewPassword vẫn lộ giá trị audit

Nguồn: `backend/src/HRM.Application/Audit/AuditMasker.cs`, FreeTextAssignmentPattern và SanitizeFreeText.
Regex assignment chỉ nhận tên đơn password/token/pwd…; CurrentPassword/NewPassword không khớp vì word
boundary. Regex FreeTextSecretPattern sau đó xóa tên field nhưng không xóa phần value.
Đây cũng là tên credential thực trong ChangePasswordRequest của API, không phải chỉ marker ngẫu nhiên.

Probe gửi HR PATCH với Reason `CurrentPassword=<giá trị giả>; NewPassword=<giá trị giả>`:
- mutation200;
- SystemAdmin không HR/Payroll GET audit200;
- `currentPasswordValueStillVisible=true`, `newPasswordValueStillVisible=true`.

Cần xử lý theo nhóm credential/alias/structured audit và allowlist thay vì thêm riêng từng marker để pass.
Bổ sung tests cho CurrentPassword/NewPassword, current_password/new-password, access_token/refresh_token,
Code/secret assignments đúng ngữ cảnh, dấu nháy và case, nested JSON/array; kiểm dữ liệu persist và response.
Mục tiêu không phải regex khớp mọi bí mật vô danh trong văn bản, mà không để các cặp credential có nhãn
đã biết lọt qua hoặc cho audit mặc định phơi bày input nhạy cảm chưa được xử lý.

## P1-V3-02 — HR chưa được phân công vẫn có quyền toàn cục

**Quyết định mới đã chốt trực tiếp với chủ dự án trong lượt review này:** HR chỉ quản lý nhân viên được
phân công. [Biên bản quyết định](../decisions/T07-hr-scope.md) thay phần scope còn mở/chuyển T08 trước đó.
Chưa có phân công thì mặc định từ chối truy cập người khác; không tự gán phòng ban làm scope.

Code hiện tại vẫn lấy `EmployeesRead/EmployeesManage` từ role HR, rồi cho đọc/PATCH employee bất kỳ.
SecurityContext chỉ có employee link của chính người gọi, không có phân công scope.
Probe tạo HR chỉ có role, không có hồ sơ/assignment; GET profile employee khác200, PATCH200.
Vì vậy báo cáo “không tự cấp HR toàn cục” chưa khớp source/runtime và lựa chọn vừa được chủ dự án chốt.

Cần mapping phân công employee tường minh, quyền gán/thu hồi riêng phía server, resource guards/query scoping
cho HR read/update/list/bulk/export và các nghiệp vụ HR liên quan như duyệt phép. Không cho tự mở rộng scope.
Thu hồi phân công có hiệu lực trên request mới; không phụ thuộc permission claim stale trong token.
Test 401, HR chưa có scope403, assigned được phép, unassigned bị chặn, bulk không trộn phạm vi,
thu hồi hiệu lực, không tự gán scope, role phối hợp HR+Payroll không làm lộ full PII ngoài HR scope.

## Giới hạn và bàn giao

- Hai finding trên cần sửa và regression SQL tests trước khi nghiệm thu T07; không làm toàn bộ T08.
- Không thay đổi các kết luận đã đạt của T06 hoặc của hai ca payroll vừa sửa đúng.
- Chưa kiểm mọi biến thể credential/concurrency, deployment, remote CI hay dữ liệu production.
- Fixture đã dọn DB GUID. Graphify code graph được cập nhật sau thêm source probe.

Chạy lại từ root giống probe v2, đổi project sang `docs/verification/t07-review-v3/probe` và ledger path
`docs/verification/t07-review-v3/sql-evidence.json`. Console ghi diagnostic, không phải regression PASS gate.
