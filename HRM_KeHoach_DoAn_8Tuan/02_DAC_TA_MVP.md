# Đặc tả MVP 8 tuần — hợp đồng chung cho AI

Trạng thái: đề xuất kỹ thuật để hiện thực; các chính sách tiền/công/phép phải được chủ dự án duyệt ở T00. Đây không phải mô tả hệ thống đã tồn tại.

## Phạm vi và quy tắc không được tự suy diễn

- Một công ty, một múi giờ Asia/Ho_Chi_Minh; lưu timestamp UTC, lịch công và kỳ lương theo ngày địa phương.
- Một lịch làm việc công ty, ca ngày; không hỗ trợ ca qua đêm trong MVP và phải trả lỗi rõ khi nhập ca qua ngày. Một bản tổng hợp công/nhân viên/ngày; event gốc giữ để truy vết.
- Dữ liệu demo: tuần thứ Hai–thứ Sáu, 08:30–17:30, nghỉ 12:00–13:00. Đây chỉ là fixture đề xuất, không phải quy tắc lao động áp dụng cho mọi đơn vị.
- Quỹ phép demo: 12 ngày/năm; không tự suy diễn phép theo thâm niên, thử việc, chuyển năm. Đơn qua năm tạm bị từ chối có lý do. Chỉ ngày làm việc mới trừ quỹ; nửa ngày hoãn.
- Đơn Pending → Approved/Rejected/Cancelled. Chỉ chủ đơn hủy Pending; không cho tự duyệt; xử lý Approved cần nghiệp vụ reversal sau MVP, không sửa trực tiếp.
- Tạo đơn kiểm tra giao nhau; duyệt phải đọc lại quỹ và trừ trong transaction có concurrency. Retry cùng idempotency key không trừ hai lần.
- Chấm công dùng giờ server, ID người từ phiên. Mobile chưa có offline queue ghi công. Lỗi mạng hiển thị chưa xác nhận và truy vấn trạng thái; không tự tạo giờ từ client.
- Đi làm và phép hưởng lương không được tính hai lần cho cùng một ngày. Xin sửa công cần lý do; chỉ HR có quyền; mọi sửa có before/after audit. Không sửa công kỳ lương khóa nếu chưa có quy trình điều chỉnh.
- Dữ liệu lương: SalaryHistory(EffectiveFrom, EffectiveTo, Amount), PayrollPeriod, PayrollRun, PayrollItem, PolicyVersion. Tiền dùng decimal và quy tắc làm tròn thống nhất phía server.
- Chính sách demo: lương ngày = mức áp dụng/ngày công chuẩn theo lịch tháng; thay đổi lương giữa tháng được phân đoạn theo ngày hiệu lực. Công thực nhận theo giờ công hợp lệ + giờ phép hưởng lương, giới hạn mỗi ngày theo lịch. OT tách khỏi công chuẩn và cần HR xác nhận. Hệ số OT là tham số demo được duyệt, không tự nhận tuân thủ pháp luật.
- Chưa đánh giá hiệu suất → hệ số trung tính 0; MVP mặc định tắt thưởng/phạt tự động theo hiệu suất. Phụ cấp/khấu trừ nhập theo loại, số tiền, lý do và người ghi; thuế/bảo hiểm chưa tự tính theo luật. Không cho số tiền âm nếu loại không cho phép. Lương thực nhận âm bị chặn chốt để HR xử lý.
- Lương Draft → Calculated → Approved → Locked/Published. Sửa đầu vào làm bản nháp cần tính lại; kỳ khóa bất biến. Xem dữ liệu không ghi. Chạy lại nháp có version/idempotency; thất bại không xóa bản cũ. Công bố chỉ sau duyệt, ghi người/thời gian; chỉ bản công bố xuất hiện trên mobile.
- Tài khoản nghỉ việc/bị khóa không tiếp tục gọi API bằng phiên đã cấp. Profile nhân viên chỉ đọc ở MVP; sửa hồ sơ do HR, DTO update riêng bảo toàn trường không sửa.

## Ma trận quyền bắt buộc

| Thao tác | Employee | HR | Payroll | SystemAdmin |
|---|---|---|---|---|
| Hồ sơ cá nhân | Của mình | Theo phạm vi quản lý | Thông tin cần tính lương | Không mặc định xem PII |
| Hồ sơ nhân viên | Không | Tạo/sửa/nghỉ việc | Đọc phần được cấp | Gán quyền |
| Chấm công | Của mình | Tra cứu/điều chỉnh | Đọc công tổng hợp | Cấu hình kỹ thuật |
| Đơn nghỉ | Tạo/hủy/xem của mình | Duyệt/từ chối, không tự duyệt | Đọc dữ liệu tính lương | Không mặc định duyệt |
| Lương | Phiếu đã công bố của mình | Chỉ khi được cấp thêm | Tính/duyệt/công bố theo permission | Không mặc định xem lương |
| Tài khoản | Đổi mật khẩu bản thân | Yêu cầu offboarding | Không | Quản lý, không đọc hash/token |

Người dùng có thể được gán nhiều vai trò; quyền tính/duyệt/công bố tách thành permission. Mỗi endpoint có test 401, 403 và đổi ID sang bản ghi người khác. Không chỉ ẩn nút.

## Màn hình nghiệm thu

Web: login/reset; dashboard; danh sách/chi tiết/sửa nhân viên; phòng ban; lịch công ty; tra cứu/sửa công; đơn nghỉ chờ duyệt; kỳ lương/tính nháp/chi tiết/duyệt/công bố/CSV; tài khoản; audit tra cứu cho người được cấp; trang cá nhân.
Mobile Android: login/reset; tổng quan cá nhân; hồ sơ; hôm nay/lịch sử công; tạo/lịch sử/chi tiết đơn nghỉ; danh sách/chi tiết phiếu lương; đổi mật khẩu/logout. Có loading, empty, lỗi mạng, hết phiên và chống bấm lặp. Không coi màn hình có mock là hoàn thành.

## API contract đề xuất

Version /api/v1; OpenAPI là hợp đồng sinh client TypeScript. Auth theo Identity adapter được chốt ở T06, không ép bearer mặc định thành JWT. API nghiệp vụ tối thiểu:

| Nhóm | Route đề xuất | Ràng buộc |
|---|---|---|
| Cá nhân | GET /me, /me/attendance, /me/leave-requests, /me/payslips | Server lấy ID từ phiên |
| Nhân viên | GET/POST /employees; GET/PATCH /employees/{id}; POST /employees/{id}/offboard | Filter/page/whitelist field |
| Phòng ban | GET/POST /departments; PATCH /departments/{id} | Không xóa vật lý lịch sử |
| Lịch | GET/PUT /work-calendar | Version và ngày áp dụng |
| Công | POST /attendance/check-in, /check-out; GET /attendance; POST /attendance/{id}/corrections | Idempotency-Key; giờ server; lý do sửa |
| Phép | POST /leave-requests; POST /leave-requests/{id}/approve,reject,cancel; GET /leave-balances | Kiểm quyền + rowversion + transaction |
| Lương | POST /payroll-periods; POST /payroll-periods/{id}/calculate,approve,publish; GET /payroll-periods/{id}/items | Snapshot/version; không sửa kỳ khóa |
| Xuất | GET /payroll-periods/{id}/export | Quyền riêng; audit; chống CSV formula |
| Quản trị | GET /accounts; POST /accounts/{id}/disable; PUT /accounts/{id}/roles; GET /audit-events | Không tự nâng quyền; audit đọc có permission và phân trang |
| Lương hiệu lực | GET/POST /employees/{id}/salary-history; POST /payroll-periods/{id}/adjustments | Quyền Payroll riêng; không ghi đè kỳ khóa |
| OT | POST /overtime-entries; POST /overtime-entries/{id}/approve | HR xác nhận phút được tính, không suy từ giờ checkout |

Các ký hiệu approve,reject,cancel là ba endpoint riêng, không phải một URL chứa dấu phẩy. Danh sách page mặc định 20, tối đa 100; sort/filter whitelist. Lỗi dùng ProblemDetails + errorCode + traceId, không lộ stack/SQL. 409 cho xung đột trạng thái/version; 422 cho vi phạm quy tắc; 401/403 theo danh tính/quyền. Timestamp ISO8601 UTC; ngày nghiệp vụ YYYY-MM-DD; số tiền truyền chuỗi decimal hoặc đơn vị tiền nhỏ nhất theo một ADR duy nhất, tránh làm tròn float trên client.

## Mô hình dữ liệu cần thiết

Giữ các khái niệm Employee, Department, Position từ nguồn cũ; không nhất thiết giữ nguyên mọi tên bảng. IdentityUser liên kết Employee bằng FK tường minh; username chuẩn hóa unique. SalaryHistory không chồng ngày hiệu lực. AttendanceEvent giữ giờ server, source, requestId; AttendanceDay tổng hợp và rowversion. LeaveRequest + LeaveBalance hoặc LeaveLedger với transaction; unique request id. PayrollPeriod/Run/Item lưu policy version, salary snapshot, attendance/leave inputs và các dòng tiền. AuditEvent, OutboxMessage có retention và index phù hợp. Migrations kiểm tra duplicate trước khi thêm unique; không tự xóa duplicate.

## Definition of Done dùng chung

1. Chỉ sửa phạm vi task; nếu cần đổi contract/module khác phải nêu tác động và cập nhật task liên quan trước.
2. Build và test phù hợp đạt; lỗi tiền/quyền có test tái hiện trước sửa, test âm và biên. Integration chạy SQL Server test riêng.
3. Không hard-code mật khẩu/API key; không log token/PII; query có giới hạn và scope người dùng.
4. Migrations có kế hoạch dữ liệu cũ, kiểm thử DB trống/nâng cấp và rollback hoặc forward recovery rõ.
5. OpenAPI và client đồng bộ; không sửa file client được sinh tay; cập nhật hướng dẫn nếu lệnh chạy thay đổi.
6. Kết quả có bằng chứng thật: lệnh, pass/fail, ảnh UI nếu task UI, môi trường/commit. Không có bằng chứng → chưa Verified.
7. Không gọi API production, sửa DB thật hoặc phát hành ứng dụng nếu task chưa được cấp phạm vi đó.
8. Nếu graph tồn tại phải query trước; sau sửa code cập nhật Graphify theo AGENTS.md; không coi graph là bằng chứng runtime.
