-- ============================================================
-- LEGACY CLEAN SEED DATA (REHEARSAL FIXTURE)
-- Safe synthetic data: NO real PII, NO real secrets
-- ============================================================

SET NOCOUNT ON;

-- 1. ChucVu
SET IDENTITY_INSERT dbo.ChucVu ON;
INSERT INTO dbo.ChucVu (MaChucVu, TenChucVu, MoTa, CapBac) VALUES
(1, N'Giám đốc', N'Giám đốc điều hành công ty', 6),
(2, N'Trưởng phòng', N'Trưởng phòng ban', 4),
(3, N'Nhân viên', N'Nhân viên chuyên môn', 1);
SET IDENTITY_INSERT dbo.ChucVu OFF;

-- 2. PhongBan
SET IDENTITY_INSERT dbo.PhongBan ON;
INSERT INTO dbo.PhongBan (MaPhongBan, TenPhongBan, MoTaChucNang, NgayThanhLap, DiaDiemLamViec, NganSach, TrangThai) VALUES
(1, N'Ban Giám đốc', N'Điều hành toàn bộ hoạt động công ty', '2020-01-01', N'Tầng 10', 500000000, N'Hoạt động'),
(2, N'Phòng Nhân sự', N'Quản lý nhân sự, tuyển dụng, đào tạo', '2020-01-01', N'Tầng 5', 200000000, N'Hoạt động'),
(3, N'Phòng Kỹ thuật', N'Phát triển sản phẩm và hệ thống phần mềm', '2020-01-01', N'Tầng 4', 400000000, N'Hoạt động');
SET IDENTITY_INSERT dbo.PhongBan OFF;

-- 3. NhanVien
SET IDENTITY_INSERT dbo.NhanVien ON;
INSERT INTO dbo.NhanVien (
    MaNhanVien, HoTen, NgaySinh, GioiTinh, CCCD, DiaChi, SoDienThoai, Email,
    TinhTrangHonNhan, MaPhongBan, MaChucVu, NgayVaoLam, MucLuong, TrangThai, NgayNghiViec
) VALUES
(1, N'Nguyễn Văn Giám Đốc', '1980-01-15', N'Nam', '001080000001', N'Hà Nội', '0901000001', 'director@example.com', N'Đã kết hôn', 1, 1, '2020-01-01', 50000000.00, N'Đang làm việc', NULL),
(2, N'Trần Thị Trưởng Nhân Sự', '1985-05-20', N'Nữ', '001085000002', N'Hà Nội', '0902000002', 'hr_lead@example.com', N'Đã kết hôn', 2, 2, '2020-02-01', 30000000.00, N'Đang làm việc', NULL),
(3, N'Lê Văn Kỹ Thuật', '1990-10-10', N'Nam', '001090000003', N'Hà Nội', '0903000003', 'tech_staff@example.com', N'Độc thân', 3, 3, '2021-03-01', 20000000.00, N'Đang làm việc', NULL),
(4, N'Phạm Thị Đã Nghỉ', '1992-12-12', N'Nữ', '001092000004', N'Hà Nội', '0904000004', 'resigned@example.com', N'Độc thân', 3, 3, '2021-04-01', 18000000.00, N'Nghỉ việc', '2025-12-31'),
(5, N'Hoàng Văn Thử Việc', '1998-07-07', N'Nam', '001098000005', N'Hà Nội', '0905000005', 'probation@example.com', N'Độc thân', 3, 3, '2026-03-01', 15000000.00, N'Thử việc', NULL);
SET IDENTITY_INSERT dbo.NhanVien OFF;

-- 4. TaiKhoan (Legacy accounts with legacy hashes, roles: Admin, Phòng Nhân sự, Quản lý, Nhân viên)
SET IDENTITY_INSERT dbo.TaiKhoan ON;
INSERT INTO dbo.TaiKhoan (MaTaiKhoan, MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai) VALUES
(1, 1, 'admin_legacy', '$2a$12$e80MvQkK1qQzX7/legacybcryptadminhash', N'Admin', N'Hoạt động'),
(2, 2, 'hr_legacy', '$2a$12$e80MvQkK1qQzX7/legacybcrypthruserhash', N'Phòng Nhân sự', N'Hoạt động'),
(3, 3, 'tech_legacy', '$2a$12$e80MvQkK1qQzX7/legacybcryptstaffhash', N'Nhân viên', N'Hoạt động'),
(4, 4, 'resigned_legacy', '$2a$12$e80MvQkK1qQzX7/legacybcryptresigned', N'Nhân viên', N'Khóa'),
(5, 5, 'probation_legacy', '$2a$12$e80MvQkK1qQzX7/legacybcryptprobation', N'Nhân viên', N'Hoạt động');
SET IDENTITY_INSERT dbo.TaiKhoan OFF;

-- 5. LoaiNghiPhep
SET IDENTITY_INSERT dbo.LoaiNghiPhep ON;
INSERT INTO dbo.LoaiNghiPhep (MaLoaiPhep, TenLoaiPhep, MoTa, CoLuong) VALUES
(1, N'Phép năm', N'Nghỉ phép thường niên', 1),
(2, N'Phép không lương', N'Nghỉ việc riêng không lương', 0);
SET IDENTITY_INSERT dbo.LoaiNghiPhep OFF;

-- 6. SoNgayPhep (Năm 2026)
INSERT INTO dbo.SoNgayPhep (MaNhanVien, Nam, TongSoNgayPhep, SoNgayDaSuDung, PhepNamCuConLai) VALUES
(1, 2026, 12, 0, 0),
(2, 2026, 12, 1, 0),
(3, 2026, 12, 2, 0),
(4, 2026, 12, 0, 0),
(5, 2026, 12, 0, 0);

-- 7. DonNghiPhep
SET IDENTITY_INSERT dbo.DonNghiPhep ON;
INSERT INTO dbo.DonNghiPhep (
    MaDonPhep, MaNhanVien, MaLoaiPhep, NgayBatDau, NgayKetThuc, SoNgayNghi, LyDo, TrangThai, NguoiPheDuyet, NgayPheDuyet
) VALUES
(1, 2, 1, '2026-04-10', '2026-04-10', 1.0, N'Việc gia đình', N'Đã duyệt', 1, '2026-04-09 10:00:00'),
(2, 3, 1, '2026-04-15', '2026-04-16', 2.0, N'Nghỉ việc cá nhân', N'Đã duyệt', 2, '2026-04-14 14:30:00'),
(3, 3, 1, '2026-05-20', '2026-05-20', 1.0, N'Nghỉ phép riêng', N'Chờ duyệt', NULL, NULL);
SET IDENTITY_INSERT dbo.DonNghiPhep OFF;

-- 8. ChamCong (Tháng 4/2026 - giờ local Asia/Ho_Chi_Minh)
SET IDENTITY_INSERT dbo.ChamCong ON;
INSERT INTO dbo.ChamCong (MaChamCong, MaNhanVien, NgayChamCong, GioVao, GioRa, TongGioLam, GioLamThem, HinhThuc, TrangThai) VALUES
(1, 1, '2026-04-01', '08:30:00', '17:30:00', 8.0, 0.0, N'Vân tay', N'Bình thường'),
(2, 2, '2026-04-01', '08:30:00', '17:30:00', 8.0, 0.0, N'Vân tay', N'Bình thường'),
(3, 3, '2026-04-01', '08:25:00', '17:35:00', 8.0, 0.0, N'Vân tay', N'Bình thường'),
(4, 1, '2026-04-02', '08:30:00', '17:30:00', 8.0, 0.0, N'Vân tay', N'Bình thường'),
(5, 2, '2026-04-02', '08:35:00', '17:30:00', 8.0, 0.0, N'Vân tay', N'Đi muộn'),
(6, 3, '2026-04-02', '08:30:00', '18:30:00', 8.0, 1.0, N'Vân tay', N'Bình thường');
SET IDENTITY_INSERT dbo.ChamCong OFF;

-- 9. BangLuong (Kỳ 2026-04 đã chốt)
SET IDENTITY_INSERT dbo.BangLuong ON;
INSERT INTO dbo.BangLuong (
    MaBangLuong, MaNhanVien, Thang, Nam, LuongCoBan, TongPhuCap, SoNgayLamViec, SoGioLamThem, TienLamThem,
    TongThuong, TongPhat, BHXH, BHYT, BHTN, ThueTNCN, TongThuNhap, TongKhauTru, LuongThucNhan, NgayTinhLuong, TrangThai
) VALUES
(1, 1, 4, 2026, 50000000.00, 2000000.00, 22, 0.0, 0.00, 5000000.00, 0.00, 4000000.00, 750000.00, 500000.00, 4500000.00, 57000000.00, 9750000.00, 47250000.00, '2026-04-30 17:00:00', N'Đã chốt'),
(2, 2, 4, 2026, 30000000.00, 1500000.00, 22, 0.0, 0.00, 2000000.00, 0.00, 2400000.00, 450000.00, 300000.00, 1800000.00, 33500000.00, 4950000.00, 28550000.00, '2026-04-30 17:00:00', N'Đã chốt'),
(3, 3, 4, 2026, 20000000.00, 1000000.00, 22, 1.0, 170454.55, 1000000.00, 0.00, 1600000.00, 300000.00, 200000.00, 750000.00, 22170454.55, 2850000.00, 19320454.55, '2026-04-30 17:00:00', N'Đã chốt'),
(4, 5, 4, 2026, 15000000.00,  500000.00, 22, 0.0, 0.00,       0.00, 0.00, 1200000.00, 225000.00, 150000.00, 250000.00, 15500000.00, 1825000.00, 13675000.00, '2026-04-30 17:00:00', N'Đã chốt');
SET IDENTITY_INSERT dbo.BangLuong OFF;
