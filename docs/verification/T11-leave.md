# T11 — Nghỉ phép, giao dịch và chống âm quỹ

Ngày 12/11/2026. **VERIFIED theo nghiệm thu kỹ thuật được chủ dự án ủy quyền.** Anti triển khai; Codex review, sửa và chạy kiểm chứng độc lập tại `D:\dotnet project`.

## Kết quả cuối

- **Full gate PASS239/239**, 0 failed/skip: 204 lượt trước T11 +29 lượt Anti bổ sung +6 ca SQL review Codex.
- `leave_matrix`:35 lượt (8 unit,27 SQL); `attendance_matrix`:26 tên/30 lượt; `calendar_matrix`:15 tên/27 lượt; `t08_matrix`:21 lượt. Giữ toàn bộ auth/authorization matrices.
- Full backend build 0 cảnh báo,0 lỗi. NuGet/npm audit, OpenAPI/client drift, TypeScript type tests,2 Node client tests và Kestrel smoke đạt.
- 52 Python verifier tests đạt. Legacy gate khớp8 lỗi baseline đã biết và16 characterization tests đạt; không tuyên bố đã sửa8 lỗi baseline.
- SQL Server thật,10 migrations trên DB GUID riêng. Ledger và truy vấn metadata xác nhận cả5 DB của Anti/full gate độc lập/RED/GREEN/full gate cuối đều đã xóa. Không dùng HRM_System/email thật.
- SHA-256 đầu vào khớp source cuối; source legacy db/ giữ nguyên so với gate T10. Graphify đã cập nhật sau thay đổi.

## Review độc lập đã phát hiện và sửa

Codex chạy lại bản Anti tại `docs/verification/runs/T11-codex-review/`: **PASS233/233**. Sau đó6 ca SQL mới đều thất bại trên source này:

| Ca | Trước sửa | Sau sửa |
|---|---|---|
| Payload halfDay=true | 201, tạo đơn nguyên ngày | 400, không tạo đơn |
| Replay approve sau thu hồi HR scope | 204 | 403 |
| Duyệt mới cho target account inactive | 204, trừ phép | Bị chặn, giữ Pending/quỹ |
| Exclusive publication month lock đang giữ | Approve vẫn commit | Approve chờ giải phóng khóa |
| Hai approve đồng thời cùng key | 204 và500 | 204/204, đúng1 debit |
| Pending cũ WorkingDays=0 | 204, debit0 | 409, không duyệt |

`docs/verification/t11-codex-review/review-red.trx`:6/6 lỗi. `review-green.trx`:27/27 đạt (21 SQL Anti +6 ca Codex); race15 vòng của Anti được giữ. Sáu ca được thêm vào matrix bắt buộc, gate tăng từ233 lên239, không hạ điều kiện hoặc bỏ test.

## Hành vi và lựa chọn kỹ thuật

- Ngày phép theo phiên bản lịch T09; nghỉ cuối tuần/ngày lễ có StandardMinutes=0 không trừ phép. Chặn qua năm, đơn đảo ngày, không có ngày làm việc, overlap Pending/Approved.
- Chỉ nguyên ngày; trường JSON ngoài hợp đồng bị từ chối. Không hỗ trợ half-day/hourly qua việc bỏ qua field lạ.
- Grant tường minh một lần/người/năm, không tự cấp ngầm và chưa có bổ sung quota nhiều lần. Pending không giữ chỗ; approve recheck quỹ.
- Approve cập nhật trạng thái/quỹ/ledger/audit/receipt trong transaction. SQL application locks serialize receipt và employee; tài khoản dùng UPDLOCK/HOLDLOCK; month shared lock đồng bộ với publish exclusive lock. Không dùng mutex chỉ trong process.
- SQL CHECK bảo vệ số dư không âm, unique request/action chống debit trùng. EF guard bảo vệ ledger/receipt append-only qua SaveChanges sync/async.
- Scope/quyền sở hữu được kiểm trước replay, trạng thái được đọc sau employee lock. HR không tự duyệt/từ chối. Duyệt mới kiểm tra active/employment và kỳ đóng. Pending cũ thiếu ngày tính bị chặn để hủy/tạo lại.
- Cancel Pending không đổi quỹ; cancel Approved ngoài MVP. Lỗi stale/state/conflict trả409. RowVersion request và Idempotency-Key vẫn optional để giữ hợp đồng cũ; nếu gửi thì được kiểm tra. Không key vẫn chống double-debit nhưng không hứa response replay thành công.

## File và bằng chứng

Implementation chính: `backend/src/HRM.Infrastructure/Leave/LeaveService.cs`, `backend/src/HRM.Application/Leave/LeavePorts.cs`, `backend/src/HRM.Domain/Leave/LeaveModel.cs`, `backend/src/HRM.Domain/Personnel/LeaveRecord.cs`, DbContext/DI và controllers trong **HRM.Api**, không phải HRM.Web.

Migration thực: `backend/src/HRM.Infrastructure/Persistence/Migrations/20261112041500_LeaveTransactionAndLedger.cs` cùng Designer/Snapshot. Codex không cần sửa migration; đã chặn dữ liệu thiếu WorkingDays ở service.

Tests thực: `backend/tests/Authorization/LeaveMathTests.cs`, `T11LeaveSqlTests.cs`, `T11CodexReviewSqlTests.cs`. Gate: `scripts/verify_t11.py`, `scripts/verify_t05.py`. Contracts sinh lại qua `scripts/contracts.py`.

- `docs/verification/runs/T11-codex-final/result.json`: gate, lệnh, source hashes và matrices.
- `integration/integration.trx`, `sql-evidence.json`, `legacy/result.json`, `contracts/`: bằng chứng thực chạy.
- `docs/verification/t11-codex-review/`: RED/GREEN, cleanup metadata, graph query/update và bản báo cáo/quyết định Anti gốc đã lưu.
- `docs/decisions/T11-codex-review-fixes.md`, `T11-leave-transactions.md`, `T11-delegated-technical-acceptance.md`: quyết định cuối và căn cứ nghiệm thu.

## Chạy lại

Từ `D:\dotnet project`, dùng output mới:

```powershell
$env:HRM_T05_SQLSERVER='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
python -B scripts/verify_t11.py --dotnet .dotnet/dotnet.exe --output docs/verification/runs/T11-owner-rerun
```

## Giới hạn

Chưa có UI, carryover phức tạp, nghỉ nửa ngày/theo giờ, nhiều cấp duyệt, hủy/hoàn phép Approved hoặc công thức lương T13. Chưa thực hiện UAT/production load test/deploy/remote GitHub CI. Bất biến liên module công–phép thuộc T12, chưa coi T11 là bằng chứng đã hoàn thành T12.

Chủ dự án yêu cầu “quyết định và làm giúp t đi nhé”; VERIFIED là nghiệm thu kỹ thuật được ủy quyền, không ghi nhận chủ dự án đã tự đọc code hoặc làm UAT. Không commit/push/deploy; giữ nguyên trạng thái task khác.
