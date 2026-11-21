# Review T07 bản sửa v2 — còn ba finding chặn nghiệm thu

Ngày 11/11/2026. Trạng thái: **CHƯA ĐẠT nghiệm thu**, giữ READY_FOR_REVIEW, chưa làm T08.
Đã đọc bản sửa, đối chiếu gate T07-acceptance-v2 PASS72/72 với hash source khớp, và chạy probe SQL/HTTP mới.
Guard/test cho các ca tuần tự trước đã được bổ sung; review này xác định các đường còn thiếu, không phủ nhận
phần đã sửa và không coi 72 test hiện có là bằng chứng bao phủ mọi invariant.

## Bằng chứng

- [Probe thành công](t07-review-v2/probe-result.json), [log](t07-review-v2/probe-run.log),
  [SQL ledger](t07-review-v2/sql-evidence.json), [source probe](t07-review-v2/probe/Program.cs).
- Auth thật qua login bearer của sản phẩm; HTTP TestServer; SQL Server thật, dữ liệu/credentials tổng hợp.
  PauseCalculation chỉ là interceptor điều khiển thứ tự request ở test, không thay kết quả truy vấn hoặc quyền.
- DB GUID fixture tạo và xóa, không HRM_System/email/người thật. Không sửa product source, test suite hoặc expected.
- Một lượt đầu lỗi tên kỳ fixture vượt length20, đã rút tên đúng schema; một lượt kế tiếp thiếu field Address
  bắt buộc có mặt theo constructor nên PATCH400, chưa chứng minh audit. Lượt cuối gửi payload đúng contract,
  mutation200 và mới là bằng chứng audit. Các lượt lỗi/đầu giữ riêng, đều cleanup DB.

## P1-V2-01 — Audit che tên password/token nhưng giữ nguyên giá trị

Nguồn: `backend/src/HRM.Application/Audit/AuditMasker.cs:25-45`.
FreeTextSecretPattern khớp từ chứa password/token/secret, không tiêu thụ phần giá trị sau `=` hoặc `:`.
`SanitizeFreeText` thay nhãn bằng [REDACTED] nhưng giữ nội dung bí mật đằng sau. JSON primitive string cũng
không được xử lý giá trị tự do ở MaskNode; việc che theo property name không thay thế bảo vệ Reason.

Tái hiện với HR PATCH employee, Reason chứa hai giá trị tổng hợp dưới dạng `password=<value>; token=<value>`:
mutation **200**; SystemAdmin không HR/Payroll GET audit **200**; cả
`passwordValueStillVisible=true` và `tokenValueStillVisible=true`.
Ca cũ dùng marker TEST-ONLY-SECRET-NOT-A-REAL-TOKEN xanh vì chính marker chứa từ secret/token;
giá trị credential thực tế không bắt buộc chứa những từ đó.

Cần xử lý đầy đủ cặp tên/giá trị và free-text/nested strings, hoặc thiết kế allowlist/field projection an toàn
để audit không giữ/phơi bày raw credentials. Không hard-code marker test. Test nhiều định dạng gán, dấu nháy,
case, Bearer, nested payload và primitive arrays; kiểm cả dữ liệu persist lẫn response SystemAdmin.

## P1-V2-02 — Stale calculate vẫn mở lại kỳ đã Published khi concurrent

Nguồn: `backend/src/HRM.Api/Controllers/PayrollPeriodsController.cs:74-94` đọc trạng thái rồi ghi trong
transaction ReadCommitted bình thường; không có rowversion/CAS/khóa update bảo vệ từ lúc đọc đến ghi.
Guard Published/Locked chỉ kiểm trạng thái tại thời điểm SELECT, không bảo vệ bản ghi lúc UPDATE.

Probe dùng ba user Payroll độc lập, thứ tự có kiểm soát:
1. A đọc kỳ Draft, chuẩn bị ghi Calculated; interceptor dừng ngay trước SaveChanges SQL.
2. C calculate **204**, B approve **204**, B publish **204** trên kỳ đó.
3. Cho A tiếp tục. Request cũ trả **204**, SQL ghi đè trạng thái thành **Calculated**.
4. Context độc lập đọc thấy period **Calculated**, payslip vẫn **Published**, PublishedBy còn tồn tại.

Đây là vi phạm bất biến giống hậu quả cũ qua race, không được loại trừ bằng test tuần tự Published→calculate409.
Cần concurrency control thật cho toàn bộ các transition có liên quan: rowversion/CAS cùng preconditions hoặc
locking/transaction đủ để serialize. Stale write phải conflict/rollback, không được ghi đè kỳ công bố hoặc
ghi audit thành công giả. Bổ sung SQL concurrency regression test, kiểm trạng thái period/payslip và audit atomic.

Nếu sửa bằng locking, regression test cần cho các request tiến triển theo cơ chế serialize hợp lệ;
không giữ barrier yêu cầu writer khác phải hoàn tất trong khi writer đầu đang giữ lock. Probe hiện tại dùng
để chứng minh interleaving đang được cho phép, không áp đặt rằng một implementation có khóa phải cho phép nó.

## P1-V2-03 — Thiếu CalculatedBy vẫn được approve/publish

Nguồn: `PayrollPeriodsController.cs:144` và `:200` chỉ chặn self-approval khi `CalculatedBy.HasValue`.
Migration `20261111115438_PayrollPeriodAuditFields` thêm cột nullable và không backfill/đánh dấu các kỳ cũ
Calculated/Approved cần phục hồi. Vì vậy trạng thái hợp lệ trong DB sau nâng cấp có thể không có actor.

Tái hiện bằng bản ghi Calculated với CalculatedBy/CalculatedAtUtc null (tương đương dữ liệu trước migration):
cùng user Payroll approve **204**, rồi publish **204**. Không có căn cứ xác minh người tính khác người duyệt.

Cần fail-closed khi metadata/version phục vụ kiểm quyền thiếu. Không tự điền actor bằng người hiện tại để
cho qua. Chốt đường upgrade/recovery cho dữ liệu cũ: yêu cầu tính lại hợp lệ/duyệt lại hoặc trạng thái phục hồi
được kiểm soát; test cả Calculated thiếu người tính và Approved thiếu metadata duyệt khi publish.

## Phạm vi chưa chốt

- HR toàn công ty được bản sửa ghi là mặc định, trong khi đặc tả còn nói “theo phạm vi quản lý”. Việc một công ty
  chưa tự chứng minh mọi HR được xem/sửa mọi nhân viên. Cần quyết định/nguồn scope được ghi rõ; không tự coi
  phòng ban là scope và cũng không tự dùng thiếu mapping để cấp toàn cục. Chưa dùng điểm này làm lỗi runtime
  bổ sung vì cần chốt ngữ nghĩa quyền với chủ dự án.
- Các API calculate/approve/publish và leave approval vẫn là slice tối thiểu, không chứng minh đã hoàn tất
  nghiệp vụ T11/T14. Báo cáo “fail-closed” cần khớp hành vi mặc định của endpoint sản phẩm đang bật.
- Không chạy lại toàn bộ72 test trong lượt review này; đã xác nhận report/hash, đọc code và chạy probe mới.
  Chưa đánh giá toàn diện mọi route, remote CI hoặc production.

## Chạy lại

```powershell
$env:DOTNET_ROOT=(Resolve-Path .dotnet).Path
$env:PATH="$env:DOTNET_ROOT;$env:PATH"
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:HRM_T05_SQL_LEDGER=(Join-Path (Get-Location) 'docs/verification/t07-review-v2/sql-evidence.json')
.dotnet/dotnet.exe run --project docs/verification/t07-review-v2/probe -c Release
```

Console là diagnostic ghi lại hành vi, không phải bộ regression tự tuyên bố PASS.
Chuyển ba tình huống thành tests expected đúng, lưu RED/GREEN; sau sửa chạy gate T07 với output mới.
Giữ bằng chứng cũ, không sửa expected/hạ gate/skip để xanh.
