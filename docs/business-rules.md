# Quy tắc Nghiệp vụ Hệ thống Quản lý Nhân sự (HRM) — Business Rules

> **Tài liệu quy chuẩn logic nghiệp vụ, ma trận quyền hạn và kịch bản kiểm thử**  
> **Phiên bản:** 1.2 (Chốt quyết định T00) — Ngày lập: 10/11/2026  
> **Đối tượng áp dụng:** Backend API, Web Frontend, Mobile App  
> **Trạng thái:** `READY_FOR_REVIEW` (Đã cập nhật toàn bộ các quyết định chốt từ chủ dự án)

> [!IMPORTANT]
> **LƯU Ý PHÁP LÝ & DỮ LIỆU DEMO:**  
> Mọi tham số và quy định trong tài liệu này (khung ca ngày 08:30–17:30, quỹ phép 12 ngày/năm, hệ số làm thêm giờ 1.5/2.0/3.0, phân đoạn lương, các khoản bảo hiểm và thuế demo) **HOÀN TOÀN LÀ CHÍNH SÁCH DEMO** phục vụ kiểm thử đồ án tốt nghiệp. Chúng không cấu thành và không được tuyên bố là quy định chính thức của Bộ luật Lao động Việt Nam hay chính sách của bất kỳ doanh nghiệp thực tế nào. Khi triển khai thử nghiệm thực tế (Pilot), toàn bộ các tham số này bắt buộc phải được rà soát và cấu hình lại theo chính sách thực tế của tổ chức và quy định pháp luật hiện hành.

---

## 1. Bảng Tập trung Toàn bộ Quyết định Nghiệp vụ Đã Chốt (Confirmed Decisions)

Toàn bộ 12 nội dung từ `OPEN-01` đến `OPEN-12` đã được Chủ dự án chính thức ký duyệt chốt phương án cho bản đồ án tốt nghiệp:

| Mã quyết định | Vấn đề nghiệp vụ / kiến trúc | Quyết định đã chốt của Chủ dự án | Ranh giới thực thi & Lưu ý | Trạng thái |
| :--- | :--- | :--- | :--- | :---: |
| **`OPEN-01`** | Runtime Backend | Backend mới dùng **.NET 10 LTS**. .NET 8 chỉ phục vụ baseline legacy. | Chuẩn bị môi trường SDK theo từng task; chưa tự ý nâng toàn bộ code legacy trong T00. | `CONFIRMED` |
| **`OPEN-02`** | Database phân tách | Dùng database phát triển riêng **`HRM_Next_Dev`**, database test riêng **`HRM_Next_Test`**. Giữ nguyên `HRM_System` cũ. | Quyết định thiết kế; chưa tạo DB hoặc chạy migration trong T00. | `CONFIRMED` |
| **`OPEN-03`** | Liên kết KPI & Lương | **Tắt liên kết tự động KPI–lương trong MVP**, kể cả khi nhân viên đã có điểm KPI. | Thưởng/phạt chỉ là khoản nhập tay có quyền và lý do. Không có KPI không bị phạt. | `CONFIRMED` |
| **`OPEN-04`** | Ca làm việc & Tính công | Ca demo Thứ Hai – Thứ Sáu, **08:30 – 17:30**, nghỉ trưa **12:00 – 13:00**. Ngày nghỉ lấy từ lịch cấu hình có version. | Chỉ phần giao với ca, ngoài giờ nghỉ, được tính công chuẩn. Giờ ngoài ca không tự bù đi muộn hoặc tự thành OT. | `CONFIRMED` |
| **`OPEN-05`** | Quỹ phép năm demo | **Hạn mức demo 12 ngày/năm, không tích lũy theo tháng.** Cấp 12 ngày khi khởi tạo quỹ năm hoặc khi nhân viên mới vào trong năm. | Mỗi nhân viên/năm chỉ cấp một lần; không cộng lặp, không chuyển phép tồn. Chỉ hỗ trợ ngày nguyên; đơn qua năm bị từ chối. Phải thay đổi nếu doanh nghiệp yêu cầu. | `CONFIRMED` |
| **`OPEN-06`** | Sinh Mã nhân viên | Định dạng tự động **`NV{yyyy}{NNNN}`**, duy nhất và bất biến. | Sinh mã an toàn khi hai người tạo đồng thời (sequence/lock, không dùng MAX+1). Khi hết dải số báo lỗi rõ, không quay vòng. | `CONFIRMED` |
| **`OPEN-07`** | Mật khẩu & Kích hoạt | **Dùng lời mời kích hoạt có thời hạn để nhân viên tự đặt mật khẩu.** | Không gửi mật khẩu thô qua email, không cho HR xem mật khẩu, không có mật khẩu mặc định. Account chưa kích hoạt không được đăng nhập. Dev dùng sender thử có nhãn rõ. | `CONFIRMED` |
| **`OPEN-08`** | Hệ số OT demo | Ngày thường **1.5**, cuối tuần **2.0**, ngày nghỉ cấu hình **3.0**. | Chỉ tính phút OT được HR xác nhận, không chồng với phút công chuẩn. Không tuyên bố tuân thủ pháp luật đầy đủ. | `CONFIRMED` |
| **`OPEN-09`** | Điểm làm tròn số học | **Giữ phút/giây và decimal trong tính toán; không làm tròn giờ sớm.** Làm tròn tiền đến đồng, AwayFromZero. | Minh bạch dòng điều chỉnh làm tròn (`RoundingAdjustment`) nếu có chênh lệch giữa tổng các dòng hiển thị và Net. | `CONFIRMED` |
| **`OPEN-10`** | Xử lý Offboarding | Khóa tài khoản và thu hồi quyền truy cập. **Không tự động hủy mọi đơn Pending.** | HR xử lý đơn Pending bằng Reject có lý do & audit; cấm duyệt ngày nghỉ sau thôi việc. Đơn Approved giữ nguyên, không tự hoàn quỹ. | `CONFIRMED` |
| **`OPEN-11`** | Phân tách quyền Lương | Tách quyền: `ViewOwnPayslip`, `CalculatePayroll`, `ApprovePayroll`, `PublishPayroll`, `ExportPayroll`. | Người tính không tự duyệt chính run đó. SystemAdmin không mặc nhiên xem lương. Sửa lương cần quyền `ManageSalaryHistory`. | `CONFIRMED` |
| **`OPEN-12`** | Nghiệm thu Mobile | **Nghiệm thu Android trước, iOS hoãn.** | Bản Android tối thiểu xác định theo độ tương thích của React Native/Expo được chọn và thiết bị/emulator thực tế khi cấu hình ở T20. | `CONFIRMED` |

---

## 2. Quy tắc Quản lý Nhân sự, Vòng đời Tài khoản và Thu hồi Phiên

### 2.1. Mã định danh và Hồ sơ nhân viên
- **Mã nhân viên (Employee Code):** Sinh tự động theo định dạng `NV{yyyy}{NNNN}` (ví dụ: `NV20260001`). Cơ chế sinh mã phải an toàn khi có nhiều người tạo nhân viên đồng thời (sử dụng Sequence trong SQL Server hoặc giao dịch có khóa dòng, tuyệt đối không dùng truy vấn `MAX(MaNhanVien) + 1` thiếu bảo vệ). Khi hết dải số (vượt quá 9999 trong năm), hệ thống phải ném ngoại lệ thông báo rõ ràng, không được tự ý quay vòng dải số gây trùng lặp.
- **Hồ sơ thông tin cơ bản:** Gồm Họ tên, Ngày sinh, Giới tính, Số điện thoại, Email công ty, Địa chỉ thường trú, Tình trạng hôn nhân, Phòng ban (`DepartmentId`), Chức vụ (`PositionId`), Ngày vào làm việc (`HireDate`), Trạng thái làm việc (`Status`).
- **Cập nhật hồ sơ (Partial Update - Khắc phục F10):**
  - Mọi thao tác cập nhật hồ sơ từ giao diện Web/API phải sử dụng DTO cập nhật riêng (`UpdateEmployeeProfileRequest`), chỉ áp dụng các trường có gửi giá trị.
  - Tuyệt đối không gán `null` vào các trường dữ liệu quan trọng như `DiaChi` hoặc `TinhTrangHonNhan` khi form client không hiển thị hoặc không gửi lên.
  - Sử dụng cơ chế kiểm tra phiên bản dữ liệu (RowVersion/Concurrency Token) để chống ghi đè khi có hai người cùng chỉnh sửa một hồ sơ nhân viên.

### 2.2. Vòng đời Tài khoản và Kích hoạt Người dùng (Khắc phục F13)
- **Tạo tài khoản qua Thư mời kích hoạt:**
  - Khi HR tạo mới nhân viên, hệ thống tạo tài khoản Identity ở trạng thái `IsActive = false`.
  - Hệ thống sinh một mã kích hoạt (Activation Token) có thời hạn hiệu lực (ví dụ 48 giờ) và gửi đường dẫn kích hoạt đến email nhân viên.
  - **Không gửi mật khẩu thô qua email, không cho HR nhìn thấy mật khẩu, và không tồn tại mật khẩu mặc định dùng chung (như ngày sinh hay `Admin@123`).**
  - Nhân viên tự truy cập liên kết kích hoạt để thiết lập mật khẩu cá nhân tuân thủ chính sách bảo mật (độ dài $\ge 10$ ký tự, chữ hoa, chữ thường, số, ký tự đặc biệt). Sau khi đặt mật khẩu thành công, tài khoản mới chuyển sang `IsActive = true`.
  - **Tài khoản chưa kích hoạt (`IsActive = false`) tuyệt đối bị chặn đăng nhập và không thể gọi bất kỳ API nghiệp vụ nào.**
  - Trong môi trường phát triển (Dev/Staging), sử dụng hộp thư thử nghiệm / sender thử có gắn nhãn rõ ràng (dự kiến Mailpit/Papercut hoặc file outbox ở Task T22); không báo gửi email thật khi chưa cấu hình máy chủ SMTP thật.

### 2.3. Quy trình Thôi việc (Offboarding) và Thu hồi Phiên tức thời
- Khi nhân viên chuyển trạng thái sang "Đã nghỉ việc" (`DaNghiViec`):
  1. Cập nhật `NgayNghiViec` bằng ngày thực tế.
  2. Ngay lập tức chuyển tài khoản sang `IsActive = false` và cập nhật `SecurityStamp` trong database.
  3. **Cơ chế thu hồi phiên trực tiếp phía Server:** Đối với các request nghiệp vụ đã xác thực, Server kiểm tra trực tiếp trạng thái tài khoản (`IsActive`) và phiên bản bảo mật (`SecurityStamp`) trong database; **chưa áp dụng bộ nhớ đệm (cache) có nguy cơ làm lọt các request của tài khoản vừa bị khóa**.
  4. **Định nghĩa ranh giới thu hồi:** Bất kỳ request nghiệp vụ nào bắt đầu **sau khi thao tác khóa/thu hồi đã commit thành công** vào cơ sở dữ liệu đều bắt buộc bị từ chối với mã lỗi `401 Unauthorized`. Hệ thống không tuyên bố hủy ngược các request đã hoàn thành trước thời điểm commit.
  5. **Xử lý đơn nghỉ phép khi thôi việc:** Hệ thống **không tự động xóa/hủy hàng loạt mọi đơn `Pending`**. Toàn bộ đơn `Pending` được giữ nguyên lịch sử để HR xử lý thông qua thao tác Từ chối (`Reject`) có nhập lý do và lưu vết audit log; hệ thống cấm HR phê duyệt bất kỳ ngày nghỉ nào sau ngày nhân viên thôi việc. Các đơn đã duyệt (`Approved`) trước đó được giữ nguyên vẹn, không tự ý hoàn quỹ phép hay làm xáo trộn dữ liệu lương quá khứ.

---

## 3. Quy tắc Lịch làm việc, Chấm công và Thời gian Tính lương

### 3.1. Lịch làm việc Tiêu chuẩn (Work Calendar)
- **Múi giờ:** `Asia/Ho_Chi_Minh` (UTC+7). Mọi thời điểm được lưu trữ tại Database dưới dạng UTC ISO-8601 (`DateTimeOffset`).
- **Khung thời gian chuẩn demo:**
  - Ngày làm việc: Thứ Hai đến Thứ Sáu (5 ngày/tuần).
  - Ngày nghỉ: Lấy từ lịch cấu hình công ty có phiên bản (`WorkCalendar` có `CalendarFixtureId`, chứa danh sách ngày cuối tuần và các ngày nghỉ lễ quốc gia).
  - Khung giờ ca ngày chuẩn:
    - Giờ bắt đầu ca: `08:30:00`.
    - Giờ kết thúc ca: `17:30:00`.
    - Khung giờ nghỉ trưa: `12:00:00` đến `13:00:00` (60 phút).
- **Ràng buộc:** Bản MVP 8 tuần chỉ hỗ trợ **ca ngày cố định**. Không hỗ trợ ca đêm qua ngày (Overnight shifts); nếu ca làm qua 00:00, API trả lỗi `422 Unprocessable Entity`.

### 3.2. Phân định Thời gian Hiện diện và Thời gian Hưởng công chuẩn
1. **Thời gian hiện diện (Presence Time):** Tổng thời gian nhân viên có mặt tại cơ quan, tính từ lúc Check-in đến lúc Check-out: $\text{Thời gian hiện diện} = \text{Giờ ra} - \text{Giờ vào}$.
2. **Thời gian đủ điều kiện hưởng công chuẩn (Eligible Standard Work Time):**
   - Chỉ tính phần thời gian **nằm trong khung ca làm việc chuẩn** (08:30 – 17:30).
   - **Quy tắc giờ ngoài ca:** Việc nhân viên đến sớm (trước 08:30) hoặc ở lại muộn (sau 17:30) **KHÔNG** tự động bù đắp cho số phút đi muộn hoặc về sớm, và **KHÔNG** tự động chuyển thành giờ làm thêm (OT).
   - **Quy tắc trừ giờ nghỉ trưa:** Khung giờ nghỉ trưa cố định từ `12:00:00` đến `13:00:00` (60 phút). Khoảng làm việc của nhân viên giao với khung giờ nghỉ trưa bao nhiêu phút thì phải trừ chính xác bấy nhiêu phút.
   - *Ví dụ:* Nhân viên vào lúc 10:00, ra lúc 19:00:
     - Giao với ca chuẩn (08:30–17:30): Đoạn 10:00–17:30 dài 450 phút (7.5 giờ).
     - Giao với giờ trưa (12:00–13:00): Trọn vẹn 60 phút (1.0 giờ).
     - Phút công chuẩn hợp lệ = $450 - 60 = 390\text{ phút} = 6.5\text{ giờ}$.
     - Đi muộn: Ghi nhận 90 phút (từ 08:30 đến 10:00).
     - 90 phút ngoài ca (17:30–19:00) không bù cho 90 phút đi muộn và không tự sinh OT; ngày này chỉ nhận 6.5 giờ công chuẩn.

### 3.3. Giữ Đơn vị Tính toán và Quy tắc Làm tròn (Precision Rules)
- Mọi dữ liệu thời gian làm việc được lưu trữ và tính toán nội bộ dưới đơn vị **phút** (hoặc giây) và số thập phân `decimal`.
- **Tuyệt đối không làm tròn số giờ công trung gian sớm** (ví dụ không làm tròn $8.1666...$ giờ thành $8.17$ giờ) nhằm tránh gây sai số tiền lương tích lũy.
- Tổng số phút công chuẩn và tổng số phút OT trong cả tháng được tổng hợp đầy đủ trước khi quy đổi sang tiền lương.
- **Điểm làm tròn số học:** Làm tròn số tiền cuối cùng đến đơn vị đồng Việt Nam (VND, 0 chữ số thập phân, theo quy tắc `MidpointRounding.AwayFromZero`).
- **Minh bạch đối soát làm tròn:** Khi hiển thị trên phiếu lương hoặc báo cáo, nếu tổng các dòng thành phần hiển thị có chênh lệch $\pm 1$ đồng so với Lương thực nhận (Net) do làm tròn độc lập từng khoản, hệ thống sẽ thể hiện một dòng điều chỉnh làm tròn minh bạch (`RoundingAdjustment`) để bảo đảm:
  $$\text{Net} = \sum \text{Các dòng hiển thị}$$

### 3.4. Quy tắc Chấm công và Điều chỉnh
- **Giờ Server bắt buộc:** 100% thời gian check-in/out lấy theo đồng hồ máy chủ backend (`DateTimeOffset.UtcNow`).
- **Chống gửi trùng và Thử lại (Idempotency Rules):**
  - Mọi yêu cầu thay đổi trạng thái nhạy cảm (Check-in/Check-out, tạo đơn nghỉ phép, phê duyệt đơn, chạy tính lương) phải gửi kèm header `Idempotency-Key` (UUIDv4).
  - **Quy tắc Thử lại (Retry):** Khi client retry cùng một thao tác do mạng chập chờn hoặc timeout, **bắt buộc phải sử dụng lại chính xác `Idempotency-Key`** của lần gửi trước.
  - **Phân phạm vi (Scoped Key):** Khóa `Idempotency-Key` tại server bắt buộc phải được phân phạm vi kết hợp theo: `UserId` + `OperationType` + `IdempotencyKey`. Việc này đảm bảo một key không bị nhầm lẫn giữa các người dùng hoặc giữa các thao tác khác nhau.
  - **Chống sai lệch Payload (Payload Mismatch Protection):** Nếu client gửi lại cùng một `Idempotency-Key` nhưng có nội dung payload (tham số body) khác với lần gửi ban đầu, server phải phát hiện và **từ chối ngay lập tức** với mã lỗi `422 Unprocessable Entity` hoặc `409 Conflict`, tuyệt đối không thực thi thao tác và không ghi đè dữ liệu cũ.
  - *Lưu ý kỹ thuật:* Đây là yêu cầu kiến trúc cho Backend API mới, chưa triển khai trong Task T01.
- **Xử lý quên Check-out:** Sau 23:59:59 của ngày làm việc, bản ghi thiếu giờ ra chuyển sang trạng thái `MissingCheckout`, tạm tính 0 phút công chuẩn.
- **Điều chỉnh công:** Chỉ có người dùng có vai trò `HR` được quyền điều chỉnh giờ chấm công; bắt buộc nhập lý do điều chỉnh $\ge 10$ ký tự và hệ thống tự động ghi nhật ký vào bảng `AttendanceAuditLog`.
- **Cấm sửa công kỳ đã công bố:** **Tuyệt đối CẤM điều chỉnh chấm công của những ngày thuộc về kỳ lương đã ở trạng thái Công bố (`Published`) hoặc Khóa (`Locked`)** do bản MVP chưa có quy trình hồi tố/điều chỉnh sau chốt.

---

## 4. Quy tắc Đơn nghỉ phép và Quản lý Quỹ phép (Leave Rules)

### 4.1. Hạn mức và Cấp Quỹ phép Năm Demo
- **Hạn mức demo:** Mỗi nhân viên được cấp **12 ngày phép năm có hưởng lương** cho một năm dương lịch.
- **Cơ chế cấp:**
  - Nhân viên đang làm việc được cấp đủ 12 ngày khi hệ thống khởi tạo quỹ phép đầu năm.
  - Nhân viên mới vào làm việc trong năm cũng được cấp 12 ngày trong chính sách demo đơn giản này (không tính tỷ lệ theo tháng).
  - Mỗi nhân viên trong một năm dương lịch chỉ được cấp một lần duy nhất; không cộng dồn lặp lại, không có cơ chế chuyển phép tồn sang năm sau.
  - *Lưu ý:* Đây là chính sách demo đơn giản hóa cho đồ án; khi triển khai thực tế cho doanh nghiệp, chính sách này bắt buộc phải điều chỉnh theo quy định cụ thể của đơn vị.
- **Ràng buộc:** Chỉ hỗ trợ đơn nghỉ phép **nguyên ngày** (không hỗ trợ nửa ngày trong MVP). Đơn xin nghỉ phép có khoảng ngày vắt qua 2 năm dương lịch sẽ bị từ chối; nhân viên phải nộp 2 đơn riêng cho từng năm. Chỉ trừ phép vào các **ngày làm việc thực tế** theo lịch công ty (Thứ 2 đến Thứ 6), không trừ Thứ 7, Chủ Nhật hay ngày nghỉ lễ quốc gia.

### 4.2. Vòng đời Đơn và Phân định Kiểm tra Quỹ phép
- Trạng thái hợp lệ: `Pending` $\rightarrow$ `Approved` / `Rejected` / `Cancelled`.
- **Bước 1: Nộp đơn (`CreateLeaveRequest`):**
  - Kiểm tra khoảng ngày nghỉ không giao với đơn khác (`Pending` hoặc `Approved`) của nhân viên.
  - Kiểm tra điều kiện số dư tại thời điểm nộp: $\text{Số ngày xin nghỉ} \le \text{Số ngày phép còn lại hiện tại}$.
  - Nếu không thỏa mãn $\rightarrow$ Báo lỗi `422 Unprocessable Entity` ngay tại form nộp. Đơn nộp thành công ở trạng thái `Pending`, quỹ phép **chưa** bị trừ.
- **Bước 2: Hủy đơn (`CancelLeaveRequest`):**
  - Chỉ chính chủ nhân viên nộp đơn mới có quyền hủy đơn của mình khi đơn còn ở trạng thái `Pending`.
  - **Hủy đơn đã duyệt (`Approved`): NẰM NGOÀI PHẠM VI MVP.** Đơn đã duyệt không thể tự hủy; mọi thao tác hoàn phép đòi hỏi quy trình reversal có kiểm soát và audit sau MVP.
- **Bước 3: Phê duyệt đơn (`ApproveLeaveRequest`) & Xử lý Đồng thời:**
  - HR không được tự duyệt đơn của chính mình.
  - Cấm phê duyệt đơn nghỉ phép có ngày nghỉ thuộc về kỳ lương đã `Published` hoặc `Locked`.
  - Thao tác duyệt thực hiện trong Database Transaction kết hợp kiểm tra phiên bản dữ liệu (`RowVersion` trên bảng `LeaveBalance`).
  - Hệ thống đọc lại số dư thực tế tại thời điểm commit transaction:
    - Nếu $\text{Số ngày còn lại} \ge \text{Số ngày nghỉ}$: Cập nhật trạng thái `Approved`, trừ quỹ phép, commit transaction.
    - Nếu do xung đột đồng thời mà số dư không còn đủ: **Rollback transaction ngay lập tức**, trả mã lỗi `409 Conflict`. Quỹ phép được bảo vệ tuyệt đối không bị âm.

---

## 5. Quy tắc Tính toán, Vòng đời và Tính Bất biến của Bảng lương

### 5.1. Lịch sử Mức lương (Salary History)
- Mức lương của nhân viên được lưu trữ trong bảng `SalaryHistory` có các trường cốt lõi: `SalaryHistoryId`, `EmployeeId`, `BaseSalary` (decimal), `EffectiveFrom` (Date), `EffectiveTo` (Date, nullable).
- **Phân quyền sửa lương:** Chỉ tài khoản được cấp quyền riêng **`ManageSalaryHistory`** mới được phép tạo hoặc cập nhật các mốc lương của nhân viên; không mặc định mọi tài khoản HR đều có quyền này.
- **Quy tắc Khoảng hiệu lực không chồng lấn (No Overlapping Intervals):**
  - Các khoảng thời gian hiệu lực `[EffectiveFrom, EffectiveTo]` của cùng một nhân viên **tuyệt đối không được phép giao nhau hay chồng lấn**.
  - **Cách xác định Ngày bắt đầu / Ngày kết thúc:**
    + Khi tạo quyết định điều chỉnh mức lương mới có hiệu lực từ ngày $D_{new}$: Bản ghi mới được tạo với `EffectiveFrom = D_{new}` và `EffectiveTo = NULL` (mốc đang áp dụng).
    + Bản ghi mức lương liền trước (đang mở với `EffectiveTo == NULL` hoặc có `EffectiveTo >= D_{new}`) sẽ được tự động đóng lại bằng cách gán `EffectiveTo = D_{new}.AddDays(-1)`.
    + Khoảng hiệu lực của mốc cũ trở thành `[EffectiveFrom_cu, D_{new} - 1]`.
    + Nếu $D_{new} \le \text{EffectiveFrom của bản ghi trước}$, thao tác bị từ chối do vi phạm thứ tự thời gian hợp lệ.
- **Bảo vệ kỳ lương đã công bố:**
  - Nghiêm cấm mọi hành vi tạo mới, chỉnh sửa hoặc xóa các bản ghi lương có khoảng hiệu lực rơi vào kỳ lương đã ở trạng thái Công bố (`Published`) hoặc Khóa (`Locked`) nếu làm biến động số tiền lương đã chốt của kỳ đó.
  - Mọi quyết định thay đổi lương chỉ có hiệu lực cho các kỳ tính lương chưa công bố trong tương lai.
- *Lưu ý kỹ thuật:* Đây là quy tắc nghiệp vụ cho backend mới; chưa sửa đổi schema hay migration trong Task T01.

### 5.2. Tập hợp Nhân viên Đưa vào Tính lương
- Hệ thống chọn toàn bộ nhân viên có **thời gian làm việc giao với kỳ lương tháng $M/Y$**:
  $$\text{NgayVaoLam} \le \text{NgayCuoiKy} \quad \text{VÀ} \quad (\text{NgayNghiViec IS NULL} \;\lor\; \text{NgayNghiViec} \ge \text{NgayDauKy})$$
- Tuyệt đối không chỉ lọc nhân viên đang `Active` tại ngày chạy tính lương, để đảm bảo nhân viên đã làm việc một phần trong tháng nhưng thôi việc trước ngày tính lương vẫn được chi trả đầy đủ.

### 5.3. Công thức Lương và Thành phần Không trùng lặp
Các thành phần thu nhập và giảm trừ được định nghĩa rạch ròi, không chồng lấn:

$$\text{Tổng thu nhập (Gross)} = \text{Lương công chuẩn} + \text{Tiền làm thêm giờ (OT)} + \text{Tổng phụ cấp} + \text{Thưởng}$$

$$\text{Lương thực nhận (Net)} = \text{Tổng thu nhập (Gross)} - \text{Khấu trừ khác} - \text{Bảo hiểm demo} - \text{Thuế TNCN demo}$$

Trong đó:
1. **Lương công chuẩn:**
   - Ngày công chuẩn trong tháng ($N_{standard}$): Tổng số ngày làm việc từ Thứ 2 đến Thứ 6 trừ ngày nghỉ lễ theo `CalendarFixtureId`.
   - Đơn giá phút công chuẩn: $R_{min} = \frac{\text{Lương cơ bản}}{N_{standard} \times 8 \times 60}$.
   - Tổng phút công chuẩn được hưởng = Tổng phút làm việc hợp lệ trong ca + Tổng phút nghỉ phép có lương (tối đa 480 phút/ngày chuẩn).
   - $\text{Lương công chuẩn} = \text{Tổng phút công chuẩn được hưởng} \times R_{min}$.
   - Nếu có thay đổi lương giữa tháng: Phân đoạn độc lập từng giai đoạn theo mức lương hiệu lực tương ứng trong `SalaryHistory`.
2. **Tiền làm thêm giờ (OT demo):**
   - Chỉ tính cho các phút OT có phiếu phê duyệt của HR, không chồng với phút công chuẩn.
   - $\text{Tiền OT} = \sum \left( \text{Số phút OT} \times R_{min} \times \text{Hệ số OT demo} \right)$.
   - Hệ số OT demo: Ngày thường $1.5$, Ngày nghỉ cuối tuần $2.0$, Ngày nghỉ lễ $3.0$. Không tuyên bố đây là bộ công thức tuân thủ pháp luật đầy đủ.
3. **Đánh giá hiệu suất (KPI):**
   - **Tắt liên kết tự động KPI–lương trong MVP**, kể cả khi nhân viên đã có điểm KPI. Không có KPI không bị phạt.
   - Thưởng/phạt chỉ là khoản nhập tay có quyền và lý do cụ thể.
4. **Phụ cấp & Thưởng:** Các khoản phụ cấp và thưởng nhập tay có nhãn demo.
5. **Khấu trừ khác:** Gồm phạt vi phạm nội quy, tạm ứng trừ trong kỳ (nhập tay có nhãn demo).
6. **Bảo hiểm demo & Thuế TNCN demo:**
   - Là các khoản khấu trừ demo riêng biệt; **tuyệt đối không gộp chung vào Khấu trừ khác rồi lại trừ thêm lần nữa**.
   - MVP không tự động tính thuế lũy tiến hay tỷ lệ bảo hiểm theo luật; chỉ nhận giá trị demo nhập vào.
7. **Xử lý Lương thực nhận âm ($\text{Net} < 0$):**
   - Nếu $\text{Net} < 0$, hệ thống **chặn không cho phép Phê duyệt (`Approve`) kỳ lương**.
   - Bắt buộc Kế toán tiền lương phải xử lý nghiệp vụ tại kỳ này (ví dụ: điều chỉnh giảm khoản khấu trừ tạm ứng để $\text{Net} \ge 0$).
   - Hệ thống MVP **không** tự động sinh nghiệp vụ chuyển dư nợ sang các kỳ tương lai.

### 5.4. Vòng đời Bảng lương và Tính Bất biến (Payroll Lifecycle)
- **Các chuyển trạng thái hợp lệ:**
  - `Draft` $\rightarrow$ `Calculated`: Chạy tính toán lương trong Transaction nguyên tử, tạo `RunVersion = 1` và chụp lại Snapshot toàn bộ dữ liệu đầu vào.
  - `Calculated` $\rightarrow$ `Calculated`: Tính lại nháp khi có thay đổi dữ liệu đầu vào, tăng `RunVersion`.
  - `Calculated` $\rightarrow$ `Approved`: Phê duyệt kỳ lương. **Bắt buộc kiểm tra toàn vẹn:** Người tính không được tự duyệt chính run đó. Nếu dữ liệu chấm công, phép hoặc lương nhân viên bị thay đổi sau thời điểm `Calculate`, hệ thống từ chối duyệt và yêu cầu phải chạy tính lại trước.
  - `Approved` $\rightarrow$ `Published`: Công bố bảng lương. **Bắt buộc kiểm tra toàn vẹn:** Nếu dữ liệu đầu vào thay đổi sau bước `Approve`, hệ thống từ chối công bố và yêu cầu tính lại/duyệt lại. Khi đã công bố, bảng lương trở thành **Bất biến (Immutable)**.
  - `Published` $\rightarrow$ `Locked`: Khóa vĩnh viễn kỳ lương.
- **Quyền xem sau khi Khóa (`Locked`):**
  - Sau khi kỳ lương đã `Locked`, **nhân viên vẫn tiếp tục xem được phiếu lương đã công bố của mình**.
  - Việc khóa kỳ lương chỉ cấm sửa đổi số liệu của kỳ đó; tuyệt đối không ngăn cản việc cập nhật hồ sơ hay tăng lương nhân viên cho các kỳ tương lai.
- **Cấm sửa dữ liệu đầu vào của kỳ đã công bố:** Hệ thống cấm mọi thao tác điều chỉnh chấm công hoặc phê duyệt nghỉ phép có ngày rơi vào kỳ lương đã ở trạng thái `Published` hoặc `Locked`.

---

## 6. Mười Kịch bản Dữ liệu Kiểm thử Nghiệp vụ (Công, Phép, Lương)

> **Ghi chú kiểm chuẩn:** Toàn bộ 10 kịch bản dưới đây được xây dựng dựa trên các Calendar Fixture và Policy Version xác định. Mọi con số đã được tính toán bằng phép tính số học độc lập.

### Danh mục Calendar Fixtures áp dụng:
1. `FIXTURE-CAL-2026-04-A`: Tháng 04/2026 (30 ngày). 8 ngày cuối tuần (4, 5, 11, 12, 18, 19, 25, 26). 1 ngày lễ 30/04 (Thứ Năm). Giỗ Tổ Hùng Vương 10/3 Âm lịch rơi vào Chủ Nhật 26/04, nghỉ bù Thứ Hai 27/04 (1 ngày). Tổng ngày nghỉ = 10 ngày. **Số ngày làm việc tiêu chuẩn $N_{standard} = 20\text{ ngày}$** (160 giờ = 9,600 phút).
2. `FIXTURE-CAL-2026-04-DEMO-22D`: Tháng 04/2026 giả định demo không có ngày lễ. 8 ngày cuối tuần. **Số ngày làm việc tiêu chuẩn $N_{standard} = 22\text{ ngày}$** (176 giờ = 10,560 phút).
3. `FIXTURE-CAL-2026-05-A`: Tháng 05/2026 (31 ngày). 10 ngày cuối tuần. 1 ngày lễ 01/05 (Thứ Sáu). **Số ngày làm việc tiêu chuẩn $N_{standard} = 20\text{ ngày}$** (160 giờ = 9,600 phút).
4. `FIXTURE-CAL-2026-06-A`: Tháng 06/2026 (30 ngày). 8 ngày cuối tuần. Không có ngày lễ. **Số ngày làm việc tiêu chuẩn $N_{standard} = 22\text{ ngày}$** (176 giờ = 10,560 phút).

---

### Kịch bản 1: Đi làm đầy đủ công chuẩn (Standard Happy Path)
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-04-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260001` - Nguyễn Văn An. Mức lương cơ bản: `20,000,000 VND`.
- **Đơn giá:**
  - Đơn giá ngày công: $R_{day} = \frac{20,000,000}{20} = 1,000,000\text{ VND/ngày}$.
  - Đơn giá phút công: $R_{min} = \frac{1,000,000}{480} = 2,083.3333...\text{ VND/phút}$.
- **Dữ liệu chấm công:** Đi làm đủ 20 ngày làm việc. Mỗi ngày quẹt vào `08:25`, quẹt ra `17:35`.
  - Khung làm việc 08:25–17:35 bao trọn ca chuẩn (08:30–17:30) và giờ trưa (12:00–13:00).
  - Phút công chuẩn hợp lệ mỗi ngày = đúng 480 phút ($8.0\text{ giờ}$). 5 phút đến sớm và 5 phút về muộn không có đơn OT nên không tính.
  - Tổng phút công chuẩn trong tháng = $20 \times 480 = 9,600\text{ phút}$.
- **Dữ liệu phụ cấp & giảm trừ demo:**
  - Phụ cấp ăn trưa demo: `1,000,000 VND`.
  - Khấu trừ khác: `0 VND`.
  - Bảo hiểm demo (nhập tay): `2,100,000 VND`.
  - Thuế TNCN demo (nhập tay): `500,000 VND`.
- **Phép tính kết quả:**
  - $\text{Lương công chuẩn} = 9,600 \times 2,083.3333... = 20,000,000\text{ VND}$.
  - $\text{Tiền OT} = 0\text{ VND}$.
  - $\text{Tổng thu nhập (Gross)} = 20,000,000 + 0 + 1,000,000 + 0 = 21,000,000\text{ VND}$.
  - $\text{Lương thực nhận (Net)} = 21,000,000 - 0 - 2,100,000 - 500,000 = \mathbf{18,400,000\text{ VND}}$.
- **Kiểm tra biên / lỗi:** Đi muộn = 0 phút, Về sớm = 0 phút.

---

### Kịch bản 2: Quên Check-out và được HR điều chỉnh có lý do
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-04-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260002` - Trần Thị Bình. Mức lương: `16,000,000 VND`. Đơn giá ngày: $R_{day} = \frac{16,000,000}{20} = 800,000\text{ VND/ngày}$.
- **Dữ liệu đầu vào:**
  - 19 ngày đi làm đủ 8.0h công chuẩn ($19 \times 480 = 9,120\text{ phút}$).
  - Ngày 10/04/2026: Check-in `08:30`, quên Check-out.
  - Hết ngày 10/04: Bản ghi chuyển thành `MissingCheckout`, phút công ngày 10/04 tạm tính = 0.
  - Ngày 11/04: HR kiểm tra camera lễ tân, thực hiện điều chỉnh trên Web: nhập `GioRa = 17:30:00`, nhập lý do: *"Xác nhận qua camera lễ tân có mặt làm việc đến 17:30"*.
- **Phép tính sau điều chỉnh:**
  - Ngày 10/04 sau điều chỉnh có thời gian đủ điều kiện hưởng công chuẩn = $(17:30 - 08:30) - 1\text{h trưa} = 8.0\text{ giờ}$ (480 phút).
  - Tổng phút công chuẩn cả tháng = $9,120 + 480 = 9,600\text{ phút}$ (đủ 20 công chuẩn).
  - Khấu trừ khác = 0, Bảo hiểm demo = `1,680,000 VND`, Thuế demo = `0 VND`.
- **Phép tính kết quả:**
  - $\text{Lương công chuẩn} = 20 \times 800,000 = 16,000,000\text{ VND}$.
  - $\text{Tổng thu nhập (Gross)} = 16,000,000\text{ VND}$.
  - $\text{Lương thực nhận (Net)} = 16,000,000 - 1,680,000 = \mathbf{14,320,000\text{ VND}}$.
- **Kiểm tra biên / lỗi:** Bảng `AttendanceAuditLog` phải lưu bản ghi có `ModifiedBy = HR_ID`, `OldGioRa = null`, `NewGioRa = 17:30`, lý do $\ge 10$ ký tự.

---

### Kịch bản 3: Làm thêm giờ (OT) ngày nghỉ cuối tuần có phê duyệt
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-04-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260003` - Lê Hoàng Cường. Mức lương: `16,000,000 VND`.
  - Đơn giá phút chuẩn: $R_{min} = \frac{16,000,000}{20 \times 480} = 1,666.6666...\text{ VND/phút}$.
- **Dữ liệu đầu vào:**
  - Đi làm ngày thường đủ 20 ngày công chuẩn ($9,600\text{ phút}$).
  - Thứ Bảy (18/04/2026) có phiếu duyệt OT làm 4.0 giờ buổi sáng (`08:00 – 12:00` = 240 phút). Quẹt thẻ thực tế: vào 08:00, ra 12:00.
  - Hệ số OT cuối tuần demo: $\times 2.0$.
- **Phép tính kết quả:**
  - $\text{Lương công chuẩn} = 9,600 \times R_{min} = 16,000,000\text{ VND}$.
  - $\text{Tiền OT} = 240\text{ phút} \times R_{min} \times 2.0 = 240 \times 1,666.6666... \times 2.0 = 800,000\text{ VND}$.
  - $\text{Tổng thu nhập (Gross)} = 16,000,000 + 800,000 = 16,800,000\text{ VND}$.
  - Bảo hiểm demo = `1,680,000 VND`, Thuế demo = `200,000 VND`.
  - $\text{Lương thực nhận (Net)} = 16,800,000 - 1,680,000 - 200,000 = \mathbf{14,920,000\text{ VND}}$.
- **Kiểm tra biên / lỗi:** Nếu quẹt thẻ vào Thứ Bảy mà không có phiếu duyệt OT $\rightarrow$ Gán `UnscheduledPunch`, tiền OT = 0 VND.

---

### Kịch bản 4: Nghỉ phép năm có hưởng lương vắt qua ngày cuối tuần
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-05-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260004` - Phạm Minh Dũng. Quỹ phép còn lại: `5.0 ngày`. Lương: `18,000,000 VND`.
  - Đơn giá ngày: $R_{day} = \frac{18,000,000}{20} = 900,000\text{ VND/ngày}$.
- **Dữ liệu đầu vào:** Xin nghỉ phép từ Thứ Năm (07/05/2026) đến hết Thứ Ba tuần sau (12/05/2026).
- **Quy tắc trừ phép:**
  - Khoảng ngày gồm: Thứ 5 (07/05), Thứ 6 (08/05), Thứ 7 (09/05), CN (10/05), Thứ 2 (11/05), Thứ 3 (12/05).
  - Loại trừ Thứ 7 và CN $\rightarrow$ Số ngày làm việc thực tế xin nghỉ = $4.0\text{ ngày}$.
  - Quỹ phép đủ ($5.0 \ge 4.0$). Khi HR duyệt $\rightarrow$ Quỹ phép còn lại cập nhật = $5.0 - 4.0 = \mathbf{1.0\text{ ngày}}$.
- **Phép tính công & lương tháng 05/2026:**
  - Đi làm thực tế 16 ngày chuẩn ($16 \times 480 = 7,680$ phút) + 4 ngày phép hưởng nguyên lương ($4 \times 480 = 1,920$ phút).
  - Tổng phút công chuẩn được hưởng = $7,680 + 1,920 = 9,600\text{ phút}$ (đủ 20 công chuẩn).
  - $\text{Lương công chuẩn} = 20 \times 900,000 = 18,000,000\text{ VND}$.
  - $\text{Tổng thu nhập (Gross)} = 18,000,000\text{ VND}$.
  - Bảo hiểm demo = `1,890,000 VND`, Thuế demo = `300,000 VND`.
  - $\text{Lương thực nhận (Net)} = 18,000,000 - 1,890,000 - 300,000 = \mathbf{15,810,000\text{ VND}}$.
- **Kiểm tra biên / lỗi:** Quỹ phép bị trừ đúng 4 ngày, tuyệt đối không bị trừ 6 ngày.

---

### Kịch bản 5: Không đủ quỹ phép tại thời điểm tạo đơn
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-05-A`. `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260005` - Hoàng Thu Em. Quỹ phép năm còn lại: `1.0 ngày`.
- **Hành động:** Nhân viên nộp đơn xin nghỉ phép năm 3 ngày làm việc (13/05/2026 đến 15/05/2026).
- **Quy tắc kiểm tra lúc tạo đơn:**
  - $\text{Số ngày nghỉ yêu cầu} = 3.0 > \text{Số ngày phép còn lại} = 1.0$.
- **Kết quả mong đợi:**
  - Server trả lỗi ngay tại bước gọi API `POST /leave-requests`: mã `422 Unprocessable Entity` kèm thông điệp: *"Số ngày phép năm còn lại (1.0 ngày) không đủ cho đơn nghỉ 3.0 ngày"*.
  - Đơn nghỉ không được tạo; quỹ phép giữ nguyên $1.0\text{ ngày}$.
- **Kiểm tra biên / lỗi:** Tách biệt hoàn toàn việc kiểm tra số dư lúc tạo đơn với kiểm tra lúc phê duyệt.

---

### Kịch bản 6: Hai Quản lý duyệt 2 đơn phép đồng thời (Race Condition)
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-06-A`. `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260006` - Đỗ Quốc Giang. Quỹ phép còn lại: đúng `2.0 ngày`.
- **Dữ liệu đầu vào:**
  - Nhân viên có 2 đơn `Pending`: Đơn A (nghỉ 2 ngày: 04–05/06) và Đơn B (nghỉ 1 ngày: 18/06).
  - Cả 2 đơn đều hợp lệ lúc nộp vì $2.0 \le 2.0$ và $1.0 \le 2.0$.
  - Hai HR cùng mở form và gửi lệnh duyệt đồng thời tại thời điểm $T_0$.
- **Xử lý đồng thời (Concurrency Control qua Database Transaction & RowVersion):**
  - **Trường hợp A thắng (Giao dịch Đơn A commit trước):**
    - Đơn A duyệt thành công $\rightarrow$ Quỹ phép trừ 2.0 ngày, còn $0.0\text{ ngày}$.
    - Giao dịch Đơn B phát hiện xung đột `RowVersion` hoặc đọc lại thấy số dư $0.0 < 1.0 \rightarrow$ Giao dịch Đơn B tự động Rollback, trả về mã lỗi `409 Conflict`.
    - Kết quả: Quỹ phép còn đúng $0.0\text{ ngày}$ (không bị âm).
  - **Trường hợp B thắng (Giao dịch Đơn B commit trước):**
    - Đơn B duyệt thành công $\rightarrow$ Quỹ phép trừ 1.0 ngày, còn $1.0\text{ ngày}$.
    - Giao dịch Đơn A phát hiện số dư hiện tại $1.0 < 2.0 \rightarrow$ Giao dịch Đơn A tự động Rollback, trả về mã lỗi `409 Conflict`.
    - Kết quả: Quỹ phép còn đúng $1.0\text{ ngày}$ (không bị âm).
  - **Quy tắc kiểm thử xác định (Deterministic Test Harness):** Trong bài test tự động, test harness sẽ điều khiển thứ tự giao dịch (khóa barrier/lần lượt) để kiểm chứng cả 2 nhánh kịch bản mà không để xảy ra trạng thái bất định.
- **Kiểm tra biên / lỗi:** Trong cả 2 trường hợp, quỹ phép **tuyệt đối không bao giờ bị âm** ($-1.0$ ngày).

---

### Kịch bản 7: Thay đổi Lương cơ bản giữa tháng (Prorated Salary Split)
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-06-A` ($N_{std} = 22\text{ ngày}$ làm việc). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260007` - Bùi Hải Hà. Đi làm đủ 22 ngày công chuẩn trong tháng:
  - Đoạn 1 (từ 01/06 đến 14/06): có 10 ngày làm việc.
  - Đoạn 2 (từ 15/06 đến 30/06): có 12 ngày làm việc.
- **Lịch sử mức lương (`SalaryHistory`):**
  - Mức lương cũ: `13,200,000 VND` (hiệu lực đến hết 14/06/2026).
  - Mức lương mới: `17,600,000 VND` (hiệu lực từ 15/06/2026).
- **Phép tính độc lập:**
  - Đơn giá ngày đoạn 1: $R_{day1} = \frac{13,200,000}{22} = 600,000\text{ VND/ngày}$.
  - Lương đoạn 1 = $10 \times 600,000 = 6,000,000\text{ VND}$.
  - Đơn giá ngày đoạn 2: $R_{day2} = \frac{17,600,000}{22} = 800,000\text{ VND/ngày}$.
  - Lương đoạn 2 = $12 \times 800,000 = 9,600,000\text{ VND}$.
  - $\text{Lương công chuẩn} = 6,000,000 + 9,600,000 = 15,600,000\text{ VND}$.
  - Phụ cấp demo = `500,000 VND`, Khấu trừ khác = 0, Bảo hiểm demo = `1,638,000 VND`, Thuế demo = `200,000 VND`.
  - $\text{Tổng thu nhập (Gross)} = 15,600,000 + 500,000 = 16,100,000\text{ VND}$.
  - $\text{Lương thực nhận (Net)} = 16,100,000 - 0 - 1,638,000 - 200,000 = \mathbf{14,262,000\text{ VND}}$.
- **Kiểm tra biên / lỗi:** Tổng số ngày công tính của 2 đoạn = $10 + 12 = 22 = N_{std}$.

---

### Kịch bản 8: Nhân viên chưa có Đánh giá Hiệu suất (Missing KPI)
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-04-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260008` - Vũ Tuấn Kiệt. Lương: `15,000,000 VND`. Đơn giá ngày: $R_{day} = \frac{15,000,000}{20} = 750,000\text{ VND/ngày}$.
- **Dữ liệu đầu vào:**
  - Đi làm đủ 20 ngày công chuẩn ($20 \times 480 = 9,600$ phút).
  - Chưa được chấm điểm KPI trong kỳ (`DiemTong == NULL`).
- **Quy tắc áp dụng (Đã chốt OPEN-03):**
  - Tắt liên kết tự động KPI–lương trong MVP. Không có KPI không bị phạt.
  - $\text{Lương công chuẩn} = 20 \times 750,000 = 15,000,000\text{ VND}$.
  - Phụ cấp = 0, Khấu trừ khác = 0, Bảo hiểm demo = `1,575,000 VND`, Thuế demo = `0 VND`.
  - $\text{Tổng thu nhập (Gross)} = 15,000,000\text{ VND}$.
  - $\text{Lương thực nhận (Net)} = 15,000,000 - 1,575,000 = \mathbf{13,425,000\text{ VND}}$.
- **Kiểm tra biên / lỗi:** Nhân viên **tuyệt đối không bị trừ phạt âm 10%** lương (không bị trừ mất 1,500,000 VND).

---

### Kịch bản 9: Sửa lương sau khi kỳ trước đã Khóa (Payroll Immutability)
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-04-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260009` - Đặng Thu Loan.
  - Kỳ tháng 04/2026 đã được duyệt, công bố và chuyển sang `Locked` ngày 05/05/2026 với mức lương cơ bản lúc đó là `12,000,000 VND`. Lương thực nhận đã chốt trong snapshot: `10,740,000 VND`.
- **Sự kiện phát sinh:**
  - Ngày 10/05/2026, tài khoản có quyền **`ManageSalaryHistory`** vào hệ thống tạo bản ghi mới nâng mức lương cơ bản của nhân viên Loan lên `16,000,000 VND`, gán hiệu lực từ ngày `01/05/2026`.
  - Kế toán hoặc nhân viên mở lại phiếu lương tháng 04/2026 trên Web/Mobile.
- **Quy tắc áp dụng (Đã chốt OPEN-11 & Payroll Immutability):**
  - API `GET /payroll-periods/{id}/items` thuần túy là Read-Only, chỉ đọc dữ liệu từ snapshot trong `PayrollItem`.
  - Kỳ đã `Locked` hoàn toàn bất biến. Mức lương mới chỉ áp dụng cho kỳ tháng 05/2026 trở đi.
- **Kết quả mong đợi:**
  - Phiếu lương tháng 04/2026 của nhân viên Loan vẫn hiển thị chính xác mức lương cơ bản $12,000,000\text{ VND}$ và thực nhận cũ $\mathbf{10,740,000\text{ VND}}$.
  - Sau khi khóa, nhân viên vẫn xem được phiếu lương tháng 04 bình thường.

---

### Kịch bản 10: Xử lý Khấu trừ vượt Lương (Lương Thực nhận Âm)
- **Tham chiếu:** `CalendarFixtureId = FIXTURE-CAL-2026-04-A` ($N_{std} = 20\text{ ngày}$). `PolicyVersion = POL-2026-V1`.
- **Nhân viên:** `NV20260010` - Mai Văn Nam. Lương: `10,000,000 VND`. Đơn giá ngày: $R_{day} = \frac{10,000,000}{20} = 500,000\text{ VND/ngày}$.
- **Dữ liệu đầu vào:**
  - Nhân viên chỉ đi làm 4 ngày công ($4 \times 500,000 = 2,000,000\text{ VND}$); nghỉ không lương 16 ngày còn lại.
  - Trong kỳ có khoản khấu trừ tạm ứng nhập tay là `3,000,000 VND`.
  - Bảo hiểm demo = 0, Thuế demo = 0.
- **Phép tính:**
  - $\text{Tổng thu nhập (Gross)} = 2,000,000\text{ VND}$.
  - $\text{Khấu trừ khác} = 3,000,000\text{ VND}$.
  - $\text{Lương thực nhận (Net)} = 2,000,000 - 3,000,000 = \mathbf{-1,000,000\text{ VND}} < 0$.
- **Quy tắc xử lý ngoại lệ:**
  - Hệ thống phát hiện $\text{Net} < 0 \rightarrow$ Gắn cờ cảnh báo `FlaggedNegativeNet`, **CHẶN không cho phép duyệt kỳ lương (`Approve`)**.
  - Kế toán tiền lương bắt buộc phải xử lý nghiệp vụ tại kỳ này: Điều chỉnh giảm khoản khấu trừ tạm ứng của kỳ này xuống mức tối đa là `2,000,000 VND` để $\text{Net} = 0\text{ VND}$.
  - Hệ thống MVP **không** tự động sinh nghiệp vụ chuyển dư nợ sang kỳ sau.
- **Kết quả sau điều chỉnh:** Bảng lương có $\text{Net} = \mathbf{0\text{ VND}}$, được phép duyệt bình thường.


## T12 — Quy tắc liên module được chốt (12/11/2026)

- Phép nguyên ngày Approved và công chuẩn/OT không cùng hưởng cho ngày có lịch chuẩn >0. Pending chưa trừ quỹ, cho phép chấm công nhưng approve recheck. Hiện diện mở cũng phải được xử lý trước duyệt phép; HR có thể sửa về khoảng hoàn tất0 phút chuẩn, giữ event/audit gốc. Cấm khôi phục công dương/OT khi ngày phép đã duyệt.
- Cả hai module dùng chung khóa employee SQL trong transaction; thứ tự request không được làm đổi bất biến. Ngày nghỉ trong khoảng phép không trừ quỹ và không tự có công chuẩn hoặc OT.
- Receipt chưa có TTL hoặc reuse trong MVP, được giữ lâu dài cùng bản ghi. Retry cùng key sau restart vẫn replay, payload khác bị409. Attendance bắt buộc key; leave giữ key/RowVersion optional theo quyết định T11 để tương thích, không hứa replay khi không gửi key.
- Chi tiết, seed và fault/mutation controls: `docs/decisions/T12-invariants.md`. Chưa thay thế calculator lương T13/T14.


## T13 — Công thức demo và snapshot được hiện thực (13/11/2026)

- Denominator là tổng phút chuẩn của lịch cả tháng đã pin, hỗ trợ ca khác480 phút và không cố định26 ngày. Cộng tử số salaryBase×creditedMinutes và salaryBase×approvedOT×factor theo ngày rồi chia một lần; không cộng các tỷ lệ đã làm tròn/chia lặp.
- Snapshot giữ riêng input/source/output, policy POL-2026-V1 và raw/display amounts. Signed display lines có RoundingAdjustment, tổng bằng Net. Raw net âm hoặc checkout thiếu không được approve/publish; missing hire/salary/calendar fail-closed.
- Salary append tăng ngày, giữ MonthlyBase cũ, đóng interval trước ở ngày liền trước; SQL bảo vệ unique date/một interval mở, source/employee locks bảo vệ giao dịch. Khoản demo nhập tay chỉ có5 loại, void/replacement có lý do/version, không tự tính PIT/BHXH/KPI.
- Employee đã nghỉ việc vẫn được chọn nếu employment giao tháng; chỉ trả phần ngày trong employment. Future salary không đổi snapshot/Payslip đã Published/Locked. Đầu vào tiền API tối đa2 chữ số lẻ, quá precision bị từ chối, không làm tròn SQL âm thầm.
- Capture trực tiếp cũng là calculation, phải gắn đúng CalculatedBy của batch mới để chống tự duyệt. Các endpoint ghi T13 mới bắt buộc UUIDv4 key. Compatibility metadata-only cũ khi chưa có salary source không được coi là engine lương thật; lifecycle/run idempotency/tính lại Approved còn thuộc T14.
- Chi tiết ranh giới, evidence cộng tác và phần dữ liệu bổ sung fixture09: `docs/decisions/T13-calculation.md`.


## T14 — Vòng đời run lương được hiện thực (13/11/2026)

- Kỳ tạo bằng API mới luôn SnapshotRequired; lệnh workflow cần key/version, không dùng metadata-only làm kết quả lương thật.
- Active/Approved/PublishedBatchId gắn đúng immutable run; stale nguồn ở approve/publish bị409, phải calculate lại rõ ràng. Calculate từ Approved xóa approval cũ chỉ khi run mới thành công trong cùng transaction; lỗi giữ nguyên kỳ/run cũ.
- Published→Locked không đổi tiền, employee vẫn đọc own Published slip; future salary không tính lại lịch sử. Locked bất biến qua luồng ghi được hỗ trợ.
- Receipt actor/operation/key lưu bền; retry sau mất completion không tăng version/công bố/audit lần hai. Quyền được kiểm lại trước replay. Chi tiết tại docs/decisions/T14-workflow.md; compatibility reason-only cũ chỉ giữ cho fixture metadata-only.

## T15 — Phiếu lương, đối soát và CSV

Phiếu chi tiết đọc đúng run/snapshot của kỳ, chỉ owner thấy bản Published của mình (kể cả kỳ Locked); payroll.read_all cho phép preview bản tính. Tổng đối soát COUNT/SUM toàn kỳ từ snapshot, không cộng riêng page, không dùng salary hiện tại. Version/amount khác snapshot thì409.

CSV toàn kỳ cần payroll.export, chỉ Published/Locked, maxRows mặc định500/tối đa1000; vượt giới hạn trả409, không cắt file. UTF-8 BOM và CRLF, text quote/escape và prefix apostrophe chống formula injection kể cả sau control/whitespace. Số decimal dùng InvariantCulture. Không CCCD/ngân hàng/địa chỉ; tên/mã là nhãn hiện tại, tiền là snapshot lịch sử. Audit ghi actor/period/run/row count trước response, không ghi file/tiền/tên; audit lỗi thì không cấp file. Detail/summary không ghi dữ liệu.

Xem docs/decisions/T15-payslips.md. Không thay công thức demo T13; PDF/mail thật/chuyển khoản/thuế tự động chưa triển khai.
