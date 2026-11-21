# Biên bản Ký duyệt Quyết định Nhiệm vụ T00 (T00 Sign-off Record)

> **Dự án:** Hệ thống Quản lý Nhân sự (HRM System) — Đồ án Tốt nghiệp 8 tuần  
> **Ngày ký duyệt:** 10/11/2026  
> **Người ký duyệt:** Chủ dự án (Sinh viên thực hiện đồ án)  
> **Đơn vị hỗ trợ:** AI Coding Assistant (Antigravity)  
> **Trạng thái nhiệm vụ:** `VERIFIED` (Chủ dự án đã chính thức nghiệm thu ngày 10/11/2026; chuyển giao sang T01)

> [!IMPORTANT]
> **TUYÊN BỐ PHÁP LÝ & RANH GIỚI KỸ THUẬT:**
>
> 1. Toàn bộ các tham số giờ làm việc, hạn mức phép 12 ngày/năm, hệ số làm thêm giờ (OT), phân đoạn lương và các khoản thuế/bảo hiểm demo dưới đây **HOÀN TOÀN LÀ CHÍNH SÁCH DEMO** phục vụ kiểm thử đồ án tốt nghiệp, không phải quy định pháp luật hay chính sách của bất kỳ doanh nghiệp thực tế nào.
> 2. **Việc chốt các quyết định thiết kế dưới đây KHÔNG đồng nghĩa với việc tính năng đã được triển khai.** Toàn bộ mã nguồn sản phẩm, cơ sở dữ liệu và cấu hình triển khai tại mốc T00 đều giữ nguyên 100%, chưa có bất kỳ dòng code sản phẩm nào bị sửa đổi hay migration nào được thực thi.

---

## 1. Danh mục 12 Quyết định Đã Chốt Chính thức

|      Mã       | Nội dung quyết định            | Chi tiết phương án đã chốt                                                                                                                                                                      | Ranh giới thực thi                                                                                                                                                                                                        |
| :-----------: | :----------------------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **`OPEN-01`** | **Runtime Backend mới**        | Backend mới sử dụng **.NET 10 LTS**. Nền tảng .NET 8 chỉ phục vụ mục tiêu tham chiếu baseline legacy.                                                                                           | Chuẩn bị môi trường .NET 10 SDK theo từng task triển khai; chưa tự ý nâng toàn bộ code legacy trong Task T00.                                                                                                             |
| **`OPEN-02`** | **Phân tách Database**         | Sử dụng database phát triển riêng **`HRM_Next_Dev`** và database kiểm thử riêng **`HRM_Next_Test`**. Giữ nguyên cơ sở dữ liệu `HRM_System` cũ làm đối chiếu.                                    | Đây là quyết định thiết kế schema và cấu hình kết nối; chưa tạo DB hoặc chạy migration trong Task T00.                                                                                                                    |
| **`OPEN-03`** | **Liên kết KPI & Lương**       | **Tắt liên kết tự động KPI–lương trong bản MVP**, kể cả khi nhân viên đã có điểm KPI.                                                                                                           | Thưởng/phạt chỉ là khoản nhập tay độc lập có quyền hạn và lý do rõ ràng. Không có điểm KPI tuyệt đối không bị phạt.                                                                                                       |
| **`OPEN-04`** | **Ca làm việc & Tính công**    | Ca demo Thứ Hai – Thứ Sáu, **08:30 – 17:30**, nghỉ trưa **12:00 – 13:00**. Ngày nghỉ lấy từ lịch cấu hình có phiên bản (`WorkCalendar`).                                                        | Chỉ phần thời gian giao với ca chuẩn, ngoài giờ nghỉ trưa, mới được tính công chuẩn. Giờ ngoài ca không tự bù cho đi muộn và không tự thành OT.                                                                           |
| **`OPEN-05`** | **Hạn mức Quỹ phép demo**      | **Hạn mức demo 12 ngày/năm, không tích lũy theo tháng.** Nhân viên đang làm được cấp 12 ngày khi khởi tạo quỹ năm; nhân viên mới vào trong năm cũng được cấp 12 ngày trong chính sách demo này. | Mỗi nhân viên/năm chỉ cấp một lần; không cộng dồn lặp, không chuyển phép tồn sang năm sau. Chỉ hỗ trợ ngày nguyên; đơn qua năm bị từ chối. Chính sách này phải thay đổi nếu doanh nghiệp yêu cầu.                         |
| **`OPEN-06`** | **Sinh Mã nhân viên**          | Mã nhân viên tự sinh định dạng **`NV{yyyy}{NNNN}`**, duy nhất và bất biến.                                                                                                                      | Cơ chế sinh mã an toàn khi hai người tạo đồng thời (sequence/lock, không dùng `MAX+1` thiếu bảo vệ). Khi hết dải số (vượt 9999 trong năm), hệ thống báo lỗi rõ, không quay vòng tạo trùng.                                |
| **`OPEN-07`** | **Kích hoạt Tài khoản**        | **Dùng lời mời kích hoạt có thời hạn để nhân viên tự đặt mật khẩu.**                                                                                                                            | Không gửi mật khẩu thô qua email, không cho HR xem mật khẩu, không có mật khẩu mặc định dùng chung. Tài khoản chưa kích hoạt bị chặn đăng nhập. Môi trường Dev dùng sender thử có nhãn rõ; chi tiết triển khai ở T06/T22. |
| **`OPEN-08`** | **Hệ số OT demo**              | Hệ số OT demo: Ngày thường **1.5**, Ngày nghỉ cuối tuần **2.0**, Ngày nghỉ cấu hình **3.0**.                                                                                                    | Chỉ tính phút OT được HR xác nhận, không chồng với phút công chuẩn. Không tuyên bố đây là bộ công thức tuân thủ pháp luật đầy đủ.                                                                                         |
| **`OPEN-09`** | **Điểm làm tròn số học**       | **Giữ phút/giây và decimal trong suốt pipeline tính toán; không làm tròn giờ sớm.** Làm tròn tiền đến đồng (VND), quy tắc AwayFromZero.                                                         | Thể hiện dòng điều chỉnh làm tròn minh bạch (`RoundingAdjustment`) nếu có chênh lệch giữa tổng các dòng thành phần hiển thị và Net.                                                                                       |
| **`OPEN-10`** | **Quy trình Offboarding**      | Offboarding tự động khóa tài khoản và thu hồi quyền truy cập. **Không tự động xóa/hủy mọi đơn `Pending`.**                                                                                      | HR xử lý đơn `Pending` bằng thao tác `Reject` có lý do và audit log; cấm duyệt ngày nghỉ sau ngày thôi việc. Đơn `Approved` giữ nguyên, không tự hoàn quỹ hay đổi lịch sử lương.                                          |
| **`OPEN-11`** | **Phân tách quyền Lương**      | Tách riêng các quyền: `ViewOwnPayslip`, `CalculatePayroll`, `ApprovePayroll`, `PublishPayroll`, `ExportPayroll`.                                                                                | Người tính lương không được tự duyệt chính run đó. SystemAdmin không mặc nhiên xem lương. Sửa mức lương phải do tài khoản có quyền `ManageSalaryHistory`.                                                                 |
| **`OPEN-12`** | **Nền tảng nghiệm thu Mobile** | **Nghiệm thu trên nền tảng Android trước; nền tảng iOS tạm hoãn.**                                                                                                                              | Phiên bản Android tối thiểu sẽ xác định căn cứ theo bản tương thích của React Native/Expo được chọn và thiết bị/emulator thực tế khi cấu hình ở Task T20. Không suy đoán thiết bị cá nhân của chủ dự án.                  |

---

## 2. Quy chuẩn Bổ sung về Xác thực và Tính Bất biến Bảng lương

1. **Thu hồi phiên làm việc (Session Revocation):**
   - Với bản MVP, server kiểm tra trực tiếp trạng thái tài khoản (`IsActive`) và phiên bản bảo mật (`SecurityStamp`) cho mọi request nghiệp vụ đã xác thực; **chưa áp dụng cache có nguy cơ làm lọt tài khoản vừa bị khóa**.
   - Bất kỳ request nghiệp vụ nào bắt đầu **sau khi thao tác khóa/thu hồi đã commit thành công** vào cơ sở dữ liệu đều bắt buộc bị từ chối `401 Unauthorized`. Không tuyên bố hủy ngược các request đã hoàn thành trước thời điểm commit.
   - Có kịch bản kiểm thử riêng biệt cho: Cookie validation (Web), Bearer Access Token hiện hữu (Mobile), và Refresh Token.
   - Refresh Token là module mở rộng ngoài Identity cơ bản; cấu trúc bảng và lifecycle chi tiết sẽ được quyết định tại Task T06.
   - Tuyệt đối không tự viết thuật toán ký hoặc mã hóa token.

2. **Vòng đời Bảng lương và Kiểm tra Đầu vào:**
   - Hệ thống thực hiện kiểm tra phiên bản dữ liệu đầu vào (công, phép, mức lương) ở **cả bước Phê duyệt (`Approve`) và bước Công bố (`Publish`)**.
   - Nếu dữ liệu đầu vào bị thay đổi sau khi chạy tính toán (`Calculate`) hoặc sau khi duyệt (`Approve`), hệ thống từ chối thực hiện và yêu cầu phải chạy tính lại và duyệt lại từ đầu.
   - Bảng lương ở trạng thái `Published` và `Locked` là **Bất biến (Immutable)**; sau khi khóa, nhân viên vẫn tiếp tục xem được phiếu lương đã công bố của mình bình thường.
   - **Cấm sửa đổi công/phép:** Hệ thống chặn toàn bộ thao tác điều chỉnh chấm công hoặc duyệt phép thuộc về các kỳ lương đã `Published` hoặc `Locked`.

3. **Chi tiết Nghiệp vụ Idempotency (Chống trùng lặp & Thử lại):**
   - Client retry cùng một thao tác (Check-in/Check-out, nộp đơn phép, tính lương) **bắt buộc phải sử dụng lại chính xác `Idempotency-Key`** của lần gửi trước.
   - Key được phân phạm vi (scoped) kết hợp theo `UserId` + `OperationType` + `IdempotencyKey`.
   - Cùng key nhưng gửi payload khác nhau phải bị từ chối với mã lỗi `422 Unprocessable Entity` hoặc `409 Conflict`.
   - *Lưu ý:* Đây là yêu cầu cho Backend API mới, chưa triển khai trong Task T01.

4. **Chi tiết Nghiệp vụ Lịch sử Mức lương (SalaryHistory):**
   - Các khoảng thời gian hiệu lực `[EffectiveFrom, EffectiveTo]` của cùng một nhân viên **tuyệt đối không được phép giao nhau hay chồng lấn**.
   - Cách xác định: Bản ghi mới có `EffectiveFrom = D_new`, `EffectiveTo = NULL`. Bản ghi trước được tự động đóng lại bằng cách gán `EffectiveTo = D_new.AddDays(-1)`.
   - Nghiêm cấm mọi thay đổi dữ liệu lương làm biến động số tiền của các kỳ lương đã Công bố (`Published`) hoặc Khóa (`Locked`).
   - *Lưu ý:* Đây là yêu cầu nghiệp vụ; chưa sửa đổi schema hay migration trong Task T01.

---

## 3. Kế hoạch Chuẩn bị cho Nhiệm vụ Tiếp theo: T01 (Chưa Thực thi)

Khi biên bản này và bộ tài liệu T00 được chủ dự án nghiệm thu, bước tiếp theo sẽ là **T01 — Đóng băng baseline và dựng kiểm thử hồi quy**:

1. **Đóng băng baseline:** Ghi nhận và tạo tag baseline cho commit hiện tại trong Git để làm căn cứ so sánh.
2. **Thiết lập project kiểm thử độc lập:** Tạo project test riêng biệt (`HRM.Tests.Regression` hoặc `backend/tests`) độc lập hoàn toàn với mã nguồn sản phẩm.
3. **Bộ kiểm thử hồi quy đầy đủ (Không thu hẹp phạm vi):**
   - [ ] Test phát hiện F06: Token reset mật khẩu kiểm tra đúng mục đích (`Purpose`), từ chối token sai mục đích (như VerifyEmail).
   - [ ] Test phát hiện F05: Kiểm tra giao dịch duyệt phép, chặn trường hợp duyệt vượt quỹ làm số dư bị âm.
   - [ ] Test phát hiện F04: Kiểm tra công thức hiệu suất khi thiếu đánh giá (`diemTong == null`), xác nhận hệ số trung tính 0.00% và không bị trừ âm 10%.
   - [ ] Test phát hiện F02: Kiểm tra tính bất biến của lịch sử lương, xác nhận việc cập nhật mức lương mới không làm thay đổi bảng lương của các kỳ đã khóa trong quá khứ.
   - [ ] Test phát hiện F01: Kiểm tra cấu hình EF Core và metadata migration cho cột `Hwid` trong bảng `ChamCong`.
4. **CI tối thiểu và bằng chứng:** Thiết lập script build tự động và lưu log kết quả kiểm thử làm evidence; không tự ý sửa code sản phẩm hoặc chuyển sang T02 trong phạm vi T01.
