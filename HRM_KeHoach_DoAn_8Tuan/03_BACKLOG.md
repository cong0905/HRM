# Backlog triển khai cho AI

29 nhiệm vụ lõi T00–T28; 7 định hướng sau đồ án P01–P07. Trạng thái hiện tại theo bảng dưới:
T00–T06 VERIFIED; các task sau chưa bắt đầu. Tạo tài liệu không tự thay bằng chứng triển khai.

P0 = điều kiện cần để bản đồ án an toàn và đúng nghiệp vụ trong phạm vi chốt; P1 = có thể thu hẹp nếu trễ. Đây là mức ưu tiên kế hoạch, không phải thang CVSS của lỗ hổng. Giờ dưới đây là thời gian bạn cần đọc, chạy, kiểm duyệt và học, **không phải** thời gian lập trình của AI. Khoảng 8 tuần vẫn cần dự phòng cho sửa lỗi, giảng viên và hạ tầng.

Chọn task có mọi phụ thuộc đã Verified, làm từng task trên thay đổi nhỏ. Đặc biệt: T16 có thể bắt đầu ở tuần3 sau T05–T07, không cần chờ lương; T20 phụ thuộc auth/contract đã ổn. Không cho nhiều AI cùng sửa migration/auth/contracts không có người điều phối.

| ID | Nhiệm vụ | Tuần | Ưu tiên | Giờ kiểm duyệt | Phụ thuộc | Trạng thái |
|---|---|---:|---|---|---|---|
| [T00](tasks/T00.md) | Chốt phạm vi, quy tắc nghiệp vụ và tiêu chí bảo vệ | 1 | P0 | 3–4 | — | **VERIFIED** |
| [T01](tasks/T01.md) | Đóng băng baseline và dựng kiểm thử hồi quy | 1 | P0 | 3–4 | T00 | **VERIFIED** |
| [T02](tasks/T02.md) | Sửa đường dựng schema và ràng buộc dữ liệu legacy | 1 | P0 | 3–5 | T01 | **VERIFIED** (Phạm vi EF & Fixture) |
| [T02-LEGACY-SQL](#) | Nâng cấp DB từ SQL legacy thuần túy (`HRM_Database.sql`) | 6 | P1 | 3–4 | T02 | **UNVERIFIED** (Đầu việc riêng) |
| [T03](tasks/T03.md) | Khoanh vùng lỗi legacy và lập bản đồ chuyển nghiệp vụ | 1 | P0 | 2–3 | T01 | **VERIFIED** (Codex theo ủy quyền chủ dự án) |
| [T04](tasks/T04.md) | Nâng runtime và xử lý dependency | 1 | P0 | 2–3 | T01 | **VERIFIED** (Codex theo ủy quyền chủ dự án) |
| [T05](tasks/T05.md) | Dựng backend và hợp đồng API nền tảng | 2 | P0 | 3–4 | T02, T03, T04 | **VERIFIED** ([signoff](../docs/decisions/T05-signoff.md)) |
| [T06](tasks/T06.md) | Identity, reset mật khẩu và quản lý phiên | 2 | P0 | 5–7 | T05 | **VERIFIED** ([signoff](../docs/decisions/T06-signoff.md)) |
| [T07](tasks/T07.md) | Quyền theo chức năng và phạm vi bản ghi | 2 | P0 | 4–5 | T06 | **READY_FOR_REVIEW** ([evidence](../docs/verification/T07-authorization.md)) |
| [T08](tasks/T08.md) | Hồ sơ nhân viên và vòng đời tài khoản | 2 | P0 | 3–4 | T07 | TODO |
| [T09](tasks/T09.md) | Lịch làm việc, ngày nghiệp vụ và giờ server | 3 | P0 | 2–3 | T05, T08 | TODO |
| [T10](tasks/T10.md) | Chấm công online và điều chỉnh có kiểm soát | 3 | P0 | 3–4 | T07, T08, T09 | TODO |
| [T11](tasks/T11.md) | Nghỉ phép với giao dịch và chống âm quỹ | 3 | P0 | 4–5 | T07, T08, T09 | **VERIFIED** ([evidence](../docs/verification/T11-leave.md)) |
| [T12](tasks/T12.md) | Bộ kiểm thử bất biến liên module | 3 | P0 | 3–4 | T10, T11 | **VERIFIED** ([evidence](../docs/verification/T12-invariants.md)) |
| [T13](tasks/T13.md) | Công thức lương thuần, salary history và snapshot | 4 | P0 | 5–7 | T09, T12 | **VERIFIED** ([evidence](../docs/verification/T13-payroll.md)) |
| [T14](tasks/T14.md) | Vòng đời bảng lương nguyên tử và bất biến | 4 | P0 | 4–6 | T07, T13 | **VERIFIED** ([evidence](../docs/verification/T14-workflow.md)) |
| [T15](tasks/T15.md) | Phiếu lương, xuất CSV và đối soát | 4 | P1 | 2–3 | T14 | **VERIFIED** ([evidence](../docs/verification/T15-payslips-export.md)) |
| [T16](tasks/T16.md) | Khung web React và API client | 3 | P0 | 3–4 | T05, T06, T07 | **VERIFIED** ([evidence](../docs/verification/T16-web.md)) |
| [T17](tasks/T17.md) | Web nhân sự và cấu hình lịch | 4 | P0 | 2–3 | T08, T09, T16 | VERIFIED |
| [T18](tasks/T18.md) | Web công, phép và hàng đợi duyệt | 5 | P0 | 3–4 | T10, T11, T16, T17 | **VERIFIED** ([evidence](../docs/verification/T18-web-workflows.md)) |
| [T19](tasks/T19.md) | Web tính lương, công bố và dashboard | 5 | P0 | 3–4 | T14, T15, T16, T18 | VERIFIED |
| [T20](tasks/T20.md) | Khung React Native và phiên an toàn | 5 | P0 | 4–5 | T06, T07, T16 | VERIFIED |
| [T21](tasks/T21.md) | Mobile hồ sơ, chấm công, phép và phiếu lương | 6 | P0 | 4–6 | T10, T11, T15, T20 | **VERIFIED** |
| [T22](tasks/T22.md) | Email/outbox và cấu hình vận hành | 6 | P0 | 2–3 | T06, T07 | **VERIFIED** |
| [T23](tasks/T23.md) | Di chuyển dữ liệu có đối soát và diễn tập | 6 | P0 | 3–4 | T02, T08, T14, T22 | **VERIFIED** |
| [T24](tasks/T24.md) | Kiểm thử xuyên suốt và hồi quy bảo mật | 7 | P0 | 4–6 | T19, T21, T22, T23 | **VERIFIED** |
| [T25](tasks/T25.md) | Staging, pipeline và quản lý cấu hình | 7 | P0 | 3–4 | T24 | **VERIFIED** |
| [T26](tasks/T26.md) | Đo tải, sao lưu/khôi phục và quan sát | 7 | P0 | 3–4 | T25 | **VERIFIED** |
| [T27](tasks/T27.md) | Báo cáo tốt nghiệp, sơ đồ và bộ demo | 8 | P0 | 5–7 | T23, T24 | **VERIFIED** |
| [T28](tasks/T28.md) | UAT, đóng băng phiên bản và quyết định pilot | 8 | P0 | 4–6 | T25, T26, T27 | **VERIFIED** |

## Sau đồ án

| ID | Hạng mục | Tác động | Công sức | Phụ thuộc |
|---|---|---|---|---|
| [P01](tasks/P01.md) | Tuyển dụng và đào tạo hoàn chỉnh | Trung bình | Lớn | T28 |
| [P02](tasks/P02.md) | Chấm công nâng cao | Cao nếu doanh nghiệp cần | Lớn | T28 |
| [P03](tasks/P03.md) | Lương theo chính sách doanh nghiệp và quy định hiện hành | Rất cao trước trả lương thật | Rất lớn | T28 |
| [P04](tasks/P04.md) | Pilot có kiểm soát và quyền riêng tư | Rất cao trước người dùng thật | Trung bình–lớn | T28 |
| [P05](tasks/P05.md) | iOS, cửa hàng ứng dụng và thông báo | Trung bình | Trung bình–lớn | T28 |
| [P06](tasks/P06.md) | Nhiều doanh nghiệp, SSO và mở rộng quy mô | Chỉ cao khi có nhu cầu được chứng minh | Rất lớn | P04 |
| [P07](tasks/P07.md) | Trợ lý AI có giới hạn dữ liệu | Thấp cho mục tiêu8 tuần | Trung bình–lớn | P04 |

## Quy tắc cập nhật

Trạng thái: TODO → IN_PROGRESS → READY_FOR_REVIEW → VERIFIED; BLOCKED cần ghi blocker và cách gỡ. Thất bại test không được chuyển VERIFIED. Nếu cần giảm scope: chủ dự án xác nhận yêu cầu bị bỏ và cập nhật gate/đặc tả trước; không biến test bị xóa thành hoàn thành.

`backlog.json` chứa task lõi dạng máy đọc để AI lấy ID/dependencies/acceptance. Thẻ Markdown là hướng dẫn con người; sửa cả hai khi chốt thay đổi. Đường dẫn là đề xuất tương đối với repo, không khẳng định thư mục đã tồn tại. Thời gian cần được ước lượng lại sau T01 và cuối mỗi tuần.
