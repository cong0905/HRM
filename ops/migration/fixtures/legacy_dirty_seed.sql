-- ============================================================
-- LEGACY DIRTY SEED DATA (VALIDATION GATE FIXTURE)
-- Contains intentional duplicates, orphans, and invalid values
-- to verify dry-run detection.
-- ============================================================

SET NOCOUNT ON;

-- 1. Valid core departments and positions
SET IDENTITY_INSERT dbo.ChucVu ON;
INSERT INTO dbo.ChucVu (MaChucVu, TenChucVu, MoTa, CapBac) VALUES
(1, N'Giám đốc', N'Giám đốc', 6),
(2, N'Nhân viên', N'Nhân viên', 1);
SET IDENTITY_INSERT dbo.ChucVu OFF;

SET IDENTITY_INSERT dbo.PhongBan ON;
INSERT INTO dbo.PhongBan (MaPhongBan, TenPhongBan, MoTaChucNang, NgayThanhLap, DiaDiemLamViec, NganSach, TrangThai) VALUES
(1, N'Ban Giám đốc', N'Điều hành', '2020-01-01', N'Tầng 1', 100000000, N'Hoạt động'),
(2, N'Phòng IT', N'Công nghệ', '2020-01-01', N'Tầng 2', 100000000, N'Hoạt động');
SET IDENTITY_INSERT dbo.PhongBan OFF;

-- 2. Employees:
-- ID 1: Valid
-- ID 2: Duplicate CCCD with ID 1
-- ID 3: Duplicate Email with ID 1
-- ID 4: Orphan Department (MaPhongBan = 999)
-- ID 5: Orphan Position (MaChucVu = 999)
-- ID 6: Negative Salary (-10000000.00)
SET IDENTITY_INSERT dbo.NhanVien ON;
INSERT INTO dbo.NhanVien (
    MaNhanVien, HoTen, NgaySinh, GioiTinh, CCCD, DiaChi, SoDienThoai, Email,
    MaPhongBan, MaChucVu, NgayVaoLam, MucLuong, TrangThai
) VALUES
(1, N'Người Hợp Lệ 1', '1990-01-01', N'Nam', '001090111111', N'Hà Nội', '0901111111', 'valid1@example.com', 1, 1, '2020-01-01', 30000000.00, N'Đang làm việc'),
(2, N'Trùng CCCD', '1991-01-01', N'Nữ', '001090111111', N'Hà Nội', '0901111112', 'dup_cccd@example.com', 2, 2, '2020-01-01', 20000000.00, N'Đang làm việc'),
(3, N'Trùng Email', '1992-01-01', N'Nam', '001090222222', N'Hà Nội', '0901111113', 'valid1@example.com', 2, 2, '2020-01-01', 20000000.00, N'Đang làm việc'),
(4, N'Orphan Phòng Ban', '1993-01-01', N'Nam', '001090333333', N'Hà Nội', '0901111114', 'orphan_dept@example.com', 999, 2, '2020-01-01', 20000000.00, N'Đang làm việc'),
(5, N'Orphan Chức Vụ', '1994-01-01', N'Nam', '001090444444', N'Hà Nội', '0901111115', 'orphan_pos@example.com', 2, 999, '2020-01-01', 20000000.00, N'Đang làm việc'),
(6, N'Lương Âm', '1995-01-01', N'Nam', '001090555555', N'Hà Nội', '0901111116', 'neg_salary@example.com', 2, 2, '2020-01-01', -10000000.00, N'Đang làm việc');
SET IDENTITY_INSERT dbo.NhanVien OFF;

-- 3. TaiKhoan:
-- ID 1: Valid (for employee 1)
-- ID 2: Duplicate username 'dup_user' (for employee 2)
-- ID 3: Duplicate username 'DUP_USER' (case clash) (for employee 3)
-- ID 4: Duplicate username 'dup_user ' (whitespace clash) (for employee 4)
-- ID 5: Orphan employee (MaNhanVien = 999)
SET IDENTITY_INSERT dbo.TaiKhoan ON;
INSERT INTO dbo.TaiKhoan (MaTaiKhoan, MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai) VALUES
(1, 1, 'user_valid', 'dummyhash1', N'Admin', N'Hoạt động'),
(2, 2, 'dup_user', 'dummyhash2', N'Nhân viên', N'Hoạt động'),
(3, 3, 'DUP_USER', 'dummyhash3', N'Nhân viên', N'Hoạt động'),
(4, 4, 'dup_user ', 'dummyhash4', N'Nhân viên', N'Hoạt động'),
(5, 999, 'orphan_account', 'dummyhash5', N'Nhân viên', N'Hoạt động');
SET IDENTITY_INSERT dbo.TaiKhoan OFF;

-- 4. ChamCong:
-- ID 1: Valid
-- ID 2: Orphan employee (MaNhanVien = 999)
-- ID 3: CheckOut before CheckIn
SET IDENTITY_INSERT dbo.ChamCong ON;
INSERT INTO dbo.ChamCong (MaChamCong, MaNhanVien, NgayChamCong, GioVao, GioRa, TongGioLam, GioLamThem, HinhThuc, TrangThai) VALUES
(1, 1, '2026-04-01', '08:30:00', '17:30:00', 8.0, 0.0, N'Vân tay', N'Bình thường'),
(2, 999, '2026-04-01', '08:30:00', '17:30:00', 8.0, 0.0, N'Vân tay', N'Bình thường'),
(3, 1, '2026-04-02', '17:30:00', '08:30:00', -9.0, 0.0, N'Vân tay', N'Bình thường');
SET IDENTITY_INSERT dbo.ChamCong OFF;

-- 5. DonNghiPhep:
-- ID 1: Valid
-- ID 2: Orphan employee (MaNhanVien = 999)
-- ID 3: End date before start date
-- ID 4: Cross-year leave (2025-12-28 to 2026-01-03)
SET IDENTITY_INSERT dbo.LoaiNghiPhep ON;
INSERT INTO dbo.LoaiNghiPhep (MaLoaiPhep, TenLoaiPhep, MoTa, CoLuong) VALUES (1, N'Phép năm', N'Phép năm', 1);
SET IDENTITY_INSERT dbo.LoaiNghiPhep OFF;

SET IDENTITY_INSERT dbo.DonNghiPhep ON;
INSERT INTO dbo.DonNghiPhep (
    MaDonPhep, MaNhanVien, MaLoaiPhep, NgayBatDau, NgayKetThuc, SoNgayNghi, LyDo, TrangThai
) VALUES
(1, 1, 1, '2026-04-10', '2026-04-10', 1.0, N'Hợp lệ', N'Đã duyệt'),
(2, 999, 1, '2026-04-10', '2026-04-10', 1.0, N'Orphan employee', N'Chờ duyệt'),
(3, 1, 1, '2026-04-20', '2026-04-15', 5.0, N'End before start', N'Chờ duyệt'),
(4, 1, 1, '2025-12-28', '2026-01-03', 5.0, N'Cross year leave', N'Chờ duyệt');
SET IDENTITY_INSERT dbo.DonNghiPhep OFF;

-- 6. SoNgayPhep:
-- Overdraft: Used > Allocated
INSERT INTO dbo.SoNgayPhep (MaNhanVien, Nam, TongSoNgayPhep, SoNgayDaSuDung, PhepNamCuConLai) VALUES
(1, 2026, 12, 1, 0),
(2, 2026, 12, 25, 0); -- Vi phạm constraint Used <= Allocated

-- 7. BangLuong:
-- ID 1: Valid
-- ID 2: Orphan employee (MaNhanVien = 999)
SET IDENTITY_INSERT dbo.BangLuong ON;
INSERT INTO dbo.BangLuong (
    MaBangLuong, MaNhanVien, Thang, Nam, LuongCoBan, TongPhuCap, SoNgayLamViec, SoGioLamThem, TienLamThem,
    TongThuong, TongPhat, BHXH, BHYT, BHTN, ThueTNCN, TongThuNhap, TongKhauTru, LuongThucNhan, NgayTinhLuong, TrangThai
) VALUES
(1, 1, 4, 2026, 30000000.00, 1000000.00, 22, 0.0, 0.00, 0.00, 0.00, 2400000.00, 450000.00, 300000.00, 1800000.00, 31000000.00, 4950000.00, 26050000.00, '2026-04-30 17:00:00', N'Đã chốt'),
(2, 999, 4, 2026, 20000000.00, 0.00, 22, 0.0, 0.00, 0.00, 0.00, 1600000.00, 300000.00, 200000.00, 500000.00, 20000000.00, 2600000.00, 17400000.00, '2026-04-30 17:00:00', N'Đã chốt');
SET IDENTITY_INSERT dbo.BangLuong OFF;
