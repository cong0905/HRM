# T14 — Vòng đời lương versioned và nguyên tử

Ngày13/11/2026. Chủ dự án yêu cầu tiếp tục sau T13 VERIFIED; giữ quyền quyết định kỹ thuật/nghiệm thu đã giao. Không commit/push/deploy/mail thật/DB HRM_System.

## Hợp đồng mới

- POST /api/v1/payroll-periods tạo tháng yyyy-MM, SnapshotRequired=true ngay từ đầu; không salary/hire/calendar thì calculate fail-closed, không rơi vào metadata-only. Unique month, UUIDv4 key scoped actor/operation, replay create trả state gốc.
- GET /api/v1/payroll-periods/{id} chỉ projection: RowVersion, RunVersion và Active/Approved/PublishedBatchId. Token pair được kiểm lại dưới SQL locks khi ghi; GET không audit-write hoặc refresh snapshot.
- POST /api/v1/payroll-periods/{id}/workflow/{operation}, operation=calculate/approve/publish/lock; body reason>=10, RowVersion8-byte Base64, RunVersion>=0; UUIDv4 Idempotency-Key bắt buộc. Route dùng operation, tránh reserved route value action của MVC (đã có lượt RED404 trước sửa).
- Các route calculate/approve/publish cũ khi có key hoặc là tài chính thật chuyển vào cùng service, cũng yêu cầu token/run/key. Hợp đồng reason-only vẫn dành cho fixture metadata-only T07/T09 không salary source; không coi đây là luồng sản phẩm. T13 client helper được nâng để gửi key/version, không hạ assertion cũ.

## State và run binding

- Draft→Calculated→Approved→Published→Locked. Calculate rõ ràng từ Calculated hoặc Approved được tạo run mới; source/amounts/pointers và việc xóa phê duyệt cũ cùng transaction. Thất bại giữ nguyên run/approval/amount cũ, không xóa kỳ hoặc version cũ.
- Batch/snapshot T13 là immutable run history, Version tăng theo kỳ. ActiveBatchId gắn run hiện tại; approve gắn ApprovedBatchId; publish yêu cầu hai ID trùng, mọi payslip thuộc đúng batch, rồi gắn PublishedBatchId. RowVersion+RunVersion stale trả409.
- Người tính không tự approve hoặc publish run đó; kiểm CurrentUserId/permission hiện tại trước replay. Lock dùng Payroll.Publish permission, chỉ Published→Locked, không tạo duyệt hoặc thay tiền. Employee vẫn xem own Published payslip sau Locked.
- Capture snapshot trực tiếp T13 vẫn serialize và gắn actual calculator/active batch; không capture lại Approved/Published/Locked. Muốn tính lại Approved dùng workflow calculate có version/key rõ ràng.

## Idempotency, fault và isolation

- Receipt dùng bảng InputReceipts, op workflow.*, actor/key/hash; lock receipt→source→period/month. Replay chạy trước stale/version/state check sau khi quyền hiện tại đã hợp lệ, trả thành công gốc và không ghi run/audit lần hai. Replay approve run cũ sau recalc không phê duyệt run mới; caller đọc state gắn version.
- Hai key khác cùng expected version: chỉ một lệnh commit, lệnh còn lại409. Cùng key/body: cùng kết quả; mismatch409. Không TTL/reuse receipt trong MVP theo T12.
- Salary/manual shared source lock, calculate/approve/publish exclusive source lock; month exclusive dùng cùng resource với attendance/leave shared month lock. Profile employment đọc HOLDLOCK trong transaction để không lọt thay đổi giữa kiểm nguồn và commit. Audit/receipt/state/slips cùng transaction.
- Approve/publish so input fingerprint T13; salary/attendance/approved-leave đổi sau calculate hoặc approve đều buộc tính lại. Calendar tháng đã pin và dữ liệu Published luôn đọc từ snapshot.
- Sau publish, future salary chỉ ảnh hưởng kỳ sau; lock không re-evaluate nguồn sống vì quyết định công bố đã cố định. ORM guard Published chỉ cho seal Locked bằng lock metadata; Locked và Published slips/snapshots không được sửa/xóa qua SaveChanges sync/async.
- Fault sau SQL flush trước audit/commit phải rollback toàn bộ; mất completion sau commit trả500 có thể retry cùng key, không công bố lần hai. Các case fault chỉ được gắn trong test harness, không có endpoint fault trong sản phẩm.

## Migration, CI và giới hạn

Migration VersionedPayrollWorkflow thêm active/approved/published run refs và locked metadata, backfill refs bằng latest batch cho dữ liệu T13 hiện có; FK restrict. Fixture SQL clear refs trước cleanup GUID databases, không sửa legacy HRM_System.

CI chuyển gate T14, giữ T05–T13 matrices; no SQL=NOT_RUN/exit2, không skip/InMemory. Remote CI/UAT/production load/deploy chưa chạy. Công thức vẫn demo T13, không tự tính PIT/BHXH. Không sửa tiền kỳ khóa hoặc tự tính lại lịch sử; điều chỉnh sau khóa thuộc kỳ sau/quy trình tương lai.

Publication kiểm cả SnapshotId, EmployeeId và Amount của mỗi payslip bằng immutable snapshot.Net; không chỉ kiểm count/batch membership. Fault writer đổi draft Amount mà không đổi nguồn/run đã có RED204, sau guard phải409 và không ghi publication/audit.
