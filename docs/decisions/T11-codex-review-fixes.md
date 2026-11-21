# T11 — Quyết định sửa sau review độc lập

Ngày 12/11/2026, chủ dự án yêu cầu “quyết định và làm giúp t đi nhé”. Codex được tiếp tục sửa và nghiệm thu kỹ thuật dựa trên kiểm chứng thực chạy.

Full gate bản Anti bàn giao được Codex chạy lại PASS233/233. Sáu ca SQL review bổ sung đều thất bại trên bản này; log RED và bản sao file gốc được giữ lại. Không lấy gate cũ làm bằng chứng cho source đã sửa.

1. DTO mutation từ chối JSON ngoài hợp đồng. Payload halfDay bị400 thay vì âm thầm trở thành đơn nguyên ngày. Không bổ sung nửa ngày/theo giờ.
2. Serialize Idempotency-Key theo actor/operation/key bằng transaction-owned SQL application lock trước khi đọc receipt. Key vẫn optional để giữ hợp đồng API hiện có; khi được gửi phải UUIDv4. Không có key, trạng thái và unique debit vẫn ngăn trừ lần hai nhưng không hứa replay response gốc.
3. Approve/reject/cancel chỉ đọc employeeId để định tuyến trước khóa employee; trạng thái/RowVersion được đọc lại sau khóa. Kiểm tra quyền sở hữu hoặc scope hiện tại trước trả receipt cũ. Retry đồng thời cùng key trả204/204 thay vì204/500.
4. Duyệt mới kiểm tra target account còn active, employee chưa nghỉ việc, ngày nghỉ nằm trong thời gian làm việc. Dùng UPDLOCK/HOLDLOCK tài khoản theo cùng thứ tự với offboard. Giữ lỗi approval_after_termination_forbidden khi ngày nghỉ sau termination; trạng thái/account inactive dùng employee_inactive_or_terminated. Receipt cũ không tạo nghiệp vụ mới nhưng vẫn phải qua quyền/scope hiện tại.
5. Create/approve giữ shared month lock theo thứ tự thời gian trước kiểm tra kỳ đóng; dùng cùng resource với exclusive publish của T10. Không chỉ kiểm tra Published rồi ghi mà bỏ khoảng race.
6. Đơn Pending cũ có WorkingDays<=0 (bao gồm giá trị mặc định migration) bị409 working_days_missing khi duyệt. Không tự suy dữ liệu lịch cũ, không ghi debit0; nhân viên cần hủy đơn chờ và tạo lại. Giữ nguyên lịch sử đã có.

Giữ việc cấp quỹ tường minh một lần/người/năm; không gọi đây là API bổ sung quỹ nhiều lần. RowVersion request vẫn optional theo hợp đồng cũ, khi gửi thì kiểm tra stale; RowVersion SQL và khóa/trạng thái luôn được áp dụng. Không triển khai UI, T12 hoặc công thức lương T13. Không commit/push/deploy/mail thật/HRM_System.
