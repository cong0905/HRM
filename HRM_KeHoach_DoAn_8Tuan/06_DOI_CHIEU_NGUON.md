# Đối chiếu phát hiện với mã nguồn

Snapshot cố định: **8b1236573f99c28f4cd486831cb30635a01bd061**. Link GitHub ghim commit để vẫn đọc đúng bằng chứng khi main thay đổi. Dòng dẫn tới vùng liên quan; đọc cả phương thức và đường gọi trước khi sửa. Mức tác động là đánh giá kỹ thuật, không phải kết luận pentest.

## Danh mục phát hiện

### F01 — Migration Hwid thiếu metadata và không tạo cột cần dùng

- Tác động: **Cao**.
- File: [db/src/HRM.DAL/Migrations/20260505060254_AddChamCongHwid.cs:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Migrations/20260505060254_AddChamCongHwid.cs#L1), [db/src/HRM.DAL/Configurations/ChamCongConfiguration.cs:18](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Configurations/ChamCongConfiguration.cs#L18).
- Bằng chứng: Offline EF nhận6/7 migration và script không có cột Hwid. DB đang chạy có thể đã được vá tay; chưa kết nối để khẳng định schema của từng máy.
- Hệ quả và hướng sửa: DB mới có thể không đáp ứng model chấm công.
- Task: T02.

### F02 — Read bảng lương ghi DB và đồng bộ mức mới vào lịch sử

- Tác động: **Rất cao**.
- File: [db/src/HRM.BLL/Services/BangLuongService.cs:36](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L36), [db/src/HRM.BLL/Services/BangLuongService.cs:70](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L70), [db/src/HRM.BLL/Services/NhanVienService.cs:94](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/NhanVienService.cs#L94).
- Bằng chứng: Đọc đường gọi đồng bộ; chưa có thử trên DB thật.
- Hệ quả và hướng sửa: Mở danh sách hoặc sửa nhân viên có thể đổi số liệu quá khứ; cần snapshot/effective dates và query không mutation.
- Task: T13,T14.

### F03 — Tính lại xóa kỳ cũ trước, chưa có transaction bao toàn bộ

- Tác động: **Rất cao**.
- File: [db/src/HRM.BLL/Services/BangLuongService.cs:169](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L169), [db/src/HRM.DAL/Repositories/Repository.cs:33](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Repositories/Repository.cs#L33).
- Bằng chứng: Static: delete và nhiều SaveChanges tách rời.
- Hệ quả và hướng sửa: Lỗi giữa chừng có nguy cơ kỳ mất/thiếu dữ liệu; thay bằng run version và commit nguyên tử.
- Task: T05,T14.

### F04 — Chưa có hiệu suất vẫn bị giảm10%

- Tác động: **Cao**.
- File: [db/src/HRM.BLL/Services/HieuSuatService.cs:481](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/HieuSuatService.cs#L481), [db/src/HRM.BLL/Services/BangLuongService.cs:285](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L285).
- Bằng chứng: Harness gọi helper với null nhận -0.10.
- Hệ quả và hướng sửa: Không có đánh giá không tương đương hiệu suất0; mặc định trung tính và policy minh bạch.
- Task: T13.

### F05 — Duyệt phép không kiểm lại quỹ và ghi tách bước

- Tác động: **Rất cao**.
- File: [db/src/HRM.BLL/Services/DonNghiPhepService.cs:74](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/DonNghiPhepService.cs#L74), [db/src/HRM.BLL/Services/DonNghiPhepService.cs:121](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/DonNghiPhepService.cs#L121).
- Bằng chứng: Harness fake repo: còn1, duyệt3, còn-2; race SQL chưa chạy.
- Hệ quả và hướng sửa: Duyệt vượt quỹ hoặc lỗi giữa status/balance; recheck trong transaction và concurrency.
- Task: T11,T12.

### F06 — Reset không kiểm Purpose, OTP ngẫu nhiên thường và ghi password/token tách

- Tác động: **Rất cao**.
- File: [db/src/HRM.BLL/Services/AuthService.cs:57](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/AuthService.cs#L57), [db/src/HRM.BLL/Services/AuthService.cs:102](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/AuthService.cs#L102), [db/src/HRM.DAL/Repositories/PasswordResetTokenRepository.cs:18](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Repositories/PasswordResetTokenRepository.cs#L18).
- Bằng chứng: Harness token VerifyEmail được service chấp nhận reset; không phải PoC tấn công qua UI/network.
- Hệ quả và hướng sửa: Lẫn mục đích token, replay/race và brute force cần xử lý; dùng Identity lifecycle thay thiết kế này.
- Task: T06.

### F07 — NullEmailSender báo hoàn tất dù không gửi

- Tác động: **Cao**.
- File: [db/src/HRM.Common/Helpers/NullEmailSender.cs:9](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.Common/Helpers/NullEmailSender.cs#L9), [db/src/HRM.GUI/Forms/Auth/frmQuenMatKhau.cs:45](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Auth/frmQuenMatKhau.cs#L45), [db/src/HRM.BLL/Services/AuthService.cs:80](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/AuthService.cs#L80).
- Bằng chứng: Harness: no SMTP reset trả null; GUI diễn giải như đã gửi.
- Hệ quả và hướng sửa: Người dùng không nhận được mail nhưng UI báo thành công; thời hạn mail1h lệch token15phút, URL placeholder.
- Task: T06,T22.

### F08 — Thiếu unique username và FK token dùng shadow property

- Tác động: **Cao**.
- File: [db/src/HRM.DAL/Configurations/ChinhSachHeThongConfiguration.cs:14](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Configurations/ChinhSachHeThongConfiguration.cs#L14), [db/src/HRM.DAL/Migrations/20260517153408_AddPasswordResetTokens.cs:15](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Migrations/20260517153408_AddPasswordResetTokens.cs#L15), [db/src/HRM.Domain/Entities/PasswordResetToken.cs:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.Domain/Entities/PasswordResetToken.cs#L1).
- Bằng chứng: EF metadata không có username unique; token FK TaiKhoanMaTaiKhoan thay MaTaiKhoan.
- Hệ quả và hướng sửa: Duplicate account và tham chiếu không được DB ràng buộc như code kỳ vọng; cần preflight dữ liệu.
- Task: T02,T06.

### F09 — Phân quyền chủ yếu tại GUI, service chưa là biên bảo mật

- Tác động: **Rất cao khi mở API**.
- File: [db/src/HRM.GUI/Forms/Main/frmMain.cs:122](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Main/frmMain.cs#L122), [db/src/HRM.BLL/Services/ChamCongService.cs:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/ChamCongService.cs#L1), [db/src/HRM.BLL/Services/BangLuongService.cs:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L1).
- Bằng chứng: Đọc menu role và service signatures; chưa có API hiện tại để pentest HTTP.
- Hệ quả và hướng sửa: Bọc service hiện tại bằng controller nguyên trạng sẽ tin IDs/bool do client đưa; cần server policies và record scope.
- Task: T07.

### F10 — Update hồ sơ làm rơi địa chỉ/tình trạng hôn nhân

- Tác động: **Cao**.
- File: [db/src/HRM.GUI/Forms/Main/NhanVien/frmSuaNhanVien.cs:86](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Main/NhanVien/frmSuaNhanVien.cs#L86), [db/src/HRM.BLL/Services/NhanVienService.cs:94](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/NhanVienService.cs#L94).
- Bằng chứng: Form không gán các trường nhưng service overwrite từ DTO.
- Hệ quả và hướng sửa: Sửa trường khác có thể xóa thông tin; request DTO riêng và partial-update semantics.
- Task: T08,T17.

### F11 — NoTracking mặc định nhưng mutation không attach/update

- Tác động: **Cao**.
- File: [db/src/HRM.GUI/Program.cs:58](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Program.cs#L58), [db/src/HRM.BLL/Services/TinTuyenDungService.cs:47](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/TinTuyenDungService.cs#L47), [db/src/HRM.BLL/Services/BangLuongService.cs:125](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L125).
- Bằng chứng: Kiểm tracking độc lập cho detached posting không phát hiện changes; lương đọc tĩnh.
- Hệ quả và hướng sửa: Có đường SaveChanges không lưu sửa entity; quy định tracking và unit-of-work rõ, không kết luận mọi update đều lỗi.
- Task: T05,P01.

### F12 — Giờ máy/IP/HWID không tạo nguồn chấm công đáng tin

- Tác động: **Cao trước dùng thật**.
- File: [db/src/HRM.BLL/Services/ChamCongService.cs:25](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/ChamCongService.cs#L25), [db/src/HRM.BLL/Services/ChamCongService.cs:58](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/ChamCongService.cs#L58).
- Bằng chứng: Static: DateTime.Now/Today, IP local whitelist và cập nhật HWID checkout.
- Hệ quả và hướng sửa: Client desktop truy cập DB trực tiếp; thay giờ/môi trường chưa được thử nhưng trust boundary yếu; server time trước, chống gian lận nâng cao sau.
- Task: T09,T10,P02.

### F13 — Mật khẩu DOB, đổi mật khẩu và offboarding không đồng nhất

- Tác động: **Cao**.
- File: [db/src/HRM.BLL/Services/NhanVienService.cs:48](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/NhanVienService.cs#L48), [db/src/HRM.BLL/Services/NhanVienService.cs:121](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/NhanVienService.cs#L121), [db/src/HRM.GUI/Forms/Main/frmDoiMatKhau.cs:90](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Main/frmDoiMatKhau.cs#L90), [db/src/HRM.BLL/Services/TaiKhoanService.cs:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/TaiKhoanService.cs#L1).
- Bằng chứng: Đọc code; harness default password không qua PasswordPolicy. AuthService có hàm kiểm mật khẩu cũ nhưng form đi đường TaiKhoanService khác.
- Hệ quả và hướng sửa: Không nên kết luận tất cả đường đổi đều giống nhau; hợp nhất Identity và khóa account theo lifecycle.
- Task: T06,T08.

### F14 — System.Text.Json8.0.0 bắc cầu bị cảnh báo

- Tác động: **Cao theo advisory**.
- File: [db/src/HRM.BLL/HRM.BLL.csproj:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/HRM.BLL.csproj#L1), [db/src/HRM.DAL/HRM.DAL.csproj:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/HRM.DAL.csproj#L1), [db/src/HRM.GUI/HRM.GUI.csproj:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/HRM.GUI.csproj#L1).
- Bằng chứng: Scan resolved dependency hiện tại có2 High; chi tiết evidence JSON.
- Hệ quả và hướng sửa: Cần nâng và scan lại, không suy từ advisory rằng mọi đường chạy đều khai thác được.
- Task: T04.

### F15 — DB/DI lifetime và quyền truy cập DB ở client

- Tác động: **Cao**.
- File: [db/src/HRM.GUI/Program.cs:16](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Program.cs#L16), [db/src/HRM.GUI/Program.cs:34](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Program.cs#L34), [db/src/HRM.GUI/Program.cs:50](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Program.cs#L50).
- Bằng chứng: Root ServiceProvider giữ services, startup auto migrate; scope riêng chỉ thấy cho migration.
- Hệ quả và hướng sửa: Scoped registration không tự tạo scope cho từng form; cần quản lý lifetime. Web/mobile chuyển DB access về API.
- Task: T05,T25.

### F16 — Lịch công, phép và lương chưa thống nhất

- Tác động: **Cao**.
- File: [db/src/HRM.BLL/Services/BangLuongService.cs:285](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L285), [db/src/HRM.BLL/Services/BangLuongService.cs:347](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/BangLuongService.cs#L347), [db/src/HRM.BLL/Services/HieuSuatService.cs:548](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/HieuSuatService.cs#L548), [db/src/HRM.BLL/Services/DonNghiPhepService.cs:74](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/DonNghiPhepService.cs#L74).
- Bằng chứng: Static: denominator26, record-day count, weekdays ở hiệu suất, calendar days ở phép.
- Hệ quả và hướng sửa: Các module có thể cho kết quả khác nhau từ cùng một kỳ; tạo calendar service/policy version và fixtures chung.
- Task: T09,T11,T13.

### F17 — Tuyển dụng ghi nhiều bước và tự tuyển theo vòng

- Tác động: **Trung bình–cao**.
- File: [db/src/HRM.BLL/Services/PhongVanService.cs:19](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/PhongVanService.cs#L19), [db/src/HRM.BLL/Services/PhongVanService.cs:55](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/PhongVanService.cs#L55), [db/src/HRM.GUI/Forms/Main/UngVien/ucUngVien.cs:251](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Main/UngVien/ucUngVien.cs#L251).
- Bằng chứng: Static: nhiều saves; vòng>=3 chuyển trạng thái; tạo nhân viên nằm ở GUI.
- Hệ quả và hướng sửa: Quyết định tuyển dụng cần explicit approval; chuyển candidate phải atomic/idempotent.
- Task: P01.

### F18 — Chatbot gửi thông tin nhân viên ra dịch vụ ngoài từ client

- Tác động: **Cao trước dùng PII thật**.
- File: [db/src/HRM.GUI/Forms/Chat/frmChatBot.cs:24](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Chat/frmChatBot.cs#L24), [db/src/HRM.BLL/Services/GeminiService.cs:24](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/GeminiService.cs#L24).
- Bằng chứng: Đọc xây prompt gồm tên/role/phòng ban/phép và cách gọi; chưa gọi Gemini thật.
- Hệ quả và hướng sửa: Đưa key về server, giảm PII và rà soát quyền/retention nếu mở lại; không cần module này để bảo vệ core.
- Task: P07.

### F19 — SQL tài liệu và EF chưa là một nguồn schema thống nhất

- Tác động: **Cao**.
- File: [db/docs/HRM_Database.sql:493](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/docs/HRM_Database.sql#L493), [db/fix_db_history.sql:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/fix_db_history.sql#L1), [db/src/HRM.DAL/Context/HrmDbContext.cs:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Context/HrmDbContext.cs#L1).
- Bằng chứng: SQL có constraint khác EF; script history có thao tác ghi lịch sử.
- Hệ quả và hướng sửa: Người dựng DB theo tài liệu khác nhau có schema khác; không chữa history trước khi biết schema thực.
- Task: T02,T23.

### F20 — Ranh giới lớp và quản lý lỗi chưa đồng nhất

- Tác động: **Trung bình**.
- File: [db/src/HRM.GUI/Forms/Main/NhanVien/frmThemNhanVien.cs:9](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.GUI/Forms/Main/NhanVien/frmThemNhanVien.cs#L9), [db/src/HRM.BLL/Services/NhanVienService.cs:48](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/NhanVienService.cs#L48), [db/src/HRM.DAL/Repositories/Repository.cs:33](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.DAL/Repositories/Repository.cs#L33).
- Bằng chứng: GUI inject repository trực tiếp ở một số chỗ, catch nuốt lỗi tạo account, repository tự SaveChanges.
- Hệ quả và hướng sửa: Không chỉ đổi tên folder thành Clean Architecture; cần use case boundary, errors có mã và transaction do application kiểm soát.
- Task: T03,T05,T08.

### F21 — Chưa có bộ test/pipeline sản phẩm trong tracked repo

- Tác động: **Cao với phát hành**.
- File: [README.md:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/README.md#L1), [db/src/HRM.sln:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.sln#L1), [db/HuongDanChayDuAn.md:1](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/HuongDanChayDuAn.md#L1).
- Bằng chứng: Kiểm inventory tracked files; build đạt và harness ngoài repo không phải suite sẵn có. Actions quan sát được không chứng minh product tests.
- Hệ quả và hướng sửa: Thiếu regression và deployment evidence; thêm CI/E2E/staging/restore trước dùng thật.
- Task: T01,T24,T25,T26.

### F22 — Xóa hiệu suất thiếu cùng kiểm soát khóa như create/update

- Tác động: **Trung bình–cao**.
- File: [db/src/HRM.BLL/Services/HieuSuatService.cs:313](https://github.com/cong0905/HRM/blob/8b1236573f99c28f4cd486831cb30635a01bd061/db/src/HRM.BLL/Services/HieuSuatService.cs#L313).
- Bằng chứng: Static branch review, chưa thực thi trên DB thật.
- Hệ quả và hướng sửa: Nếu hiệu suất ảnh hưởng tiền, deletion phải kiểm period lock và snapshot; MVP tắt liên kết, nếu bật phải bổ sung test xóa.
- Task: T13,T14.

## Bằng chứng đã có và giới hạn

- [Kết quả kiểm tra độc lập](evidence/HRM-ket-qua-kiem-tra.json): EF model/migration và service với fake repository; không có thử SQL Server thật.
- [Kết quả quét thư viện](evidence/HRM-kiem-tra-thu-vien.json): phiên bản resolved và advisory tại lần audit; phải restore/scan lại sau thay đổi.
- Build solution baseline thành công có warning. Không tuyên bố giao diện chạy đúng, SMTP gửi được, Gemini phản hồi thật hoặc schema DB người dùng đã được xác minh.
- Các file graph được tạo để điều hướng code; graph không phải bằng chứng nghiệp vụ hoạt động. Không chỉnh source sản phẩm trong lần lập báo cáo.

## Nguồn kỹ thuật ngoài repository

Đã tra trong lần lập kế hoạch; version/support cần kiểm tra lại khi bắt đầu task liên quan.

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy): .NET8 hết hỗ trợ10/11/2026; chọn .NET10 LTS và patch đang hỗ trợ.
- [ASP.NET Core Identity API](https://learn.microsoft.com/en-gb/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0): cookie/bearer và giới hạn token tích hợp. Token bearer Identity không mặc nhiên là JWT/OIDC.
- [React Native security](https://reactnative.dev/docs/security): AsyncStorage không phải kho giữ bí mật; token cần secure storage.
- [OpenIddict: lựa chọn flow](https://documentation.openiddict.com/guides/choosing-the-right-flow.html): tài liệu tham khảo khi thật sự cần OIDC/SSO, không bắt buộc thêm trong8 tuần.
- [System.Text.Json advisory1](https://github.com/advisories/GHSA-hh2w-p6rv-4g7w) và [advisory2](https://github.com/advisories/GHSA-8g4q-xg66-9fp4): đối chiếu với phiên bản resolved trong evidence, không suy diễn exploitability chỉ từ severity.

Không có công thức lương pháp lý hoặc kết luận tuân thủ dữ liệu cá nhân trong báo cáo. Khi pilot phải rà soát quy định hiện hành và chính sách doanh nghiệp với người chịu trách nhiệm.
