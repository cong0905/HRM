# T13 — Salary history, calculator demo và snapshot

Ngày 13/11/2026. Chủ dự án yêu cầu Codex và Anti cùng làm T13, sau đó “tiếp tục đi”, tiếp nối quyền quyết định kỹ thuật/nghiệm thu đã giao. Không commit/push/deploy/mail thật hoặc dùng DB HRM_System.

## Phân công và chứng cứ

Anti thực viết duy nhất `backend/src/HRM.Domain/Payroll/PayrollCalculator.cs`, theo model cố định do Codex chuẩn bị. Phiên CLI `a5b5270c-3939-477a-b2f7-3bd9b5dae1e2`; lần đầu exit0,328.84s. Codex làm model/ports, persistence/migration, DI/API, source locks, fixtures và kiểm chứng độc lập. Anti không tự chạy tests và không tự chốt VERIFIED.

Kiểm tra precision của Codex tái hiện rawNet=-0.0000000000000000000000000002 dù net toán học=0 (base0.02,22 ngày đầy đủ, trừ đúng0.02). Anti sửa: cộng numeratorStandard=sum(base*creditedMinutes), numeratorOt=sum(base*approvedOT*factor), rồi chia cùng mẫu số tháng đúng một lần. Không epsilon, không float/double, không bỏ qua số âm thật, không làm tròn giữa chừng. Log/trạng thái trong docs/verification/t13-implementation; RED trong t13-review/calculator-precision.trx.

## Hợp đồng tính tiền

- Policy server cố định `POL-2026-V1`: OT weekday1.5/weekend2/holiday3; holiday được phân loại trước weekend. Đây là chính sách demo, không phải PIT/BHXH hay pháp luật tự động.
- Mẫu số = tổng phút chuẩn của lịch công ty cả tháng đã pin, không cố định26 ngày và không lấy riêng ngày làm việc của employee. SalaryBase ngày nào lấy interval inclusive ngày đó; cộng numerator phân đoạn và chia mẫu số chung sau cùng.
- Employee chọn theo giao khoảng hire..termination với tháng, không lọc IsActive hiện tại. Ngày ngoài thời gian làm việc hưởng0; thiếu hire date hoặc salary coverage cho ngày chuẩn/OT phải fail-closed.
- Day không có punch là0 phút không lương. Punch thiếu checkout hưởng0 phút và có blocking reason; không tự tính đủ ngày. Paid leave chỉ lấy Approved, full-day credit bằng phút chuẩn; công+phép dirty input được cap theo chuẩn thực tế (ví dụ420), không cộng đôi. OT chỉ lấy entry HR đã xác nhận, không suy từ về muộn, không kết hợp với full paid leave ngày chuẩn hoặc checkout thiếu.
- Gross=standard+OT+allowance+bonus; Net=gross-otherDeduction-demoInsurance-demoTax. Mỗi khoản trừ đúng một lần. KPI luôn tắt/neutral, dù score thiếu,0 hay100. Khoản thuế/bảo hiểm là số demo nhập tay có lý do; không tự suy tỷ lệ/luật.
- Decimal toàn bộ. Raw values giữ lại; tiền display round cuối cùng đến VND, AwayFromZero. Dòng RoundingAdjustment làm tổng signed lines bằng Net chính xác. Raw net<0 vẫn chặn dù display Net=0.

## Salary history và khoản nhập tay

- Salary append theo ngày tăng nghiêm ngặt; đóng mốc cuối về newFrom-1, không ghi đè MonthlyBase cũ. SQL unique(employee,effectiveFrom), unique một interval mở/employee, CHECK mức dương và end>=start. Khoảng không chồng được bảo vệ thêm bằng employee SQL lock và append-only ordering; không khẳng định SQL có exclusion constraint chặn mọi overlap raw SQL.
- Cấm effective date ảnh hưởng Published/Locked. Đóng một interval cũ ở kỳ tương lai hợp lệ không đổi snapshot kỳ đã chốt. Lịch sử đọc từ các decision đã lưu, không tính lại phiếu lương GET bằng lương hiện tại.
- Chỉ Payroll.HistoryManage được append salary; HR/SystemAdmin/Employee thuần không có mặc định. Read salary/snapshot/adjustment yêu cầu PayrollReadAll. Payroll.Calculate được thêm/void manual.
- Các kind chính xác: Allowance,Bonus,OtherDeduction,DemoInsurance,DemoTax. Amount dương, có lý do; không sửa/xóa số cũ. Void bằng RowVersion, lưu tombstone/lý do rồi thêm khoản thay thế. Không tác động kỳ Published/Locked.
- Tiền API nhận string, tối đa2 chữ số lẻ phù hợp cột lưu; >2 chữ số lẻ bị400 thay vì SQL tự làm tròn. Calculator vẫn giữ decimal chính xác cho tỷ lệ/phút và hỗ trợ biên làm tròn.
- Tất cả endpoint ghi mới của T13 yêu cầu UUIDv4 Idempotency-Key, scoped actor/operation/key, SHA256 fingerprint; replay trả response gốc và mismatch409. Receipt giữ lâu dài theo T12, append-only; không TTL/reuse.

## Snapshot và tích hợp workflow

- Capture tạo batch version tăng trong transaction, input/source/output JSON theo giá trị và hash SHA256. Source giữ calendar đã pin, profile employment/version, salary decisions, attendance/version, approved leave/version, confirmedOT và active manual/version. Không tham chiếu output sống tới salary hiện tại.
- Tất cả employee hợp lệ phải tính được rồi mới chốt batch/slips; lỗi thiếu data hoặc audit sau SQL flush rollback calendar snapshot, batch, slips và metadata. Output âm/incomplete được giữ để xử lý nhưng CanPublish=false.
- Draft Payslip.Amount/ SnapshotId trỏ kết quả latest batch. Capture là một lần tính thật, kể cả gọi trực tiếp endpoint snapshots: phải cập nhật CalculatedBy/CalculatedAtUtc/Calculated để người vừa capture không tự duyệt. Published payslips, snapshot/batch/receipt có EF guards sync/async; không tuyên bố có trigger bảo vệ khỏi mọi raw SQL admin.
- Global SQL SalaryInputs lock shared cho salary/manual, exclusive cho capture/approve/publish; month lock exclusive khi snapshot/ready check, dùng cùng resource với attendance/leave shared locks. Salary append vẫn có employee lock; version/index chống duplicate.
- Khi có salary source hoặc SnapshotRequired, endpoint calculate hiện tại tự tạo snapshot thật. Approve/publish kiểm âm/incomplete/missing snapshot và so hash input hiện tại; stale bị409, không công bố output cũ. Mẫu record metadata-only T07/T09 không có salary source vẫn giữ compatibility cho regression, không coi chúng là bằng chứng tính lương thật.
- T14 vẫn phải hoàn thiện lifecycle/run idempotency và workflow tính lại từ Approved khi input stale. Endpoint calculate cũ chưa có durable command receipt, mỗi lần gọi hợp lệ có thể tạo version mới; endpoint snapshot mới có key/replay. Không gán T14 VERIFIED từ các guard nền tảng này.

## Kiểm chuẩn

8 golden số tiền đọc từ payroll-golden.demo.json;2 fixture T00 còn lại là kiểm tra quỹ phép và hai thứ tự duyệt, không bịa số lương cho chúng. T00 case09 chỉ cho base12m và locked Net10.74m; test nhập rõ OtherDeduction1.26m bằng hiệu hai số, không gọi đó là insurance/tax do T00 quy định.

Đối chiếu độc lập Python Fraction (không gọi C# CalendarMath/calculator) lưu independent-math.json và CSV trong outputs. Tests gồm boundaries precision/zero month/missing/overlap, SQL uniqueness, race, role/IDOR, immutable/read-only và fault sau SQL flush. CI chuyển gate T13; remote CI chưa chạy.

Manual adjustments phải có employee employment giao kỳ; active adjustment bị mồ côi sau đổi employment sẽ chặn calculation thay vì âm thầm bỏ qua. Output Net vượt range cột Payslip bị từ chối trước INSERT để tránh SQL tự lỗi/round.
