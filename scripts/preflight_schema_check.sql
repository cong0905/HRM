-- ==============================================================================
-- SCRIPT SQL PREFLIGHT KIỂM TRA CHỈ ĐỌC (READ-ONLY PREFLIGHT AUDIT)
-- Mục đích: Khảo sát hiện trạng Schema và tính toàn vẹn dữ liệu trước khi nâng cấp.
-- 
-- NGUYÊN TẮC THIẾT KẾ:
-- 1. CHỈ DÙNG TRUY VẤN SELECT (READ-ONLY 100%). TUYỆT ĐỐI KHÔNG CÓ LỆNH GHI/SỬA.
-- 2. ĐỊNH DANH SCHEMA TƯỜNG MINH (schema 'dbo.').
-- 3. CHỐNG LỖI BIÊN DỊCH TRÊN SCHEMA THIẾU BẢNG/CỘT: Mọi truy vấn phụ thuộc cột
--    tùy chọn (như Hwid, TaiKhoanMaTaiKhoan) đều dùng sp_executesql sau khi kiểm
--    tra COL_LENGTH, tránh lỗi phân giải tên cột (Invalid column name) ở pha compile.
-- 4. BẢO MẬT: KHÔNG TRUY VẤN MẬT KHẨU (MatKhauHash) HOẶC CHUỖI TOKEN BÍ MẬT (Token).
-- 
-- LƯU Ý TRẠNG THÁI: Script này được thiết kế và kiểm tra cú pháp tĩnh;
-- CHƯA ĐƯỢC KIỂM CHỨNG THỰC THI TRÊN SQL SERVER THẬT do chưa khởi tạo database.
-- ==============================================================================

SET NOCOUNT ON;

PRINT '==============================================================================';
PRINT 'BẮT ĐẦU KIỂM TRA PREFLIGHT SCHEMA VÀ DỮ LIỆU LEGACY (READ-ONLY)';
PRINT 'Thời gian thực thi: ' + CONVERT(VARCHAR(30), GETDATE(), 121);
PRINT '==============================================================================';

-- ------------------------------------------------------------------------------
-- 1. THÔNG TIN DATABASE & COLLATION
-- ------------------------------------------------------------------------------
PRINT CHAR(10) + '--- [1/5] THÔNG TIN DATABASE & COLLATION ---';
SELECT 
    DB_NAME() AS DatabaseName,
    DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DatabaseCollation,
    SERVERPROPERTY('ServerName') AS ServerName,
    SERVERPROPERTY('ProductVersion') AS SqlServerVersion,
    SERVERPROPERTY('Edition') AS SqlServerEdition;

-- ------------------------------------------------------------------------------
-- 2. KIỂM TRA BẢNG dbo.TaiKhoan (CỘT, COLLATION, INDEX & DUPLICATE)
-- ------------------------------------------------------------------------------
PRINT CHAR(10) + '--- [2/5] KIỂM TRA CẤU TRÚC VÀ RÀNG BUỘC TRÊN dbo.TaiKhoan ---';

IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NOT NULL
BEGIN
    -- 2.1 Cấu trúc cột TenDangNhap và MaNhanVien kèm Collation riêng của từng cột
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLengthInBytes,
        c.collation_name AS ColumnCollation,
        DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DatabaseDefaultCollation,
        c.is_nullable AS IsNullable
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.TaiKhoan', N'U')
      AND c.name IN (N'TenDangNhap', N'MaNhanVien', N'MaTaiKhoan');

    -- 2.2 Danh sách Index trên dbo.TaiKhoan (Kiểm tra sự tồn tại của Unique Index)
    SELECT 
        i.name AS IndexName,
        i.type_desc AS IndexType,
        i.is_unique AS IsUnique,
        i.is_primary_key AS IsPrimaryKey,
        c.name AS ColumnName,
        ic.key_ordinal AS KeyOrdinal
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE i.object_id = OBJECT_ID(N'dbo.TaiKhoan', N'U')
    ORDER BY i.name, ic.key_ordinal;

    -- 2.3 RÀ SOÁT TRÙNG LẶP THEO QUY TẮC UNIQUE INDEX DỰ KIẾN (Theo Collation của cột TenDangNhap)
    -- Đây là kiểm tra quyết định: Nếu phát hiện bản ghi trùng, lệnh CREATE UNIQUE INDEX sẽ FAIL.
    PRINT '--> [2.3] Kiểm tra Username trùng lặp theo Collation của cột TenDangNhap:';
    SELECT 
        TenDangNhap,
        COUNT(*) AS DuplicateCount,
        MIN(MaTaiKhoan) AS FirstMaTaiKhoan,
        MAX(MaTaiKhoan) AS LastMaTaiKhoan,
        STRING_AGG(CAST(MaTaiKhoan AS VARCHAR(20)), ', ') AS AccountIdsToResolve
    FROM dbo.TaiKhoan
    GROUP BY TenDangNhap
    HAVING COUNT(*) > 1;

    -- 2.4 KIỂM TRA BỔ SUNG UPPER/TRIM (Chuẩn hóa dự phòng xung đột logic ứng dụng)
    -- Giúp phát hiện các tài khoản chỉ khác nhau bởi khoảng trắng thừa hoặc khác chữ hoa/thường 
    -- trong trường hợp collation cột là Case-Sensitive (_CS).
    PRINT '--> [2.4] Kiểm tra bổ sung: Username xung đột khi chuẩn hóa UPPER & TRIM:';
    SELECT 
        UPPER(RTRIM(LTRIM(TenDangNhap))) AS NormalizedUsername,
        COUNT(*) AS ConflictCount,
        STRING_AGG(CAST(MaTaiKhoan AS VARCHAR(20)), ', ') AS ConflictingAccountIds,
        COUNT(DISTINCT TenDangNhap) AS DistinctVariantsCount
    FROM dbo.TaiKhoan
    GROUP BY UPPER(RTRIM(LTRIM(TenDangNhap)))
    HAVING COUNT(*) > 1;

    -- 2.5 Kiểm tra Nhân viên gắn nhiều tài khoản (Duplicate MaNhanVien)
    PRINT '--> [2.5] Kiểm tra Nhân viên gắn nhiều Tài khoản:';
    SELECT 
        MaNhanVien,
        COUNT(*) AS AccountCount,
        STRING_AGG(CAST(MaTaiKhoan AS VARCHAR(20)), ', ') AS AssociatedAccountIds
    FROM dbo.TaiKhoan
    WHERE MaNhanVien IS NOT NULL
    GROUP BY MaNhanVien
    HAVING COUNT(*) > 1;
END
ELSE
BEGIN
    PRINT '[CẢNH BÁO] Bảng dbo.TaiKhoan KHÔNG TỒN TẠI trong database này!';
END;

-- ------------------------------------------------------------------------------
-- 3. KIỂM TRA BẢNG dbo.ChamCong VÀ CỘT Hwid
-- ------------------------------------------------------------------------------
PRINT CHAR(10) + '--- [3/5] KIỂM TRA CỘT Hwid TRÊN BẢNG dbo.ChamCong ---';

IF OBJECT_ID(N'dbo.ChamCong', N'U') IS NOT NULL
BEGIN
    -- 3.1 Kiểm tra metadata cột Hwid
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLengthInBytes,
        c.is_nullable AS IsNullable
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.ChamCong', N'U') AND c.name = N'Hwid';

    -- 3.2 Thống kê số lượng Hwid (Dùng SQL động để tránh compile error nếu cột Hwid chưa tồn tại)
    IF COL_LENGTH(N'dbo.ChamCong', N'Hwid') IS NOT NULL
    BEGIN
        DECLARE @sqlChamCong NVARCHAR(MAX) = N'
            SELECT 
                COUNT(*) AS TotalRecords,
                COUNT(Hwid) AS HasHwidCount,
                SUM(CASE WHEN Hwid IS NULL THEN 1 ELSE 0 END) AS NullHwidCount,
                COUNT(DISTINCT Hwid) AS DistinctHwidCount
            FROM dbo.ChamCong;';
        EXEC sp_executesql @sqlChamCong;
    END
    ELSE
    BEGIN
        PRINT '[XÁC NHẬN METADATA] Cột Hwid CHƯA TỒN TẠI trên bảng dbo.ChamCong.';
    END;
END
ELSE
BEGIN
    PRINT '[CẢNH BÁO] Bảng dbo.ChamCong KHÔNG TỒN TẠI trong database này!';
END;

-- ------------------------------------------------------------------------------
-- 4. KIỂM TRA BẢNG dbo.PasswordResetTokens VÀ KHÓA NGOẠI
-- ------------------------------------------------------------------------------
PRINT CHAR(10) + '--- [4/5] KIỂM TRA BẢNG dbo.PasswordResetTokens & KHÓA NGOẠI ---';

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NOT NULL
BEGIN
    -- 4.1 Danh sách cột kỹ thuật (KHÔNG SELECT NỘI DUNG CỘT Token)
    SELECT 
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLengthInBytes,
        c.is_nullable AS IsNullable
    FROM sys.columns c
    JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.PasswordResetTokens', N'U');

    -- 4.2 Danh sách Foreign Keys thực tế
    SELECT 
        fk.name AS ForeignKeyName,
        COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ForeignKeyColumn,
        OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
        COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn,
        fk.delete_referential_action_desc AS OnDeleteAction
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    WHERE fk.parent_object_id = OBJECT_ID(N'dbo.PasswordResetTokens', N'U');

    -- 4.3 RÀ SOÁT TOKEN MỒ CÔI (MaTaiKhoan không tồn tại trong dbo.TaiKhoan)
    -- CHÚ Ý: CHỈ LẤY ID KỸ THUẬT, TUYỆT ĐỐI KHÔNG TRUY VẤN CỘT Token
    PRINT '--> [4.3] Kiểm tra Token có MaTaiKhoan mồ côi (không tồn tại trong dbo.TaiKhoan):';
    SELECT 
        p.Id AS TokenId,
        p.MaTaiKhoan,
        p.Purpose,
        p.CreatedAt,
        p.ExpiresAt,
        p.IsUsed
    FROM dbo.PasswordResetTokens p
    WHERE p.MaTaiKhoan IS NOT NULL 
      AND NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan t WHERE t.MaTaiKhoan = p.MaTaiKhoan);

    -- 4.4 THỐNG KÊ 5 TRẠNG THÁI KHÓA NGOẠI (MaTaiKhoan vs TaiKhoanMaTaiKhoan)
    -- Dùng SQL động để kiểm tra an toàn, phòng ngừa database chưa có cột TaiKhoanMaTaiKhoan
    IF COL_LENGTH(N'dbo.PasswordResetTokens', N'TaiKhoanMaTaiKhoan') IS NOT NULL
    BEGIN
        PRINT '--> [4.4] Thống kê ma trận trạng thái dữ liệu Token (5 trường hợp):';
        DECLARE @sqlTokenMatrix NVARCHAR(MAX) = N'
            SELECT 
                COUNT(*) AS TotalTokenRecords,
                -- Trường hợp 1: Hai ID bằng nhau
                SUM(CASE WHEN MaTaiKhoan IS NOT NULL AND TaiKhoanMaTaiKhoan IS NOT NULL AND MaTaiKhoan = TaiKhoanMaTaiKhoan THEN 1 ELSE 0 END) AS State1_BothEqual,
                -- Trường hợp 2: Chỉ MaTaiKhoan có giá trị (shadow FK là NULL)
                SUM(CASE WHEN MaTaiKhoan IS NOT NULL AND TaiKhoanMaTaiKhoan IS NULL THEN 1 ELSE 0 END) AS State2_OnlyMaTaiKhoanHasValue,
                -- Trường hợp 3: Chỉ shadow FK có giá trị (MaTaiKhoan là NULL)
                SUM(CASE WHEN MaTaiKhoan IS NULL AND TaiKhoanMaTaiKhoan IS NOT NULL THEN 1 ELSE 0 END) AS State3_OnlyShadowHasValue,
                -- Trường hợp 4: Hai ID khác nhau (Xung đột dữ liệu)
                SUM(CASE WHEN MaTaiKhoan IS NOT NULL AND TaiKhoanMaTaiKhoan IS NOT NULL AND MaTaiKhoan <> TaiKhoanMaTaiKhoan THEN 1 ELSE 0 END) AS State4_BothNonNullDifferent,
                -- Trường hợp 5: Cả hai đều NULL
                SUM(CASE WHEN MaTaiKhoan IS NULL AND TaiKhoanMaTaiKhoan IS NULL THEN 1 ELSE 0 END) AS State5_BothNull
            FROM dbo.PasswordResetTokens;';
        EXEC sp_executesql @sqlTokenMatrix;

        -- Liệt kê chi tiết các bản ghi xung đột (Trường hợp 4) để dừng báo cáo nếu có
        DECLARE @sqlConflictDetails NVARCHAR(MAX) = N'
            SELECT 
                Id AS ConflictTokenId,
                MaTaiKhoan,
                TaiKhoanMaTaiKhoan,
                Purpose,
                CreatedAt,
                IsUsed
            FROM dbo.PasswordResetTokens
            WHERE MaTaiKhoan IS NOT NULL 
              AND TaiKhoanMaTaiKhoan IS NOT NULL 
              AND MaTaiKhoan <> TaiKhoanMaTaiKhoan;';
        EXEC sp_executesql @sqlConflictDetails;
    END
    ELSE
    BEGIN
        PRINT '[XÁC NHẬN METADATA] Cột shadow TaiKhoanMaTaiKhoan KHÔNG TỒN TẠI trên bảng dbo.PasswordResetTokens.';
    END;
END
ELSE
BEGIN
    PRINT '[CẢNH BÁO] Bảng dbo.PasswordResetTokens KHÔNG TỒN TẠI trong database này!';
END;

-- ------------------------------------------------------------------------------
-- 5. ĐỐI CHIẾU LỊCH SỬ MIGRATION (dbo.__EFMigrationsHistory)
-- ------------------------------------------------------------------------------
PRINT CHAR(10) + '--- [5/5] ĐỐI CHIẾU LỊCH SỬ MIGRATION (dbo.__EFMigrationsHistory) ---';

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
BEGIN
    SELECT 
        MigrationId,
        ProductVersion
    FROM dbo.__EFMigrationsHistory
    ORDER BY MigrationId;

    -- Kiểm tra sự hiện diện của InitialCreate và AddChamCongHwid
    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260331144555_InitialCreate')
        PRINT '--> InitialCreate: ĐÃ GHI NHẬN trong history.';
    ELSE
        PRINT '--> InitialCreate: CHƯA CÓ trong history.';

    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260502105319_AddChamCongHwid')
        PRINT '--> AddChamCongHwid: ĐÃ GHI NHẬN trong history.';
    ELSE
        PRINT '--> AddChamCongHwid: CHƯA CÓ trong history.';

    IF EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId LIKE N'%AddHwidToChamCong%')
        PRINT '--> [CHÚ Ý] Phát hiện migration AddHwidToChamCong trong history (class này thiếu metadata trên code).';
END
ELSE
BEGIN
    PRINT '[XÁC NHẬN METADATA] Bảng dbo.__EFMigrationsHistory KHÔNG TỒN TẠI (Database có thể được tạo bằng script SQL legacy).';
END;

PRINT CHAR(10) + '==============================================================================';
PRINT 'HOÀN TẤT PREFLIGHT AUDIT. KHÔNG CÓ THAO TÁC GHI HAY SỬA ĐỔI NÀO ĐƯỢC THỰC HIỆN.';
PRINT '==============================================================================';
