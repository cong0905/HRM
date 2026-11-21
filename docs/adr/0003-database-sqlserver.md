# ADR-0003: Quản lý Cơ sở dữ liệu SQL Server và Phân tách Database mới

> **Trạng thái:** `PROPOSED` (Đề xuất chờ Chủ dự án xác nhận)  
> **Ngày quyết định:** 09/11/2026  
> **Tác giả:** AI Coding Assistant / Nhóm phát triển HRM

---

## 1. Bối cảnh và Vấn đề (Context & Problem Statement)

Dự án hiện tại lưu trữ dữ liệu trên Microsoft SQL Server thông qua Entity Framework Core 8 (Code-First). Báo cáo kiểm tra hiện trạng cho thấy một số rủi ro kỹ thuật:

- Tồn tại sự không nhất quán giữa mã nguồn Migration và tệp tài liệu `HRM_Database.sql` (F19).
- Migration `20260505060254_AddChamCongHwid.cs` thiếu metadata tạo cột `Hwid` (F01).
- Thiếu ràng buộc Unique trên `TenDangNhap` và quan hệ khóa ngoại shadow property trên bảng `PasswordResetTokens` (F08).
- Có nguy cơ việc chạy thử nghiệm phiên bản mới sẽ làm hỏng hoặc ghi đè dữ liệu trên cơ sở dữ liệu `HRM_System` cũ đang phục vụ cho bản WinForms tham chiếu.

---

## 2. Các Yếu tố Thúc đẩy Quyết định (Decision Drivers)

1. **Bảo toàn dữ liệu cũ làm bản đối chiếu (Baseline):** Bản WinForms cần được giữ nguyên vẹn để làm cơ sở đối chiếu số liệu nghiệp vụ và demo so sánh.
2. **Khởi tạo cơ sở dữ liệu sạch và tự động (Repeatable Migrations):** Đồ án tốt nghiệp phải đảm bảo bất kỳ giám khảo hay người dùng nào clone mã nguồn về đều có thể tạo cơ sở dữ liệu mới 100% thành công bằng lệnh migration chuẩn.
3. **Tính toàn vẹn dữ liệu từ tầng Database (Constraints & Indexes):** Ràng buộc duy nhất, khóa ngoại và concurrency token phải được SQL Server bảo vệ chặt chẽ, không chỉ dựa vào logic kiểm tra ở tầng ứng dụng.

---

## 3. Các Phương án đã Cân nhắc (Considered Options)

1. **Phương án A: Dùng chung Database `HRM_System` cũ với ứng dụng WinForms**
   - _Rủi ro:_ Xung đột schema, WinForms và Web API cùng ghi vào một DB gây sai lệch dữ liệu và mất tính toàn vẹn.
2. **Phương án B: Chuyển sang PostgreSQL hoặc MySQL**
   - _Rủi ro:_ Phải cấu hình lại toàn bộ kiểu dữ liệu C# / EF Core, thay đổi dialect SQL, tăng rủi ro tương thích trong thời gian 8 tuần.
3. **Phương án C (Được chọn): Tiếp tục sử dụng SQL Server, nhưng phân tách Database mới hoàn toàn (`HRM_Next_Dev`)**
   - Sửa toàn bộ chuỗi Migration cũ, tạo bộ Migration mới hoàn chỉnh, chuẩn hóa kiểu dữ liệu và ràng buộc.

---

## 4. Quyết định (Decision Outcome)

**Lựa chọn Phương án C:**

- Giữ nguyên hệ quản trị cơ sở dữ liệu là **Microsoft SQL Server** (hỗ trợ SQL Server LocalDB, SQL Server Express hoặc SQL Server Developer Edition).
- **Phân tách hoàn toàn Database mới:** Phiên bản mới sẽ kết nối vào cơ sở dữ liệu riêng biệt có tên là `HRM_Next_Dev` (môi trường phát triển) và `HRM_Next_Test` (môi trường kiểm thử tự động). Cơ sở dữ liệu cũ `HRM_System` được giữ nguyên ở trạng thái Read-only làm tài liệu đối chiếu.
- **Sửa dứt điểm lỗi Migration (Task T02):**
  - Bổ sung cấu hình Fluent API tạo cột `Hwid` chuẩn xác trong bảng `ChamCong`.
  - Thêm `IsUnique` cho cột `NormalizedUserName` trong bảng tài khoản `AspNetUsers`.
  - Chuẩn hóa khóa ngoại `TaiKhoanId` trong bảng `PasswordResetTokens`.
  - Thêm cột kiểm tra đồng thời `RowVersion` (`byte[] [Timestamp]`) trên các bảng nhạy cảm: `LeaveBalance`, `LeaveRequest`, `PayrollPeriod`, `PayrollItem`.

---

## 5. Đánh đổi và Hệ quả (Trade-offs & Consequences)

### Điểm tích cực (Pros):

- Độc lập hoàn toàn với lỗi của quá khứ; không lo ngại việc WinForms làm sai lệch dữ liệu của phiên bản Web/Mobile mới.
- Dễ dàng xóa đi tạo lại (Drop & Re-create) trong quá trình phát triển và kiểm thử tự động CI/CD.
- Khắc phục triệt để các rủi ro bảo mật F01, F08, F19.

### Điểm hạn chế và Thách thức (Cons & Mitigations):

- Cần có kịch bản nạp dữ liệu mẫu (Seeding Data) chuẩn chỉnh cho DB mới.
  - _Giải pháp:_ Xây dựng dữ liệu Seeding chuyên dụng (gồm tài khoản Admin, HR, Kế toán, Nhân viên mẫu cùng 10 kịch bản test) tại Task T02 và T08.
