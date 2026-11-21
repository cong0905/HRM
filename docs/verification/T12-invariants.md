# T12 — Bất biến công/phép và phục hồi giao dịch

Ngày 12/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền.** Triển khai tại `D:\dotnet project`.

## Kết quả

- **Full gate PASS255/255**,0 failed/skip:239 lượt T11 và16 lượt SQL T12 mới. Giữ đầy đủ matrices T05–T11, thêm invariant_matrix16.
- Hai lượt SQL T12 riêng đạt16/16 mỗi lượt; tổng20 vòng race check-in/approve. Full gate chạy thêm10 vòng race. Suite T11 race15 vòng cũng được giữ trong full gate.
- Full backend build0 warnings/errors; NuGet/npm audit, OpenAPI/client drift, TypeScript,2 Node client tests và Kestrel smoke đạt.
- 56 Python verifier tests đạt (52 cũ+4 gate T12). Thiếu SQL đã kiểm chứng NOT_RUN, executed0, exit2; không fallback InMemory/skip.
- Legacy gate khớp8 lỗi baseline đã biết và16 characterization tests đạt. Không sửa source legacy db/ hoặc các file test cũ; không tuyên bố đã sửa8 lỗi này.
- Input SHA-256 khớp source cuối, gồm quyết định/bản đồ bất biến. Mọi database GUID sở hữu trong các lượt RED/GREEN/mutant/full gate đều cleanup; truy vấn metadata xác nhận0 DB còn lại.
- Graphify đã cập nhật. Không commit/push/deploy/mail thật hoặc kết nối HRM_System.

## Lỗ hổng RED và cách chốt quy tắc

Bản T11 đã VERIFIED được đối chiếu source trước khi bắt đầu. RED xác nhận `invariants-red-confirmed.trx`:13/16 đạt,3 lỗi — check-in vẫn thành công trên phép Approved, approve không chặn ngày đã có công, và race có thể cho cả hai bên thành công. Lượt đầu còn có lỗi fixture đăng nhập sau offboard; fixture đã sửa bằng token có trước offboard, rồi mới chốt RED xác nhận. Log ban đầu vẫn được giữ.

MVP chỉ có phép nguyên ngày: **ngày phép Approved có lịch chuẩn>0 không được nhận thêm hiện diện mở, phút công chuẩn dương hoặc OT**. Pending chưa tiêu quỹ nên vẫn cho chấm công; approve phải recheck hiện diện/công/OT trong cùng khóa employee.

HR có thể sửa công về khoảng đã hoàn tất có0 phút chuẩn với lý do/audit trước khi approve phép. Không xóa event gốc. Sau approve không khôi phục paid minutes/OT hoặc mở lại checkout. Ngày nghỉ trong khoảng phép có0 phút chuẩn, không trừ thêm phép và không tự sinh OT. Guard dùng định nghĩa lịch pinned của attendance, không âm thầm bỏ công cũ khi lịch mới đổi.

## Đã triển khai

- `backend/src/HRM.Infrastructure/Attendance/WorkLeaveInvariant.cs`: guard dùng chung trong transaction và employee SQL application lock hiện có.
- AttendanceService gọi guard cho check-in, check-out, HR adjustment và OT; LeaveService gọi trước approve/debit. Vi phạm trả409 work_leave.paid_day_conflict. Không thêm migration hoặc endpoint.
- `backend/tests/Authorization/T12InvariantSqlTests.cs`:16 lượt SQL kiểm scoped keys/canonical payload, retention400 ngày/API host khác, fault trước/sau commit cho cả2 module, audit rollback, race, ngày nghỉ, offboard, IDOR và physical defenses.
- `scripts/verify_t12.py`, `verify_t05.py`: min255, kế thừa toàn bộ matrices, bắt buộc16 lượt invariants, fail-closed khi thiếu SQL.
- `scripts/tests/test_verify_t12.py`:4 verifier cases complete/missing/skipped/NOT_RUN.
- `.github/workflows/ci.yml`: backend-sql-contract chạy T12 thay T07; vẫn phụ thuộc Windows legacy gate và tạo SQL Server container riêng. YAML đã parse/kiểm tra local; **chưa chạy remote GitHub CI**.
- `docs/business-rules.md`, `docs/decisions/T12-invariants.md`, `docs/verification/invariants.md`: quy tắc, seed, coverage và cách tái hiện.

## Fault injection và kiểm tra độ nhạy

- Trước commit: interceptor quan sát event/debit/receipt đã xuống SQL trong transaction rồi gây lỗi; đọc bằng context mới phải thấy rollback toàn bộ. Retry cùng key tạo đúng một kết quả.
- Sau commit: gây lỗi ở TransactionCommitted, API trả500 nhưng dữ liệu/receipt đã bền. Gửi lại qua API host khác trả thành công và không nhân đôi event/debit.
- Audit sau flush: trạng thái/quỹ/debit phải rollback cùng nhau. Mutation tạm commit trước audit làm test thất bại đúng vì SQL còn Approved thay vì Pending. `mutation-evidence.json` ghi expected failure và SHA source đã restore; mutant không cộng vào PASS.
- Physical negative controls cho unique summary(employee,date), unique debit(request,action) và CHECK không âm balance: schema gốc chặn SQL sai, tạm bỏ defense thì cùng SQL lọt qua. Rollback DDL/data và xác nhận trạng thái gốc. Chỉ dùng database GUID fixture.

Receipt được giữ lâu dài cùng hồ sơ, chưa có expiry/reuse trong MVP. Cùng key sau400 ngày và API host mới vẫn replay response gốc; đổi payload trả409. Attendance bắt buộc key; leave giữ key/RowVersion optional theo hợp đồng T11. Không key vẫn chống double-debit qua state/SQL unique nhưng không hứa response replay.

## Bằng chứng và chạy lại

- `docs/verification/runs/T12-final/result.json`, `integration/integration.trx`, `sql-evidence.json`, `legacy/result.json`, `contracts/`.
- `docs/verification/t12-review/`: confirmed RED, hai GREEN, mutant early commit, no-sql, graph update và cleanup metadata.
- Seed request `HRM-T12-seed-12026:<case>` → UUIDv4; race seed0..9 mỗi lượt suite. Winner có thể khác, bất biến không đổi.

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t12.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T12-owner-rerun
```

## Giới hạn

Không có UI, calculator/formula lương T13/T14, nhiều cấp duyệt, half-day hoặc hoàn/hủy phép Approved. Chưa chạy remote CI, production load test/UAT hoặc deploy. Guard bảo vệ ghi qua service; không tự sửa dữ liệu lịch sử có sẵn hoặc khẳng định cross-module invariant được trigger SQL bảo vệ khỏi mọi raw SQL của quản trị viên.

VERIFIED là nghiệm thu kỹ thuật theo ủy quyền đã giao; không ghi nhận chủ dự án đã tự review/UAT. Chỉ cập nhật trạng thái T12, giữ nguyên task khác.
