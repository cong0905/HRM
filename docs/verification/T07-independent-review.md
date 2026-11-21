# Review độc lập T07 — cần sửa trước nghiệm thu

Ngày 11/11/2026. Kết luận: **CHƯA ĐẠT nghiệm thu T07**, giữ READY_FOR_REVIEW, không chuyển VERIFIED/T08.
Đã đối chiếu báo cáo Antigravity với code, ADR/đặc tả, TRX và thực hiện probe HTTP/SQL riêng.
T06 có biên bản signoff theo ủy quyền. Review này không thu hồi kết luận T06; các findings dưới đây ở code T07 mới.

## Bằng chứng nền

- `runs/T07-acceptance/result.json` PASS, 67/67 TRX thực thi/đạt, không skip; hash input trùng source hiện tại
  trước và sau probe. Các test đang có không bao phủ các tình huống dưới đây.
- [Kết quả probe SQL/HTTP](t07-review/probe-result.json), [log](t07-review/probe-run.log),
  [SQL ledger](t07-review/sql-evidence.json). Chỉ tài khoản/dữ liệu tổng hợp trong DB GUID fixture tự tạo/xóa.
- Probe dùng AuthHarness `testIdentity:false`: login/claims/policies/DbContext của API thật với SQL Server thật,
  HTTP qua TestServer; không phải fake auth hoặc in-memory database. Sender/keyring chỉ là cấu hình test.
- Không sửa code sản phẩm, test suite hay expected result. Chỉ thêm artefact review và console probe.
  Hai lượt đầu lỗi định vị content root của console harness, chưa thực hiện HTTP; đã sửa cwd theo manifest,
  DB của các lượt lỗi cũng được cleanup. Chỉ lượt thành công cuối là bằng chứng findings.

## Findings

### P1-01 — Người tính tự duyệt và công bố cùng kỳ lương

Nguồn: `backend/src/HRM.Application/Authorization/HrmPermissions.cs:65` cấp cả calculate/approve/publish cho
mọi Payroll; `backend/src/HRM.Api/Controllers/PayrollPeriodsController.cs:95` chỉ kiểm policy, không kiểm
actor đã tính; `Domain/Payroll/PayrollPeriodRecord.cs` không lưu calculatedBy/approvedBy.

Tái hiện: một user chỉ role Payroll gọi calculate → approve → publish trên cùng kỳ Draft.
Cả ba response **204**. Trái ma trận đã chốt: người tính không được tự duyệt chính run đó.
Tên permission khác nhau chưa tạo được separation of duties khi role tự nhận toàn bộ và không có resource guard.

Sửa: kiểm người tính/người duyệt trên resource persisted, permission độc lập và test cùng actor bị chặn,
actor khác đủ quyền mới được duyệt. Nếu workflow lương chưa đủ vì thuộc T14, business mutation chưa hoàn chỉnh
phải fail-closed/mặc định tắt; không để endpoint thật báo thành công bằng cách chỉ gán Status.

### P1-02 — Kỳ đã công bố bị mở ngược về Calculated

Nguồn: `backend/src/HRM.Api/Controllers/PayrollPeriodsController.cs:66`, cũng không có precondition ở approve/publish.
Calculate ghi `period.Status = "Calculated"` bất kể trạng thái cũ. Probe tạo kỳ **Published**, gọi calculate,
nhận **204** và đọc context độc lập xác nhận DB chuyển thành **Calculated**.

Trái đặc tả kỳ khóa/công bố bất biến; đồng thời có thể làm period và payslip Published không đồng nhất.
Cần guard trạng thái/version phía server và test Published/Locked bị từ chối, không đổi DB hoặc ghi audit
thành công giả. Không dùng nhãn vertical slice để bỏ qua invariant tại endpoint sản phẩm đang hoạt động.

### P1-03 — SystemAdmin đọc PII và nội dung nhạy cảm qua audit

Nguồn: `EmployeesController.cs:87-104` giữ nguyên phone/address trong Payload và `Reason = request.Reason`;
`AuditRepository.cs:53` trả nguyên Reason/Payload; `HrmPermissions.cs:76` cấp audit.read cho SystemAdmin.
AuditMasker chỉ xử lý theo tên property trong JSON, không xử lý Reason nằm ngoài JSON.

Tái hiện: HR sửa employee với lý do chứa CCCD tổng hợp và một marker secret thử nghiệm; SystemAdmin
không có HR/Payroll gọi audit-events nhận **200** và đọc được:
- CCCD đầy đủ trong Reason;
- marker secret nguyên vẹn trong Reason;
- địa chỉ riêng tư trong Payload.

Đây là đường vòng vào PII dù endpoint employee/payroll chặn SystemAdmin. Cần allowlist audit fields,
phân quyền/phép chiếu dữ liệu nhạy cảm, xử lý free-text Reason an toàn; không chỉ che property citizenId.
Vẫn giữ actor/trace/reason có ích, nhưng không ghi/cho đọc credentials hoặc CCCD thô. Bổ sung test trên
response audit và dữ liệu persist, gồm free-text và nested payload, không chỉ test helper MaskObject.

### P2-04 — Payroll được nhận toàn bộ CCCD/địa chỉ nhân viên

Nguồn: `HrmPermissions.cs:61` cấp employees.read cho Payroll; `EmployeesController.cs:50-57` dùng chung
EmployeeProfileResponse gồm CitizenId/Phone/Address cho mọi người có quyền đọc.
User chỉ role Payroll gọi GET employee khác nhận **200**, CCCD đầy đủ và địa chỉ trong response.

Đặc tả chỉ cấp Payroll thông tin cần tính lương, không mặc định toàn bộ hồ sơ. Cần DTO projection/field policy
theo mục đích/quyền; bảo vệ cả response PATCH và bulk/export khi bổ sung. Test Payroll có dữ liệu cần dùng
nhưng không nhận trường PII thừa. Nếu cần ngoại lệ nghiệp vụ, phải chốt permission/ADR rõ thay vì role blanket.

## Khoảng trống khác cần đối chiếu

- HR đọc/sửa hồ sơ chỉ kiểm employees.read/manage toàn cục, chưa có resolver phạm vi quản lý. Probe HR thuộc
  phòng A sửa employee phòng B nhận200. Không mặc định khác phòng ban luôn trái quyền vì đặc tả chưa định
  nghĩa mapping scope cụ thể; cần chốt/triển khai nguồn scope đáng tin cậy và negative tests, không tuyên bố
  đã hoàn thiện data scope HR chỉ từ test Employee own/other.
- Personal leave/payslip lists và payroll items đang ToListAsync không phân trang. Đặc tả danh sách default20/max100
  cần được áp dụng, không chỉ cho audit. Đây là kết quả đọc source, chưa stress bằng dataset lớn.
- Chưa thực hiện audit bảo mật toàn hệ thống, concurrency fuzz hoặc remote CI. Không nhận kết quả 67/67 là
  bằng chứng rằng mọi đường quyền/tiền đều đúng.

## Chạy lại probe

Từ root, dùng SDK đúng global.json và SQL test local:

```powershell
$env:DOTNET_ROOT=(Resolve-Path .dotnet).Path
$env:PATH="$env:DOTNET_ROOT;$env:PATH"
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
$env:HRM_T05_SQL_LEDGER=(Join-Path (Get-Location) 'docs/verification/t07-review/sql-evidence.json')
.dotnet/dotnet.exe run --project docs/verification/t07-review/probe -c Release
```

Console hiện ghi nhận hành vi sai chứ chưa là regression suite fail-closed. Anti cần chuyển từng kịch bản
thành test expected đúng, lưu red trước sửa và green sau sửa. Không sửa probe/expected để che findings.
Chạy full verify_t07 sau khi sửa, giữ T05/T06 và manifest legacy, cập nhật contracts/client/Graphify.
