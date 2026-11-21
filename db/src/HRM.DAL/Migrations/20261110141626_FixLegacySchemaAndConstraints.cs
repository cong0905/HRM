using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixLegacySchemaAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BẢO VỆ CHỦ ĐỘNG TRƯỚC THAY ĐỔI SCHEMA:
            // Chặn đứng migration nếu phát hiện bất kỳ nguy cơ mất liên kết token hoặc xung đột dữ liệu nào.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.PasswordResetTokens', N'TaiKhoanMaTaiKhoan') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            DECLARE @msg NVARCHAR(2048);
            DECLARE @ids NVARCHAR(MAX);

            -- 1. MaTaiKhoan NULL nhưng shadow FK có giá trị
            SELECT @ids = STRING_AGG(CAST(Id AS NVARCHAR(20)), '', '')
            FROM dbo.PasswordResetTokens
            WHERE MaTaiKhoan IS NULL AND TaiKhoanMaTaiKhoan IS NOT NULL;

            IF @ids IS NOT NULL
            BEGIN
                SET @msg = N''[MIGRATION BLOCKED] Phat hien Token co TaiKhoanMaTaiKhoan nhung MaTaiKhoan bi NULL. Cac Token ID can xu ly: '' + @ids;
                THROW 51001, @msg, 1;
            END;

            -- 2. Hai ID khác nhau (kể cả cả hai tài khoản đều tồn tại)
            SELECT @ids = STRING_AGG(CAST(Id AS NVARCHAR(20)), '', '')
            FROM dbo.PasswordResetTokens
            WHERE MaTaiKhoan IS NOT NULL AND TaiKhoanMaTaiKhoan IS NOT NULL AND MaTaiKhoan <> TaiKhoanMaTaiKhoan;

            IF @ids IS NOT NULL
            BEGIN
                SET @msg = N''[MIGRATION BLOCKED] Phat hien Token co MaTaiKhoan khac voi TaiKhoanMaTaiKhoan. Cac Token ID can xu ly: '' + @ids;
                THROW 51002, @msg, 1;
            END;
        ';
    END;

    -- 3. MaTaiKhoan mồ côi (không tồn tại trong dbo.TaiKhoan)
    IF COL_LENGTH(N'dbo.PasswordResetTokens', N'MaTaiKhoan') IS NOT NULL AND OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NOT NULL
    BEGIN
        EXEC sp_executesql N'
            DECLARE @msg NVARCHAR(2048);
            DECLARE @ids NVARCHAR(MAX);

            SELECT @ids = STRING_AGG(CAST(Id AS NVARCHAR(20)), '', '')
            FROM dbo.PasswordResetTokens
            WHERE MaTaiKhoan IS NOT NULL
              AND MaTaiKhoan NOT IN (SELECT MaTaiKhoan FROM dbo.TaiKhoan);

            IF @ids IS NOT NULL
            BEGIN
                SET @msg = N''[MIGRATION BLOCKED] Phat hien Token co MaTaiKhoan mo coi khong ton tai trong dbo.TaiKhoan. Cac Token ID can xu ly: '' + @ids;
                THROW 51003, @msg, 1;
            END;
        ';
    END;
END;

-- 4. TenDangNhap trùng lặp trong dbo.TaiKhoan
IF OBJECT_ID(N'dbo.TaiKhoan', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.TaiKhoan', N'TenDangNhap') IS NOT NULL
BEGIN
    EXEC sp_executesql N'
        DECLARE @msg NVARCHAR(2048);
        DECLARE @duplicates NVARCHAR(MAX);

        SELECT @duplicates = STRING_AGG(TenDangNhap + N'' ('' + CAST(Cnt AS NVARCHAR(10)) + N'' lan)'', '', '')
        FROM (
            SELECT TenDangNhap, COUNT(*) AS Cnt
            FROM dbo.TaiKhoan
            GROUP BY TenDangNhap
            HAVING COUNT(*) > 1
        ) AS d;

        IF @duplicates IS NOT NULL
        BEGIN
            SET @msg = N''[MIGRATION BLOCKED] Phat hien TenDangNhap trung lap trong dbo.TaiKhoan. Cac TenDangNhap can xu ly: '' + @duplicates;
            THROW 51004, @msg, 1;
        END;
    ';
END;
");

            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_MaTaiKhoan",
                table: "PasswordResetTokens",
                column: "MaTaiKhoan");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_TaiKhoan_MaTaiKhoan",
                table: "PasswordResetTokens",
                column: "MaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetTokens_TaiKhoan_MaTaiKhoan",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_MaTaiKhoan",
                table: "PasswordResetTokens");

            migrationBuilder.AddColumn<int>(
                name: "TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens",
                column: "TaiKhoanMaTaiKhoan");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens",
                column: "TaiKhoanMaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan");
        }
    }
}
