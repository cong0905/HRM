using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeLifecycleAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Gender",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaritalStatus",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PositionId",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "TerminationDate",
                schema: "personnel",
                table: "EmployeeProfiles",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivationOutboxMessages",
                schema: "personnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    DispatchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivationOutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivationOutboxMessages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCodeCounters",
                schema: "personnel",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCodeCounters", x => x.Year);
                });

            // Backfill deterministically before enabling uniqueness. Existing identities and history stay intact.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT YEAR(SWITCHOFFSET([CreatedAtUtc], '+07:00')) FROM [personnel].[EmployeeProfiles]
    GROUP BY YEAR(SWITCHOFFSET([CreatedAtUtc], '+07:00')) HAVING COUNT_BIG(*) > 9999)
    THROW 51008, 'Existing employee count exceeds the supported code range for a year.', 1;
;WITH numbered AS (
    SELECT [Id], YEAR(SWITCHOFFSET([CreatedAtUtc], '+07:00')) AS [CodeYear],
        ROW_NUMBER() OVER (PARTITION BY YEAR(SWITCHOFFSET([CreatedAtUtc], '+07:00')) ORDER BY [CreatedAtUtc], [Id]) AS [Number]
    FROM [personnel].[EmployeeProfiles]
)
UPDATE p SET [EmployeeCode] = 'NV' + RIGHT('0000' + CAST(n.[CodeYear] AS varchar(4)), 4)
    + RIGHT('0000' + CAST(n.[Number] AS varchar(4)), 4), [Status] = 'DangLamViec',
    [Email] = COALESCE(u.[Email], '')
FROM [personnel].[EmployeeProfiles] p JOIN numbered n ON n.[Id] = p.[Id]
JOIN [AspNetUsers] u ON u.[Id] = p.[UserId];
INSERT INTO [personnel].[EmployeeCodeCounters] ([Year], [LastValue])
SELECT YEAR(SWITCHOFFSET([CreatedAtUtc], '+07:00')), COUNT(*) FROM [personnel].[EmployeeProfiles]
GROUP BY YEAR(SWITCHOFFSET([CreatedAtUtc], '+07:00'));");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_EmployeeCode",
                schema: "personnel",
                table: "EmployeeProfiles",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivationOutboxMessages_Status",
                schema: "personnel",
                table: "ActivationOutboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ActivationOutboxMessages_UserId",
                schema: "personnel",
                table: "ActivationOutboxMessages",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivationOutboxMessages",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "EmployeeCodeCounters",
                schema: "personnel");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_EmployeeCode",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "Gender",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "HireDate",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "MaritalStatus",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "PositionId",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropColumn(
                name: "TerminationDate",
                schema: "personnel",
                table: "EmployeeProfiles");
        }
    }
}
