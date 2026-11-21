-- Fixture: Simulated Legacy data matching HRM_System survey metrics
-- Safe mock data: NO real PII, NO real tokens, NO real credentials

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

-- 1. Insert 7th migration to mirror HRM_System migration history
IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20260504161102_AddHwidToChamCong')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20260504161102_AddHwidToChamCong', '8.0.33');
END
GO

-- 2. Insert 9 additional synthetic employees (total 10)
SET IDENTITY_INSERT dbo.NhanVien ON;
GO

INSERT INTO dbo.NhanVien (
    MaNhanVien, HoTen, NgaySinh, GioiTinh, CCCD, DiaChi, SoDienThoai, Email,
    MaPhongBan, MaChucVu, NgayVaoLam, MucLuong, TrangThai, NgayTao, NgayCapNhat
)
SELECT v.MaNhanVien, v.HoTen, v.NgaySinh, v.GioiTinh, v.CCCD, v.DiaChi, v.SoDienThoai, v.Email,
       v.MaPhongBan, v.MaChucVu, v.NgayVaoLam, v.MucLuong, v.TrangThai, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM (VALUES
    (2, N'Nhân viên 2', '1992-02-02', N'Nữ', '000000000002', N'Hà Nội', '0900000002', 'nv2@example.com', 2, 2, '2021-01-01', 20000000.00, N'Đang làm việc'),
    (3, N'Nhân viên 3', '1993-03-03', N'Nam', '000000000003', N'Hà Nội', '0900000003', 'nv3@example.com', 2, 3, '2021-02-01', 18000000.00, N'Đang làm việc'),
    (4, N'Nhân viên 4', '1994-04-04', N'Nữ', '000000000004', N'Hà Nội', '0900000004', 'nv4@example.com', 3, 2, '2021-03-01', 22000000.00, N'Đang làm việc'),
    (5, N'Nhân viên 5', '1995-05-05', N'Nam', '000000000005', N'Hà Nội', '0900000005', 'nv5@example.com', 3, 3, '2021-04-01', 15000000.00, N'Đang làm việc'),
    (6, N'Nhân viên 6', '1996-06-06', N'Nữ', '000000000006', N'Hà Nội', '0900000006', 'nv6@example.com', 4, 2, '2021-05-01', 16000000.00, N'Đang làm việc'),
    (7, N'Nhân viên 7', '1997-07-07', N'Nam', '000000000007', N'Hà Nội', '0900000007', 'nv7@example.com', 4, 3, '2021-06-01', 14000000.00, N'Đang làm việc'),
    (8, N'Nhân viên 8', '1998-08-08', N'Nữ', '000000000008', N'Hà Nội', '0900000008', 'nv8@example.com', 5, 2, '2021-07-01', 17000000.00, N'Đang làm việc'),
    (9, N'Nhân viên 9', '1999-09-09', N'Nam', '000000000009', N'Hà Nội', '0900000009', 'nv9@example.com', 5, 3, '2021-08-01', 13000000.00, N'Đang làm việc'),
    (10, N'Nhân viên 10', '2000-10-10', N'Nữ', '000000000010', N'Hà Nội', '0900000010', 'nv10@example.com', 6, 2, '2021-09-01', 25000000.00, N'Đang làm việc')
) AS v(MaNhanVien, HoTen, NgaySinh, GioiTinh, CCCD, DiaChi, SoDienThoai, Email, MaPhongBan, MaChucVu, NgayVaoLam, MucLuong, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.NhanVien WHERE MaNhanVien = v.MaNhanVien);
GO

SET IDENTITY_INSERT dbo.NhanVien OFF;
GO

-- 3. Insert 6 additional synthetic accounts (total 7)
SET IDENTITY_INSERT dbo.TaiKhoan ON;
GO

INSERT INTO dbo.TaiKhoan (
    MaTaiKhoan, MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai, NgayTao
)
SELECT v.MaTaiKhoan, v.MaNhanVien, v.TenDangNhap, v.MatKhauHash, v.VaiTro, v.TrangThai, SYSUTCDATETIME()
FROM (VALUES
    (2, 2, 'mock_user_hr', 'mockhash2', 'HR', 'Hoạt động'),
    (3, 3, 'mock_user_lead', 'mockhash3', 'Manager', 'Hoạt động'),
    (4, 4, 'mock_user_staff1', 'mockhash4', 'NhanVien', 'Hoạt động'),
    (5, 5, 'mock_user_staff2', 'mockhash5', 'NhanVien', 'Hoạt động'),
    (6, 6, 'mock_user_accountant', 'mockhash6', 'KeToan', 'Hoạt động'),
    (7, 7, 'mock_user_staff3', 'mockhash7', 'NhanVien', 'Hoạt động')
) AS v(MaTaiKhoan, MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan WHERE MaTaiKhoan = v.MaTaiKhoan);
GO

SET IDENTITY_INSERT dbo.TaiKhoan OFF;
GO

-- 4. Insert 8 attendance records in dbo.ChamCong (4 null Hwid, 4 synthetic GUID Hwid)
SET IDENTITY_INSERT dbo.ChamCong ON;
GO

INSERT INTO dbo.ChamCong (
    MaChamCong, MaNhanVien, NgayChamCong, GioVao, GioRa, TongGioLam, GioLamThem, HinhThuc, TrangThai, Hwid
)
SELECT v.MaChamCong, v.MaNhanVien, v.NgayChamCong, v.GioVao, v.GioRa, v.TongGioLam, v.GioLamThem, v.HinhThuc, v.TrangThai, v.Hwid
FROM (VALUES
    (1, 1, '2026-04-01', CAST('08:00:00' AS time), CAST('17:00:00' AS time), 8.0, 0.0, N'Vân tay', N'Hợp lệ', NULL),
    (2, 2, '2026-04-01', CAST('08:05:00' AS time), CAST('17:05:00' AS time), 8.0, 0.0, N'Vân tay', N'Hợp lệ', NULL),
    (3, 3, '2026-04-01', CAST('08:00:00' AS time), CAST('17:00:00' AS time), 8.0, 0.0, N'Vân tay', N'Hợp lệ', NULL),
    (4, 4, '2026-04-01', CAST('08:15:00' AS time), CAST('17:15:00' AS time), 8.0, 0.0, N'Vân tay', N'Hợp lệ', NULL),
    (5, 1, '2026-05-02', CAST('07:55:00' AS time), CAST('17:00:00' AS time), 8.0, 0.0, N'Phần mềm', N'Hợp lệ', '11111111-1111-1111-1111-111111111111'),
    (6, 2, '2026-05-02', CAST('08:00:00' AS time), CAST('17:30:00' AS time), 8.5, 0.5, N'Phần mềm', N'Hợp lệ', '22222222-2222-2222-2222-222222222222'),
    (7, 3, '2026-05-02', CAST('08:00:00' AS time), CAST('17:00:00' AS time), 8.0, 0.0, N'Phần mềm', N'Hợp lệ', '33333333-3333-3333-3333-333333333333'),
    (8, 4, '2026-05-02', CAST('08:10:00' AS time), CAST('17:10:00' AS time), 8.0, 0.0, N'Phần mềm', N'Hợp lệ', '44444444-4444-4444-4444-444444444444')
) AS v(MaChamCong, MaNhanVien, NgayChamCong, GioVao, GioRa, TongGioLam, GioLamThem, HinhThuc, TrangThai, Hwid)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ChamCong WHERE MaChamCong = v.MaChamCong);
GO

SET IDENTITY_INSERT dbo.ChamCong OFF;
GO

-- 5. Insert 10 payroll records in dbo.BangLuong
SET IDENTITY_INSERT dbo.BangLuong ON;
GO

INSERT INTO dbo.BangLuong (
    MaBangLuong, MaNhanVien, Thang, Nam, LuongCoBan, TongPhuCap, SoNgayLamViec, SoGioLamThem, TienLamThem,
    TongThuong, TongPhat, BHXH, BHYT, BHTN, ThueTNCN, TongThuNhap, TongKhauTru, LuongThucNhan, NgayTinhLuong, TrangThai
)
SELECT v.MaBangLuong, v.MaNhanVien, 4, 2026, v.LuongCoBan, 1000000.0, 22, 0.0, 0.0,
       0.0, 0.0, v.LuongCoBan * 0.08, v.LuongCoBan * 0.015, v.LuongCoBan * 0.01, 500000.0,
       v.LuongCoBan + 1000000.0, (v.LuongCoBan * 0.105) + 500000.0,
       (v.LuongCoBan + 1000000.0) - ((v.LuongCoBan * 0.105) + 500000.0),
       '2026-04-30', N'Đã chốt'
FROM (VALUES
    (1, 1, 50000000.0),
    (2, 2, 20000000.0),
    (3, 3, 18000000.0),
    (4, 4, 22000000.0),
    (5, 5, 15000000.0),
    (6, 6, 16000000.0),
    (7, 7, 14000000.0),
    (8, 8, 17000000.0),
    (9, 9, 13000000.0),
    (10, 10, 25000000.0)
) AS v(MaBangLuong, MaNhanVien, LuongCoBan)
WHERE NOT EXISTS (SELECT 1 FROM dbo.BangLuong WHERE MaBangLuong = v.MaBangLuong);
GO

SET IDENTITY_INSERT dbo.BangLuong OFF;
GO

-- 6. Insert 1 synthetic reset token (State 2: valid MaTaiKhoan=4, TaiKhoanMaTaiKhoan=NULL)
SET IDENTITY_INSERT dbo.PasswordResetTokens ON;
GO

INSERT INTO dbo.PasswordResetTokens (
    Id, MaTaiKhoan, Email, Token, Purpose, CreatedAt, ExpiresAt, IsUsed, TaiKhoanMaTaiKhoan
)
SELECT 1, 4, 'mock4@example.com', 'mock-token-xyz-123', 'ResetPassword', SYSUTCDATETIME(), DATEADD(day, 1, SYSUTCDATETIME()), 0, NULL
WHERE NOT EXISTS (SELECT 1 FROM dbo.PasswordResetTokens WHERE Id = 1);
GO

SET IDENTITY_INSERT dbo.PasswordResetTokens OFF;
GO
