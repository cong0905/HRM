using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedWorkCalendars : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "calendar");

            migrationBuilder.CreateTable(
                name: "CalendarPeriodSnapshots",
                schema: "calendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayrollPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarPeriodSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarPeriodSnapshots_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkCalendarVersions",
                schema: "calendar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkCalendarVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkCalendarVersions_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarPeriodSnapshots_PayrollPeriodId",
                schema: "calendar",
                table: "CalendarPeriodSnapshots",
                column: "PayrollPeriodId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCalendarVersions_CreatedBy",
                schema: "calendar",
                table: "WorkCalendarVersions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCalendarVersions_EffectiveFrom",
                schema: "calendar",
                table: "WorkCalendarVersions",
                column: "EffectiveFrom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarPeriodSnapshots",
                schema: "calendar");

            migrationBuilder.DropTable(
                name: "WorkCalendarVersions",
                schema: "calendar");
        }
    }
}
