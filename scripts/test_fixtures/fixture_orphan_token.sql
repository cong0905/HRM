-- ==============================================================================
-- SYNTHETIC TEST FIXTURE: TOKEN RELATIONSHIP & ORPHAN TEST
-- Mục đích: Kiểm chứng khả năng phát hiện lỗi của script preflight cho Token
-- CHÚ Ý: ĐÂY LÀ FIXTURE TỰ TẠO (MÔ PHỎNG DỮ LIỆU LỖI), KHÔNG PHẢI SNAPSHOT THẬT.
-- ==============================================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'HRM_Next_Test_T02_OrphanToken')
BEGIN
    CREATE DATABASE [HRM_Next_Test_T02_OrphanToken];
END;
GO

USE [HRM_Next_Test_T02_OrphanToken];
GO

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NOT NULL DROP TABLE dbo.PasswordResetTokens;
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

INSERT INTO dbo.TaiKhoan (MaNhanVien, TenDangNhap, MatKhauHash, VaiTro, TrangThai)
VALUES 
    (1, N'valid_user_1', N'dummy_hash_1', N'Admin', N'Hoạt động'),
    (2, N'valid_user_2', N'dummy_hash_2', N'Nhân viên', N'Hoạt động');
GO

CREATE TABLE dbo.PasswordResetTokens (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MaTaiKhoan INT NULL,
    Email NVARCHAR(MAX) NULL,
    Token NVARCHAR(MAX) NOT NULL,
    Purpose NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    IsUsed BIT NOT NULL DEFAULT 0,
    TaiKhoanMaTaiKhoan INT NULL,
    CONSTRAINT FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan 
        FOREIGN KEY (TaiKhoanMaTaiKhoan) REFERENCES dbo.TaiKhoan(MaTaiKhoan)
);
GO

-- Nạp 6 bản ghi mô phỏng đúng 5 trạng thái ma trận
INSERT INTO dbo.PasswordResetTokens (MaTaiKhoan, Email, Token, Purpose, ExpiresAt, IsUsed, TaiKhoanMaTaiKhoan)
VALUES
    -- Trạng thái 1: Hai ID bằng nhau
    (1, N'demo1@example.com', N'dummy_token_1', N'ResetPassword', DATEADD(HOUR, 1, GETUTCDATE()), 0, 1),
    
    -- Trạng thái 2: Chỉ MaTaiKhoan có giá trị (shadow FK là NULL)
    (2, N'demo2@example.com', N'dummy_token_2', N'ResetPassword', DATEADD(HOUR, 1, GETUTCDATE()), 0, NULL),
    
    -- Trạng thái 3: Chỉ shadow FK có giá trị (MaTaiKhoan là NULL)
    (NULL, N'demo3@example.com', N'dummy_token_3', N'ResetPassword', DATEADD(HOUR, 1, GETUTCDATE()), 0, 2),
    
    -- Trạng thái 4: Hai ID khác nhau (Xung đột dữ liệu nghiêm trọng)
    (1, N'demo4@example.com', N'dummy_token_4', N'ResetPassword', DATEADD(HOUR, 1, GETUTCDATE()), 0, 2),
    
    -- Trạng thái 5a: Token mồ côi (MaTaiKhoan = 9999 không tồn tại)
    (9999, N'demo5@example.com', N'dummy_token_5', N'ResetPassword', DATEADD(HOUR, 1, GETUTCDATE()), 0, NULL),
    
    -- Trạng thái 5b: Cả hai đều NULL
    (NULL, N'demo6@example.com', N'dummy_token_6', N'VerifyEmail', DATEADD(DAY, 1, GETUTCDATE()), 0, NULL);
GO

