# CHƯƠNG 3: THIẾT KẾ KIẾN TRÚC VÀ CƠ SỞ DỮ LIỆU

## 3.1. Thiết kế Kiến trúc Phần mềm (Software Architecture)

Hệ thống được thiết kế theo mô hình **Modular Monolith** kết hợp nguyên lý **Clean Architecture (Onion Architecture)**, giúp phân tách rành mạch trách nhiệm của từng tầng, loại bỏ sự phụ thuộc vòng và tạo điều kiện thuận lợi cho việc kiểm thử độc lập.

```
┌────────────────────────────────────────────────────────┐
│                   Tầng API (HRM.Api)                   │
│ - Controllers, Middlewares (CookieCsrf, SessionAuth)   │
│ - Background Workers (EmailOutboxWorker)               │
│ - Serialization (ContractJson), OpenAPI Contracts      │
└───────────────────────────┬────────────────────────────┘
                            │ Phụ thuộc vào
┌───────────────────────────▼────────────────────────────┐
│         Tầng Hạ tầng (HRM.Infrastructure)              │
│ - Entity Framework Core 10 (HrmDbContext, Migrations)  │
│ - Repositories, Identity Stores, Unit of Work          │
│ - Notifications (SmtpEmailSender, EmailOutboxService)  │
└───────────────────────────┬────────────────────────────┘
                            │ Triển khai các Interface của
┌───────────────────────────▼────────────────────────────┐
│          Tầng Ứng dụng (HRM.Application)               │
│ - Interfaces, DTOs, Queries, Commands, Business Rules  │
│ - Hợp đồng giao tiếp (Contracts, Security Context)     │
└───────────────────────────┬────────────────────────────┘
                            │ Phụ thuộc vào
┌───────────────────────────▼────────────────────────────┐
│              Tầng Miền cốt lõi (HRM.Domain)            │
│ - Entities, Aggregate Roots, Domain Exceptions         │
│ - Invariants, Enums, Value Objects                     │
│ - TUYỆT ĐỐI KHÔNG PHỤ THUỘC VÀO TẦNG NGOÀI            │
└────────────────────────────────────────────────────────┘
```

### Trách nhiệm chi tiết của các tầng:
1. **`HRM.Domain`**: Chứa toàn bộ các thực thể cốt lõi (`EmployeeProfile`, `AttendanceSummary`, `LeaveRecord`, `PayrollPeriodRecord`, `EmailOutboxMessage`), định nghĩa các quy tắc bất biến và ngoại lệ nghiệp vụ (`DomainRuleException`). Tầng này hoàn toàn độc lập, không tham chiếu đến bất kỳ framework hay cơ sở dữ liệu nào.
2. **`HRM.Application`**: Định nghĩa các giao diện dịch vụ (`ILeaveService`, `IPayrollCalculationService`, `IAttendanceService`), các đối tượng truyền dữ liệu (DTOs) và luồng xử lý ca sử dụng (Use Cases).
3. **`HRM.Infrastructure`**: Hiện thực hóa việc lưu trữ dữ liệu thông qua Entity Framework Core 10, quản lý giao dịch cơ sở dữ liệu (`UnitOfWork`), tích hợp gửi email SMTP/DevSink và quản lý các adapter định danh.
4. **`HRM.Api`**: Cung cấp các RESTful API endpoints chuẩn `/api/v1/*`, kiểm soát tính hợp lệ của request payload, cấu hình Swagger/OpenAPI, và quản lý các tiến trình chạy nền (`EmailOutboxWorker`).

---

## 3.2. Kiến trúc Bảo mật và Xác thực Phiên Lai (Hybrid Authentication)

Hệ thống triển khai cơ chế xác thực phiên làm việc lai (**Hybrid Authentication Architecture**) phục vụ tối ưu cho cả hai môi trường Web và Mobile thông qua `SessionAuthenticationHandler`:

```
                       ┌────────────────────────┐
                       │  Client Request        │
                       └───────────┬────────────┘
                                   │
                    Có Cookie __Host-Hrm.Session?
                     ├─── CÓ ──► Web Path: Giải mã Session ID
                     │           Kiểm tra CSRF Header X-XSRF-TOKEN
                     │
                     └─── KHÔNG ─► Có Bearer Header?
                                   ├─── CÓ ──► Mobile Path: Giải mã Opaque Ticket
                                   │           Lấy Session ID từ Claim
                                   └─── KHÔNG ─► Anonymous / 401 Unauthorized
                                   │
                                   ▼
                       ┌────────────────────────┐
                       │  Kiểm tra AccountSession│
                       │  - Session còn hạn?    │
                       │  - Revoked == false?   │
                       │  - SecurityStamp khớp? │
                       │  - User.IsActive == true
                       │  - Chưa offboard?      │
                       └───────────┬────────────┘
                                   │
                        ┌──────────┴──────────┐
                        │                     │
                     HỢP LỆ               KHÔNG HỢP LỆ
                        ▼                     ▼
                  Tiếp tục API            401 Unauthorized
```

### 1. Kênh Web (Cookie & CSRF):
- Sử dụng Cookie máy chủ: `__Host-Hrm.Session` có gắn cờ `HttpOnly`, `Secure`, `SameSite=Strict` và `Path=/`. JavaScript phía client hoàn toàn không thể đọc giá trị cookie này, triệt tiêu nguy cơ tấn công XSS đánh cắp phiên.
- Chống tấn công giả mạo yêu cầu (CSRF): Tích hợp middleware `CookieCsrfMiddleware`, yêu cầu tất cả các request ghi (POST/PUT/PATCH/DELETE) phải gửi kèm header `X-XSRF-TOKEN` được cấp phát từ endpoint `/api/v1/auth/csrf`.

### 2. Kênh Mobile (Bearer & Refresh Token Rotation):
- Cấp cặp Token định dạng Opaque Framework Protected Ticket (không sử dụng JWT không trạng thái nhằm tránh rủi ro không thể thu hồi token trước hạn):
  - **Access Token**: Hạn dùng ngắn (15 phút), truyền qua header `Authorization: Bearer <token>`.
  - **Refresh Token**: Hạn dùng 7 ngày, lưu trữ trong Secure Storage của thiết bị di động.
- Khi Refresh, hệ thống áp dụng cơ chế **Quay vòng token nguyên tử (Atomic Refresh Token Rotation)** bằng truy vấn CAS (`ExecuteUpdateAsync`). Nếu hai request refresh gửi đồng thời cùng một token, chỉ 1 request thành công, request còn lại bị từ chối 401 và ngăn chặn hành vi Replay Attack.

### 3. Thu hồi quyền tức thời (Instant Revocation Engine):
- Mỗi phiên làm việc được định danh bằng một bản ghi trong bảng `[identity].[AccountSessions]`.
- Mọi request gửi lên đều được kiểm tra đối soát với `SecurityStamp` của `HrmUser`. Khi người dùng đổi mật khẩu, đăng xuất tất cả, hoặc nhân viên bị offboarding, `SecurityStamp` được cập nhật mới và toàn bộ cờ `Revoked` của các session cũ được bật sang `true`. Mọi token/cookie đã phát hành trước đó lập tức bị vô hiệu hóa trong chưa đầy 1 giây.

---

## 3.3. Thiết kế Cơ sở Dữ liệu Quan hệ (Relational Database Design)

Cơ sở dữ liệu được tổ chức chặt chẽ trên Microsoft SQL Server 2022, phân tách thành 6 schema logic:
1. `personnel`: Quản lý thông tin tổ chức, hồ sơ nhân viên, chức vụ, phòng ban và phân công HR.
2. `identity`: Quản lý tài khoản `AspNetUsers`, phiên `AccountSessions` và phân quyền vai trò.
3. `attendance`: Lưu trữ sự kiện chấm công gốc (`AttendanceEvents`), bảng tổng hợp ngày (`AttendanceSummaries`), duyệt OT và nhật ký sửa công.
4. `calendar`: Lịch làm việc công ty theo phiên bản và snapshot lịch theo kỳ lương.
5. `leave`: Đơn nghỉ phép (`LeaveRecords`), quỹ phép năm (`LeaveBalances`), sổ cái nghỉ phép (`LeaveLedgerEntries`) và biên lai idempotency.
6. `payroll`: Lịch sử lương (`SalaryHistory`), kỳ lương (`PayrollPeriods`), đợt tính lương (`CalculationBatches`), snapshot chi tiết từng nhân viên (`CalculationSnapshots`) và phiếu lương công bố (`Payslips`).
7. `notifications`: Hàng đợi gửi email giao dịch (`EmailOutboxMessages`).
8. `audit`: Nhật ký kiểm toán hệ thống (`AuditEvents`).

### Cơ chế Kiểm soát Đồng thời (Concurrency Control):
Các bảng thường xuyên phát sinh thao tác ghi đồng thời đều được trang bị cột kiểm soát khóa lạc quan `RowVersion` (kiểu dữ liệu SQL Server `rowversion / timestamp` 8-byte):
- `EmployeeProfiles.RowVersion`
- `LeaveBalances.RowVersion` & `LeaveRecords.RowVersion`
- `AttendanceSummaries.RowVersion`
- `PayrollPeriods.RowVersion` & `SalaryHistory.RowVersion`

Khi xảy ra xung đột cập nhật đồng thời (hai tiến trình cùng đọc một phiên bản dữ liệu và cố gắng ghi đè), Entity Framework Core sẽ phát hiện chênh lệch `RowVersion` và ném ra ngoại lệ `DbUpdateConcurrencyException`, giúp hệ thống chủ động rollback giao dịch và trả về mã lỗi HTTP `409 Conflict` kèm thông báo rõ ràng cho người dùng thay vì ghi đè mất mát dữ liệu (Lost Update).
