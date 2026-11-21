using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OnlineAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attendance");

            migrationBuilder.CreateTable(
                name: "AttendanceIdempotencyRecords",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceIdempotencyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceIdempotencyRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSummaries",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CheckOutUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CalendarVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CalendarDefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EligibleMinutes = table.Column<decimal>(type: "decimal(20,10)", nullable: false),
                    LateMinutes = table.Column<decimal>(type: "decimal(20,10)", nullable: false),
                    EarlyLeaveMinutes = table.Column<decimal>(type: "decimal(20,10)", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSummaries_WorkCalendarVersions_CalendarVersionId",
                        column: x => x.CalendarVersionId,
                        principalSchema: "calendar",
                        principalTable: "WorkCalendarVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceAuditLogs",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    BeforeCheckInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    BeforeCheckOutUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    AfterCheckInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    AfterCheckOutUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    BeforeEligibleMinutes = table.Column<decimal>(type: "decimal(20,10)", nullable: false),
                    AfterEligibleMinutes = table.Column<decimal>(type: "decimal(20,10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceAuditLogs_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceAuditLogs_AttendanceSummaries_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "attendance",
                        principalTable: "AttendanceSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceEvents",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EffectiveCheckInUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    EffectiveCheckOutUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceEvents_AttendanceSummaries_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "attendance",
                        principalTable: "AttendanceSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceOvertimeEntries",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Minutes = table.Column<decimal>(type: "decimal(20,10)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceOvertimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceOvertimeEntries_AspNetUsers_ConfirmedBy",
                        column: x => x.ConfirmedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceOvertimeEntries_AttendanceSummaries_AttendanceId",
                        column: x => x.AttendanceId,
                        principalSchema: "attendance",
                        principalTable: "AttendanceSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_ActorId",
                schema: "attendance",
                table: "AttendanceAuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAuditLogs_AttendanceId",
                schema: "attendance",
                table: "AttendanceAuditLogs",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_ActorId",
                schema: "attendance",
                table: "AttendanceEvents",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceEvents_AttendanceId_OccurredAtUtc",
                schema: "attendance",
                table: "AttendanceEvents",
                columns: new[] { "AttendanceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceIdempotencyRecords_UserId_Operation_Key",
                schema: "attendance",
                table: "AttendanceIdempotencyRecords",
                columns: new[] { "UserId", "Operation", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceOvertimeEntries_AttendanceId",
                schema: "attendance",
                table: "AttendanceOvertimeEntries",
                column: "AttendanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceOvertimeEntries_ConfirmedBy",
                schema: "attendance",
                table: "AttendanceOvertimeEntries",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_CalendarVersionId",
                schema: "attendance",
                table: "AttendanceSummaries",
                column: "CalendarVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSummaries_EmployeeId_BusinessDate",
                schema: "attendance",
                table: "AttendanceSummaries",
                columns: new[] { "EmployeeId", "BusinessDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceAuditLogs",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "AttendanceEvents",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "AttendanceIdempotencyRecords",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "AttendanceOvertimeEntries",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "AttendanceSummaries",
                schema: "attendance");
        }
    }
}
