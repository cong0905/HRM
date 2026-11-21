# T11 — Quyết định nghỉ phép và quỹ phép MVP

Ngày 12/11/2026. Bản đã đối chiếu code sau review Codex. Bản Anti ban đầu được giữ tại docs/verification/t11-codex-review/anti-original-decisions.md.

- Lịch hiệu lực T09 xác định ngày có StandardMinutes>0; một ngày làm việc tương ứng một ngày phép, không tự lấy cuối tuần/ngày lễ làm ngày phép. Thiếu lịch trả422 calendar.not_configured. Khoảng qua năm bị422 leave.cross_year_unsupported; khoảng đảo bị422 leave.invalid_date_range.
- Chỉ nhận DTO nguyên ngày. JSON ngoài hợp đồng (ví dụ halfDay) bị400, không âm thầm tạo ngày đầy đủ. Không có mã leave.half_day_unsupported chuyên biệt trong implementation.
- Quỹ do HR có scope cấp tường minh một lần/người/năm qua POST /api/v1/leave-balances/grant. Không tự cấp mặc định; chưa có API cộng bổ sung quỹ sau grant. SystemAdmin thuần không vượt policy LeavesApprove của controller; không mô tả admin mặc định có quyền nghiệp vụ này.
- Pending không giữ chỗ hoặc trừ quỹ. Submit kiểm tra số dư hiện có và overlap Pending/Approved; approve kiểm tra lại số dư trong transaction.
- Approve cập nhật trạng thái, UsedDays, unique debit, audit và receipt trong cùng transaction. Employee application lock là sp_getapplock thuộc transaction trong SQL Server, có hiệu lực giữa nhiều process; không phải mutex chỉ trong process.
- Thứ tự khóa khi có key: receipt actor/operation/key → employee → target account → các tháng theo thứ tự. Approve/reject/cancel đọc trạng thái sau khóa employee; quyền sở hữu/scope hiện tại được kiểm trước replay. Create/approve dùng shared month lock, publish dùng exclusive cùng resource.
- Balance dùng UPDLOCK/HOLDLOCK và SQL RowVersion, CHECK UsedDays>=0 và AllocatedDays>=UsedDays. Ledger unique request/action, grant unique employee/year. Ledger/receipt được guard append-only qua EF SaveChanges sync/async; không khẳng định có database trigger chặn mọi SQL UPDATE/DELETE trực tiếp.
- Idempotency-Key optional theo hợp đồng hiện tại; khi gửi phải UUIDv4. Cùng key/body replay response gốc; khác body trả409 idempotency.payload_mismatch. Không key vẫn có trạng thái/unique debit chống trừ hai lần, không hứa replay thành công. RowVersion request optional; nếu gửi thì kiểm tra stale bằng409 leave.stale_version. Khóa SQL/RowVersion entity vẫn luôn áp dụng.
- Không tự duyệt/từ chối đơn mình; HR phải có assignment. Duyệt mới bị chặn khi target account inactive hoặc employee nghỉ việc; ngày nghỉ sau termination trả409 leave.approval_after_termination_forbidden, trường hợp inactive/terminated khác trả409 leave.employee_inactive_or_terminated.
- Kỳ Published/Locked chặn create/approve bằng409 leave.period_closed. Thiếu quỹ khi approve trả409 leave.insufficient_balance (submit trả422). Không coi mọi lỗi nghiệp vụ là400.
- Approved không được duyệt lần hai bằng key mới:409 leave.already_processed. Chỉ Pending được reject/cancel; cancel Approved bị409 leave.cancel_approved_unsupported. Hủy Pending không thay quỹ.
- Pending cũ thiếu WorkingDays (<=0, gồm migration default) bị409 leave.working_days_missing khi approve; cần hủy và tạo lại. Không tự đoán lịch lịch sử hoặc debit0.

Chi tiết các lỗi RED→GREEN và căn cứ ủy quyền: T11-codex-review-fixes.md, T11-delegated-technical-acceptance.md. UI, carryover, nửa ngày, nghỉ theo giờ, hủy Approved và công thức lương T13 nằm ngoài MVP. T12 sẽ kiểm bất biến công/phép liên module.
