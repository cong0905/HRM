/*
 * SQL Script: fix_db_history.sql
 * Mục đích: Thủ công chèn bản ghi Migration đã hoàn thành vào Database HRM_System.
 * Việc này giúp EF Core bỏ qua việc tạo lại các bảng đã tồn tại (Nhân viên, Chức vụ...).
 */

-- 0. Chọn Database HRM_System
USE [HRM_System];
GO

-- 1. Nếu chưa có bảng lịch sử thì tạo (cấu trúc chuẩn của EF Core)
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

-- 2. Kiểm tra và chèn bản ghi 'InitialCreate' nếu chưa có
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260331144555_InitialCreate')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260331144555_InitialCreate', N'8.0.0');
    
    PRINT 'Đã đồng bộ hóa Migration [InitialCreate] thành công!';
END
ELSE
BEGIN
    PRINT 'Migration [InitialCreate] đã tồn tại sẵn trong lịch sử, không cần chạy lại.';
END;
GO
