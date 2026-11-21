# T15 — Phiếu lương snapshot, đối soát và CSV

Quyết định kỹ thuật ngày 13/11/2026 trong phạm vi T15 sau T14 VERIFIED; tiếp nối ủy quyền quyết định/triển khai của chủ dự án. Không đổi công thức demo hoặc quyền hiện có.

## Nguồn dữ liệu và quyền

- Detail quản trị cần payroll.read_all; route riêng /me/payslips/{id} cần payroll.read_own và server lọc profile từ current user. Owner chỉ đọc slip Published thuộc kỳ Published/Locked. ID khác chủ hoặc chưa công bố trả404, không lộ sự tồn tại. Payroll có thể xem preview Calculated/Approved qua route quản trị.
- Detail dùng đúng ActiveBatch của draft hoặc PublishedBatch của kỳ đóng, kiểm pointer/period/snapshot/employee/net. Trả policy, ngày công/phép/OT, lịch và các signed money lines/rounding đã lưu; không trả SourceJson, salary-history reasons, CitizenId, địa chỉ, ngân hàng hoặc thông tin nhân viên khác. Không tính lại theo salary hiện tại.
- Summary cần payroll.read_all, SQL COUNT/SUM trên toàn kỳ (không tổng của một page), kiểm một-một slips với snapshot run và amount. Mismatch/missing snapshot trả409. Difference=0 khi cùng snapshot và rounding T13. Metadata-only legacy không có run không giả làm kỳ lương đối soát được.
- Detail/summary dùng transaction và shared payroll-source applock, cùng lock order với workflow T14; không SaveChanges/DML. Khóa điều phối các query với calculate/publish để không trả hỗn hợp hai version; đây chỉ là metadata khóa SQL, không ghi dữ liệu nghiệp vụ.

## Export

- Permission payroll.export được kiểm ở endpoint và service; role HR/SystemAdmin không có mặc định. Có quyền xem cá nhân không đồng nghĩa có quyền tải toàn kỳ. Không nới role/claim trong sản phẩm.
- Chỉ xuất kỳ Published/Locked và đúng PublishedBatch; đối soát trước khi tạo file, so cả JSON output.Net và signed lines với persisted Net. CSV cũ đã tái hiện xuất amount sai snapshot (RED200 thay vì409); T15 fail-closed.
- maxRows mặc định500, hợp lệ1..1000. Nếu số dòng kỳ vượt maxRows,409 và không có partial file/audit thành công. Chỉ materialize tối đa maxRows dòng; summary dùng aggregate SQL. Giới hạn phục vụ MVP; export lớn/chia lô là nghiệp vụ tương lai, không âm thầm cắt mất người.
- UTF-8 BOM, charset utf-8, CRLF, quote text và double-quote escaping. Text không tin cậy bắt đầu =,+,-,@ sau whitespace/control/Unicode format được prefix apostrophe; tab/CR/LF đầu cũng prefix. Số lấy từ decimal tin cậy, format InvariantCulture, không nhận expression từ người dùng. Tên chứa dấu phẩy/quote/newline vẫn một cell.
- Các cột: PayslipId, EmployeeCode, FullName, PeriodCode, RunVersion, StandardPay, OvertimePay, Allowance, Bonus, OtherDeduction, DemoInsurance, DemoTax, RoundingAdjustment, Gross, Net, Status, SnapshotId. Khoản trừ mang dấu âm, tổng signed lines=Net. EmployeeCode/FullName là nhãn hiện tại, tiền/version luôn từ snapshot lịch sử; không tuyên bố nhãn cá nhân được snapshot từ trước.
- Tên file chỉ GUID kỳ và version, không ghép chuỗi tên/period không tin cậy vào header. Không CCCD, bank, địa chỉ, email hay toàn bộ payroll JSON mặc định.
- Audit payroll.exported lưu actor, period, batch/version, row count, format, trace; không lưu file, danh sách tên hoặc số tiền. Audit commit thành công trước khi trả FileContentResult. Lỗi sau audit SQL flush rollback và trả ProblemDetails, không trả file. Audit chứng minh server đã cấp response/file sau kiểm quyền, không chứng minh browser đã nhận/tải hết byte.
- GET export có ghi audit có chủ đích; GET detail/summary là projection chỉ đọc. Tải lại hợp lệ ghi audit mới, không giả làm command idempotency.

## Bằng chứng và giới hạn

SQL Server GUID fixture, không HRM_System, không InMemory. Ma trận gồm nhiều employee, rounding, owner/non-owner/publish state, multi-role, UTF-8/multiline, formula cases, row bounds, audit fault và publish/export race. Unit CSV gồm control/Unicode/quote cases. Gate giữ matrices T05–T14, thêm T15; thiếu SQL phải NOT_RUN/exit2. Không migration mới.

Không PDF, ký số, email thật, ngân hàng, UI, thuế/bảo hiểm tự động, remote CI hoặc human UAT trong T15. EF guards không là SQL trigger chống mọi raw SQL của admin; fault injection SQL chỉ nằm trong test.
