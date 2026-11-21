-- ==============================================================================
-- SYNTHETIC TEST FIXTURE: DUPLICATE USERNAME TEST
-- Mục đích: Kiểm chứng khả năng phát hiện lỗi của script preflight
-- CHÚ Ý: ĐÂY LÀ FIXTURE TỰ TẠO (MÔ PHỎNG DỮ LIỆU LỖI), KHÔNG PHẢI SNAPSHOT THẬT.
-- ==============================================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'HRM_Next_Test_T02_DuplicateUser')
BEGIN
    CREATE DATABASE [HRM_Next_Test_T02_DuplicateUser];
END;
GO

USE [HRM_Next_Test_T02_DuplicateUser];
GO

-- Tạo bảng TaiKhoan mô phỏng schema baseline (chưa có Unique Index)
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NOT NULL DROP TABLE dbo.TaiKhoan;
GO

CREATE TABLE dbo.TaiKhoan (
    MaTaiKhoan INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien INT NOT NULL,
    TenDangNhap NVARCHAR(50) NOT NULL,
    MatKhauHash NVARCHAR(255) NOT NULL,
    VaiTro NVARCHAR(20) NOT NULL,
    TrangThai NVARCHAR(20) NOT NULL,
    LanDangNhapCuoi DATETIME2 NULL,
    NgayTao DATETIME2 NOT NULL DEFAULT GETDATE()
);
GO

-- Nạp dữ liệu giả mô phỏng lỗi trùng lặp
INSERT INTO dbo.TaiKhoan (MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai)
VALUES 
    (1, N'demo_admin', N'dummy_hash_1', N'Admin', N'Hoạt động'),
    (2, N'test_user_a', N'dummy_hash_2', N'Nhân viên', N'Hoạt động'),
    (3, N'test_user_a', N'dummy_hash_3', N'Nhân viên', N'Hoạt động'), -- TRÙNG LẶP EXACT VỚI ID 2
    (4, N'test_user_b', N'dummy_hash_4', N'Nhân viên', N'Hoạt động'),
    (5, N'TEST_USER_B', N'dummy_hash_5', N'Nhân viên', N'Hoạt động'), -- XUNG ĐỘT HOA/THƯỜNG VỚI ID 4
    (6, N'test_user_c ', N'dummy_hash_6', N'Nhân viên', N'Hoạt động'),-- KHOẢNG TRẮNG THỪA
    (7, N'test_user_c', N'dummy_hash_7', N'Nhân viên', N'Hoạt động'); -- XUNG ĐỘT KHOẢNG TRẮNG
GO

