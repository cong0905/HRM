# Bằng Chứng Thực Thi & Đối Soát Dữ Liệu T02 (Run: HRM_Next_Test_T02_v7)

- **Thời gian thực hiện**: 2026-11-10T22:49:48.360624
- **Máy chủ kiểm thử**: localhost (SQL Server Developer Edition)
- **Tiền tố database test**: `HRM_Next_Test_T02_v7_*`
- **Kết quả tổng thể**: **ĐẠT TOÀN DIỆN (PASS)**
- **Nguyên tắc an toàn**: Toàn bộ database test được tạo mới độc lập; không can thiệp, không đọc/ghi và không thay đổi database sản phẩm `HRM_System`.
- **Trạng thái nhiệm vụ**: **READY_FOR_REVIEW** (Chờ chủ dự án nghiệm thu)

---

## 1. Bảng Tổng Hợp Kết Quả Thực Thi 6 Kịch Bản Kiểm Thử

| Kịch Bản | Database Đích | Lệnh Thực Thi | Exit Code | Lỗi SQL Mong Đợi / Thực Tế | Kiểm Tra Schema & History Sau Chạy | Kết Quả |
| :--- | :--- | :--- | :---: | :--- | :--- | :---: |
| **1. Fresh Install** | `HRM_Next_Test_T02_v7_Fresh` | `dotnet ef database update` | `0` | Không phát hiện lỗi (Thành công) | Unique Index: Có<br>FK MaTaiKhoan: Có<br>Shadow column: Đã drop<br>Cột Hwid: Có<br>History: Khớp (7 migration) | **PASS** |
| **2. Upgrade Valid** | `HRM_Next_Test_T02_v7_UpgradeValid` | `dotnet ef database update` | `0` | Không phát hiện lỗi (Thành công) | Áp dụng migration: Exit 0; Đối soát dữ liệu: 0 diffs; History: Khớp (8 migration) | **PASS** |
| **- Idempotency** | `HRM_Next_Test_T02_v7_UpgradeValid` | `dotnet ef database update` (Lần 2) | `0` | Thông báo: 'No migrations were applied' | Đối soát trước/sau lần 2: 0 lệch dữ liệu; Schema checks: Khớp; Schema metadata: Khớp | **PASS** |
| **3. Block ShadowOnly** | `HRM_Next_Test_T02_v7_Block_ShadowOnly` | `dotnet ef database update` | `1` | Mong đợi: 51001<br>Thực tế: 51001 (Đã báo mục tiêu: 20) | Bảo toàn sau thất bại: Đạt<br>Dữ liệu: 0 lệch dữ liệu<br>Schema metadata: Khớp<br>History: Khớp | **PASS** |
| **4. Block DifferingIds** | `HRM_Next_Test_T02_v7_Block_DifferingIds` | `dotnet ef database update` | `1` | Mong đợi: 51002<br>Thực tế: 51002 (Đã báo mục tiêu: 30) | Bảo toàn sau thất bại: Đạt<br>Dữ liệu: 0 lệch dữ liệu<br>Schema metadata: Khớp<br>History: Khớp | **PASS** |
| **5. Block OrphanToken** | `HRM_Next_Test_T02_v7_Block_OrphanToken` | `dotnet ef database update` | `1` | Mong đợi: 51003<br>Thực tế: 51003 (Đã báo mục tiêu: 40) | Bảo toàn sau thất bại: Đạt<br>Dữ liệu: 0 lệch dữ liệu<br>Schema metadata: Khớp<br>History: Khớp | **PASS** |
| **6. Block DuplicateUser**| `HRM_Next_Test_T02_v7_Block_DuplicateUser` | `dotnet ef database update` | `1` | Mong đợi: 51004<br>Thực tế: 51004 (Đã báo mục tiêu: duplicate_user) | Bảo toàn sau thất bại: Đạt<br>Dữ liệu: 0 lệch dữ liệu<br>Schema metadata: Khớp<br>History: Khớp | **PASS** |

---

## 2. Đối Soát Tự Động Từng Bản Ghi Theo Khóa Chính (Upgrade Valid)

Phép so sánh tự động đối soát từng cột của từng dòng dữ liệu giữa **TRƯỚC MIGRATION** và **SAU MIGRATION** trên database `HRM_Next_Test_T02_v7_UpgradeValid`.
Bao gồm kiểm tra chặt chẽ `PhongBan` và các trường `NgayTao` và `NgayCapNhat` từng bị ảnh hưởng bởi lỗi `UpdateData` trước đây (đối soát tiền tệ qua Decimal chính xác):

| Bảng Kiểm Tra | Khóa Chính (PK) | Các Cột Được Kiểm Tra | Tổng Bản Ghi | Số Bản Ghi Lệch | Danh Sách ID Lệch | Trạng Thái Thực Tế |
| :--- | :---: | :--- | :---: | :---: | :---: | :---: |
| **`dbo.PhongBan`** | `MaPhongBan` | `TenPhongBan`, `MoTaChucNang`, `NgayTao`, `NgayCapNhat` | 6 | 0 | [] | **KHỚP (0 LỆCH)** |
| **`dbo.NhanVien`** | `MaNhanVien` | `HoTen`, `NgaySinh`, `GioiTinh`, `CCCD`, `DiaChi`, `SoDienThoai`, `Email`, `MaPhongBan`, `MaChucVu`, `NgayVaoLam`, `MucLuong`, `TrangThai`, `NgayTao`, `NgayCapNhat` | 10 | 0 | [] | **KHỚP (0 LỆCH)** |
| **`dbo.TaiKhoan`** | `MaTaiKhoan` | `MaNhanVien`, `TenDangNhap`, `VaiTro`, `TrangThai`, `LanDangNhapCuoi`, `NgayTao` *(Mật khẩu được bỏ qua không log)* | 7 | 0 | [] | **KHỚP (0 LỆCH)** |
| **`dbo.BangLuong`** | `MaBangLuong` | `MaNhanVien`, `Thang`, `Nam`, `LuongCoBan`, `TongPhuCap`, `SoNgayLamViec`, `SoGioLamThem`, `TienLamThem`, `TongThuong`, `TongPhat`, `BHXH`, `BHYT`, `BHTN`, `ThueTNCN`, `TongThuNhap`, `TongKhauTru`, `LuongThucNhan`, `NgayTinhLuong`, `TrangThai` | 10 | 0 | [] | **KHỚP (0 LỆCH)** |
| **`dbo.ChamCong`** | `MaChamCong` | `MaNhanVien`, `NgayChamCong`, `GioVao`, `GioRa`, `TongGioLam`, `GioLamThem`, `HinhThuc`, `TrangThai`, `GhiChu`, `Hwid` | 8 | 0 | [] | **KHỚP (0 LỆCH)** |
| **`dbo.PasswordResetTokens`** | `Id` | `MaTaiKhoan`, `Email`, `Purpose`, `CreatedAt`, `ExpiresAt`, `IsUsed` *(Chuỗi token bí mật được bỏ qua không log)* | 1 | 0 | [] | **KHỚP (0 LỆCH)** |

- **Tổng số bản ghi kiểm tra trên toàn bộ 6 bảng**: 42 bản ghi.
- **Tổng số bản ghi phát hiện sai lệch**: **0 bản ghi**.

---

## 3. Tuyên Bố Phạm Vi Hỗ Trợ & Ranh Giới Kỹ Thuật

> [!IMPORTANT]
> **Phạm vi hỗ trợ đã được kiểm chứng**:
> - Các database có chuỗi schema dựng từ Entity Framework Core và migration history tương thích với khảo sát.
> - Các database có cấu trúc cột shadow `TaiKhoanMaTaiKhoan` và thiếu unique index trên `TenDangNhap`.
> - Dữ liệu trong các fixture test (`fixture_simulated_legacy.sql`) là **dữ liệu mô phỏng độc lập**, tuyệt đối **không phải snapshot từ database thật `HRM_System`**.
>
> **Ranh giới chưa kiểm chứng (UNVERIFIED)**:
> - Đường chuyển đổi trực tiếp từ database dựng thuần túy bằng file SQL script legacy (`db/docs/HRM_Database.sql`) **chưa được thực hiện và chưa được kiểm chứng**, vì script SQL legacy hoàn toàn thiếu các bảng/cột phát sinh sau này (`PasswordResetTokens`, `Hwid` trên `ChamCong`, kiểu dữ liệu khác biệt).
> - Fixture mô phỏng thêm dòng migration history trong database test **chỉ là mô phỏng lịch sử EF**, tuyệt đối **không được coi là kiểm thử thay thế** cho đường nâng cấp từ database tạo thuần túy bằng SQL legacy.

---

## 4. Ghi Chú Kỹ Thuật Về Phương Thức `Down()` Của Migration

> [!WARNING]
> Phương thức `Down()` của migration `20261110141626_FixLegacySchemaAndConstraints.cs`:
> - Khi rollback, `Down()` chỉ phục hồi **cấu trúc cột** (`migrationBuilder.AddColumn<int>("TaiKhoanMaTaiKhoan", ...)`), tái lập Foreign Key và xóa Unique Index trên `TenDangNhap`.
> - `Down()` **KHÔNG TỰ PHỤC HỒI các giá trị shadow data** đã bị xóa khi cột bị DROP ở phương thức `Up()`. Toàn bộ giá trị trong cột `TaiKhoanMaTaiKhoan` sau khi rollback bằng `Down()` sẽ nhận giá trị `NULL`.
> - Tuyệt đối **không được mô tả `Down()` là cơ chế rollback dữ liệu đầy đủ**. Nếu muốn khôi phục dữ liệu shadow, bắt buộc phải restore từ bản backup database trước khi chạy migration.
