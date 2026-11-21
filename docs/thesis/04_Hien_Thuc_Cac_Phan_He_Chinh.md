# CHƯƠNG 4: HIỆN THỰC CÁC PHÂN HỆ CỐT LÕI

## 4.1. Phân hệ Định danh và Quản lý Nhân sự (Identity & Personnel)

### 1. Cơ chế sinh mã nhân viên tuần tự an toàn (`GenerateEmployeeCodeAsync`):
Mã nhân viên có định dạng chuẩn `NV{YYYY}{XXXX}` (ví dụ: `NV20260001`). Để đảm bảo tính duy nhất và không bị trùng mã khi có nhiều HR cùng thêm nhân viên tại một thời điểm, hệ thống triển khai bộ đếm tại bảng `[personnel].[EmployeeCodeCounters]` kết hợp câu lệnh SQL với gợi ý khóa `UPDLOCK, HOLDLOCK`:
```sql
UPDATE [personnel].[EmployeeCodeCounters] WITH (UPDLOCK, HOLDLOCK)
SET [LastValue] = [LastValue] + 1
OUTPUT INSERTED.[LastValue] INTO @issued
WHERE [Year] = @year AND [LastValue] < 9999;
```
Cơ chế này khóa phạm vi hàng (Key-Range Lock) trong suốt thời gian giao dịch diễn ra, ngăn chặn tình trạng hai request đồng thời nhận cùng một giá trị số thứ tự. Nếu số lượng nhân viên trong năm đạt ngưỡng 9999, hệ thống ném ra mã lỗi `employees.code_exhausted` có kiểm soát.

### 2. Phạm vi quản lý hồ sơ nhân sự (HR Scope):
Hệ thống lưu trữ quan hệ phân công giữa tài khoản HR và nhân viên phụ trách tại bảng `[personnel].[HrEmployeeAssignments]`. Khi HR thực hiện truy vấn hoặc cập nhật hồ sơ, tầng nghiệp vụ lọc chặt chẽ:
```csharp
var inScope = await db.HrEmployeeAssignments
    .AnyAsync(a => a.HrUserId == currentUserId && a.EmployeeId == employeeId);
if (!inScope && !isSystemAdmin) return Forbid();
```
Ngăn chặn tuyệt đối việc HR của đơn vị này can thiệp vào hồ sơ nhân viên của đơn vị khác.

### 3. Quy trình Thôi việc (Offboarding) nguyên tử:
Khi nhân viên nghỉ việc, phương thức `OffboardEmployeeAsync` thực thi trong một `UnitOfWork` duy nhất:
- Cập nhật trạng thái nhân viên sang `DaNghiViec` và ghi nhận `TerminationDate`.
- Vô hiệu hóa tài khoản liên kết (`HrmUser.IsActive = false`).
- Cập nhật `SecurityStamp` mới và chuyển toàn bộ `AccountSessions` của người dùng sang `Revoked = true`.
- Ghi nhận nhật ký kiểm toán `employees.offboarded`.

---

## 4.2. Phân hệ Chấm công và Lịch làm việc (Attendance & Calendar)

### 1. Bảo vệ Giờ chuẩn Máy chủ (`BusinessClock`) và Chống bấm lặp (`Idempotency-Key`):
- Để ngăn ngừa gian lận thời gian từ thiết bị người dùng, API check-in / check-out hoàn toàn bỏ qua timestamp từ client payload và chỉ sử dụng `TimeProvider.GetUtcNow()`.
- Mỗi yêu cầu check-in/out bắt buộc phải gửi kèm header `Idempotency-Key` (UUIDv4). Hệ thống lưu vết tại bảng `[attendance].[AttendanceIdempotencyRecords]` với chỉ mục duy nhất `(UserId, Operation, Key)`. Nếu nhân viên bấm nút nhiều lần do mạng lag hoặc app gửi lại request sau khi bị timeout, máy chủ sẽ phát hiện key đã tồn tại và trả về nguyên trạng kết quả của lần xử lý trước đó mà không tạo thêm bản ghi công mới.

### 2. Thuật toán Tổng hợp Công và Bất biến Công - Phép (`WorkLeaveInvariant`):
- Hệ thống tính toán thời gian làm việc hợp lệ (`EligibleMinutes`), số phút đi muộn (`LateMinutes`) và số phút về sớm (`EarlyLeaveMinutes`) dựa trên giao điểm giữa khoảng thời gian check-in/check-out thực tế và ca chuẩn trong ngày (trừ 60 phút nghỉ trưa).
- Quy tắc bất biến công - phép: Tổng thời gian làm việc thực tế và thời gian nghỉ phép hưởng lương trong một ngày không được vượt quá thời lượng ca chuẩn (480 phút):
  $$\text{EligibleMinutes} + (\text{PaidLeaveDays} \times 480) \le 480$$
Quy tắc này ngăn chặn triệt để tình trạng nhân viên vừa đi làm đủ ngày vừa xin nghỉ phép hưởng lương trọn vẹn trong cùng một ngày.

---

## 4.3. Phân hệ Nghỉ phép và Giao dịch Trừ Quỹ An toàn (Leave Management)

### 1. Giải quyết Xung đột Phép âm khi Duyệt Đồng thời (Race Condition Solved):
Khi phê duyệt đơn nghỉ phép, rủi ro lớn nhất là hai người quản lý cùng lúc duyệt hai đơn khác nhau của cùng một nhân viên, khiến tổng số ngày nghỉ vượt quá số dư phép năm. Hệ thống giải quyết triệt để vấn đề này bằng cách kết hợp:
1. **Khóa bản ghi Quỹ phép (`UPDLOCK, ROWLOCK`)** trong giao dịch cơ sở dữ liệu khi đọc số dư:
   ```csharp
   var balance = await db.LeaveBalances
       .FromSqlInterpolated($"SELECT * FROM [personnel].[LeaveBalances] WITH (UPDLOCK, ROWLOCK) WHERE [EmployeeId] = {empId} AND [Year] = {year}")
       .SingleAsync();
   ```
2. **Khóa lạc quan qua `RowVersion`**: Cả `LeaveRecord` và `LeaveBalance` đều có trường `RowVersion`. Nếu hai giao dịch cùng vượt qua vòng kiểm tra, giao dịch nào commit trước sẽ làm thay đổi `RowVersion`, khiến giao dịch sau lập tức bị từ chối với lỗi xung đột `409 Conflict`.
3. **Ràng buộc toàn vẹn SQL Check Constraint**:
   ```sql
   ALTER TABLE [personnel].[LeaveBalances] ADD CONSTRAINT [CK_LeaveBalances_NoOverdraft]
   CHECK ([AllocatedDays] >= [UsedDays]);
   ```
   Ngay cả khi có lỗi logic phần mềm, cơ sở dữ liệu sẽ chủ động chặn đứng câu lệnh ghi nếu `UsedDays > AllocatedDays`, bảo vệ quỹ phép không bao giờ bị âm.

### 2. Sổ cái biến động phép (Leave Ledger):
Mỗi lần duyệt đơn (`Approve`) hoặc hủy đơn (`Cancel`), hệ thống tự động chèn một bản ghi vào bảng `[personnel].[LeaveLedgerEntries]` ghi lại số dư trước (`BalanceBefore`), số ngày thay đổi (`Days`), số dư sau (`BalanceAfter`), mã đơn và danh tính người thực hiện, đảm bảo khả năng kiểm toán minh bạch 100%.

---

## 4.4. Phân hệ Tiền lương và Snapshot Bất biến (Payroll)

### 1. Máy trạng thái Vòng đời Kỳ lương (Payroll Period State Machine):
Kỳ lương được quản lý theo quy trình chuyển đổi trạng thái nghiêm ngặt:
$$\text{Draft} \longrightarrow \text{Calculated} \longrightarrow \text{Approved} \longrightarrow \text{Published} \longrightarrow \text{Locked}$$
- **Draft**: Kỳ lương mới tạo, cho phép cấu hình và gắn các khoản điều chỉnh thủ công (`ManualAdjustments`).
- **Calculated**: Đã thực thi tính toán lương cho toàn bộ nhân viên, tạo ra một đợt tính (`CalculationBatch`) kèm mã băm đầu vào `InputHash`.
- **Approved**: Cấp quản lý kế toán phê duyệt đợt tính toán.
- **Published**: Công bố bảng lương. Phiếu lương (`Payslip`) của nhân viên chuyển sang trạng thái sẵn sàng để nhân viên tra cứu trên Mobile App.
- **Locked**: Kỳ lương được niêm phong vĩnh viễn sau khi hoàn tất chi trả tiền.

### 2. Tính Bất biến Tuyệt đối của Dữ liệu Lương (`EnsureCalendarImmutability`):
Tại tầng Infrastructure (`HrmDbContext`), phương thức kiểm soát bất biến được kích hoạt tự động trước mỗi lệnh `SaveChanges`:
```csharp
private void EnsureCalendarImmutability()
{
    foreach (var entry in ChangeTracker.Entries<PayrollPeriodRecord>())
    {
        var original = entry.OriginalValues.GetValue<string>(nameof(PayrollPeriodRecord.Status));
        if (original == "Locked" || (original == "Published" && entry.Entity.Status != "Locked"))
            throw new DomainRuleException("payroll.period_immutable", 
                "Published data may only be sealed Locked; Locked periods are immutable.");
    }
    if (ChangeTracker.Entries<PayrollCalculationSnapshot>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
        throw new DomainRuleException("payroll.snapshot_immutable", "Payroll snapshots are append-only.");
}
```
Nhờ cơ chế này, bất kỳ nỗ lực sửa đổi hay xóa bỏ bản ghi kỳ lương đã công bố, snapshot tiền lương, hoặc lịch sử mức lương cũ đều bị chặn đứng và ném ngoại lệ ngay tại tầng hạ tầng dữ liệu. Khi mức lương của nhân viên thay đổi trong tương lai, hệ thống thêm một bản ghi mới vào `SalaryHistory` mà không làm thay đổi các bảng lương cũ đã chốt.

### 3. Phân đoạn Lương khi Thay đổi Giữa tháng:
Nếu nhân viên được tăng lương vào giữa tháng (ví dụ ngày 16 của tháng có 22 ngày công chuẩn), thuật toán tính lương trong `PayrollCalculator.cs` tự động phân tách số ngày công theo hai phân đoạn thời gian trước và sau ngày hiệu lực, tính toán chính xác mức lương tương ứng theo tỷ lệ ngày công thực tế mà không làm tròn số float.

---

## 4.5. Phân hệ Hàng đợi Gửi Email (Transactional Outbox)

Nhằm đảm bảo **tính toàn vẹn giao dịch** và không để sự cố mạng SMTP làm gián đoạn luồng nghiệp vụ của người dùng:
1. Khi có yêu cầu đặt lại mật khẩu hoặc kích hoạt tài khoản, `EmailOutboxService` tạo một bản ghi trong bảng `[notifications].[EmailOutboxMessages]` với trạng thái `Queued`, thời hạn `ExpiresAtUtc` (15 phút) và lưu trong cùng một giao dịch cơ sở dữ liệu với nghiệp vụ chính.
2. Tiến trình chạy nền `EmailOutboxWorker` (kế thừa `BackgroundService`) định kỳ quét các bản ghi `Queued` có `NextAttemptAtUtc <= now`:
   - Nếu phát hiện `now > ExpiresAtUtc`, worker chuyển trạng thái sang `Failed` với lý do *"Message expired before delivery"* và **từ chối gửi ra ngoài**, tránh gửi token đã chết đến hộp thư nhân viên.
   - Khi gửi thư qua `IEmailSender`: nếu thành công chuyển sang `Sent` và lưu `SentAtUtc`; nếu gặp lỗi tạm thời (Timeout, mạng ngắt quãng, lỗi SMTP 421) thì tính toán thời gian retry theo **Exponential Backoff** ($\text{initial} \times 2^{\text{attempt}-1}$); nếu gặp lỗi vĩnh viễn (5xx) hoặc vượt quá `MaxAttempts = 5` thì chuyển sang `Failed`.
3. Khử trùng nhật ký (`EmailLogMasker`): Tuyệt đối không log nội dung body email; tự động thay thế mọi token, mật khẩu, chuỗi truy vấn nhạy cảm bằng `[REDACTED]` trước khi ghi ra console hay file log.

---

## 4.6. Ứng dụng Giao diện Web SPA và Mobile App

### 1. Web SPA (React 18 + TypeScript + Vite + Ant Design):
- Được thiết kế theo chuẩn Single Page Application giao tiếp với API backend qua client TypeScript tự động sinh từ OpenAPI schema (`hrm-api-contracts`).
- Bảo mật: Hoạt động cùng Origin với API thông qua Reverse Proxy; quản lý xác thực bằng Cookie `HttpOnly, Secure, SameSite=Strict`; tự động đính kèm CSRF token trên mọi thao tác ghi dữ liệu.
- Đầy đủ giao diện quản trị: Dashboard tổng quan, Danh sách & Sửa hồ sơ nhân sự, Phân công HR, Lịch công ty, Quản lý công & Duyệt OT, Hàng đợi duyệt nghỉ phép, Tính toán & Duyệt kỳ lương, và Bảng kiểm toán Audit.

### 2. Mobile App (React Native / Android):
- Ứng dụng di động hướng tới trải nghiệm nhân viên tự phục vụ (Employee Self-Service):
  - Màn hình Tổng quan cá nhân và hồ sơ chi tiết.
  - Chấm công nhanh Online với định danh giao dịch `Idempotency-Key`, hiển thị rõ ràng thông báo lỗi khi mất kết nối mạng (không báo thành công giả).
  - Quản lý nghỉ phép: Xem số dư phép còn lại, nộp đơn xin nghỉ, hủy đơn chờ duyệt.
  - Bảng lương: Tra cứu danh sách các phiếu lương đã công bố và xem giải thích chi tiết các khoản thu nhập/khấu trừ.
- **Bảo mật Cache (Wipe on Logout)**: Module `DataCacheManager` quản lý cache in-memory gắn chặt với `currentUserId` và `currentEpoch`. Khi người dùng đăng xuất (`logout`), toàn bộ dữ liệu nhạy cảm và phiếu lương lưu tạm trong RAM bị xóa sạch 100%, bảo đảm cách ly hoàn toàn giữa các tài khoản đăng nhập trên cùng một thiết bị.
