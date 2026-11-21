# T12 — Bất biến công/phép, retry và ranh giới giao dịch

Ngày 12/11/2026. Chủ dự án yêu cầu “làm t12 đi”; tiếp tục quyền quyết định kỹ thuật và nghiệm thu đã giao trong chat. T12 dùng SQL Server thật, giữ nguyên dữ liệu legacy, không commit/push/deploy/mail thật.

## Quy tắc dữ liệu

1. Ngày phép Approved là nguyên ngày. Trên ngày có lịch chuẩn >0, không được tạo hiện diện mở, phút công chuẩn dương hoặc OT thêm cho ngày phép đó. Đơn Pending chưa tiêu quỹ, vẫn cho chấm công; khi approve phải recheck hiện diện/công/OT.
2. Nếu đã có công hoặc thiếu checkout, cần xử lý trước khi approve phép. HR được sửa một bản ghi về khoảng hoàn tất có0 phút chuẩn (ví dụ hiện diện ngoài ca) với lý do và audit; khi đó approve phép được. Không xóa event gốc. Sau approve, HR vẫn không được khôi phục công dương/OT hoặc mở lại checkout. Không tự hủy phép Approved/hoàn quỹ.
3. Attendance và leave cùng giữ khóa employee transaction-owned sp_getapplock đã có từ T10/T11. Guard phải chạy trong transaction đó sau replay và kiểm tra scope; race check-in/approve chỉ cho một phía thắng. Mọi check-in/out, HR adjustment/OT và approve đều gọi guard.
4. Định nghĩa lịch pinned của attendance quyết định ngày chuẩn của bản ghi đó. Guard approve kiểm tra các attendance conflicts bằng lịch pinned; nếu lịch mới biến ngày đó thành ngày nghỉ cũng không âm thầm bỏ công cũ. Đây là lựa chọn bảo thủ, không tự phân bổ lại lịch sử.
5. Ngày không làm việc trong khoảng phép không bị trừ ngày phép, công chuẩn=0; chấm công ngày này không tự thành OT. OT ngày nghỉ vẫn phải theo cơ chế xác nhận riêng hiện có, chưa có calculator lương T13.

## Idempotency và phục hồi

- Receipt được giữ lâu dài cùng hồ sơ trong MVP, chưa có expiry/cleanup/reuse. Không hứa cache TTL; không xóa receipt để tạo key mới. Test retry sau400 ngày và tạo API host mới phải trả dữ liệu gốc, khác payload bị409.
- Scope là actor/operation/key. Cùng UUID giữa check-in, check-out, tạo đơn, approve hoặc user khác không được trộn response. Body fingerprint dùng DTO chuẩn hóa, không phụ thuộc thứ tự JSON.
- Attendance bắt buộc UUIDv4. Leave vẫn giữ hợp đồng optional key/RowVersion của T11 để tương thích; có key thì kiểm tra và replay, không key thì trạng thái/unique debit chống trừ lần hai nhưng không hứa response replay. Đây là giới hạn rõ so với mục tiêu mọi sensitive command có key trong đặc tả chung.
- Fault trước commit: SQL đã có event/debit/receipt trong transaction nhưng tất cả phải rollback. Fault sau commit: API có thể500 vì mất completion, dữ liệu/receipt vẫn bền; retry cùng key qua host mới trả thành công, không tạo event/debit thứ hai.
- Fault audit sau flush: leave status/balance/debit phải rollback cùng nhau. Mutation experiment tạm commit trước audit phải bị test bắt vì Approved còn lưu; source được restore byte-for-byte trong finally trước mọi lần GREEN/full gate tiếp theo.

## Kiểm chứng và CI

- Seed request: `HRM-T12-seed-12026:<case>` → SHA256 → UUIDv4. Race dùng seed0..9; winner có thể khác, bất biến không đổi.
- Chạy suite SQL T12 ít nhất2 lần GREEN, rồi full gate; lưu TRX, lệnh, SQL migration/cleanup và input hashes.
- Negative controls trên3 ràng buộc thật: unique summary(employee,date), unique debit(request,action), CHECK không âm balance. DDL/data chỉ trên DB GUID fixture, trong transaction được rollback; test trước tiên phải thấy schema gốc từ chối SQL sai, rồi bỏ riêng defense để chứng minh SQL sai sẽ lọt.
- CI backend chuyển từ gate T07 sang T12, kế thừa mọi matrix T05–T11. Thiếu HRM_T05_SQLSERVER phải NOT_RUN, executed0 và exit2; không skip hoặc fallback InMemory. Thay đổi workflow được kiểm tra local, chưa tuyên bố remote CI đã chạy.

Không triển khai UI, T13/T14 calculator hoặc bộ chaos engineering sản phẩm. Bất biến ở đây bảo vệ ghi mới; không tự sửa dữ liệu lịch sử đã sai và không xem đây là production UAT.
