using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LeaveTransactionAndLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "WorkingDays",
                schema: "personnel",
                table: "LeaveRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "personnel",
                table: "LeaveRecords",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "DecisionReason",
                schema: "personnel",
                table: "LeaveRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DecidedAtUtc",
                schema: "personnel",
                table: "LeaveRecords",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAtUtc",
                schema: "personnel",
                table: "LeaveRecords",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "personnel",
                table: "LeaveRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                schema: "personnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AllocatedDays = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UsedDays = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.CheckConstraint("CK_LeaveBalances_UsedDays_NonNegative", "[UsedDays] >= 0");
                    table.CheckConstraint("CK_LeaveBalances_NoOverdraft", "[AllocatedDays] >= [UsedDays]");
                    table.ForeignKey(
                        name: "FK_LeaveBalances_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveIdempotencyRecords",
                schema: "personnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Key = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveIdempotencyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveIdempotencyRecords_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveLedgerEntries",
                schema: "personnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeaveRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Days = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveLedgerEntries_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveLedgerEntries_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveLedgerEntries_LeaveRecords_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalSchema: "personnel",
                        principalTable: "LeaveRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_Year",
                schema: "personnel",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveIdempotencyRecords_UserId_Operation_Key",
                schema: "personnel",
                table: "LeaveIdempotencyRecords",
                columns: new[] { "UserId", "Operation", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedgerEntries_ActorId",
                schema: "personnel",
                table: "LeaveLedgerEntries",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedgerEntries_EmployeeId_Year",
                schema: "personnel",
                table: "LeaveLedgerEntries",
                columns: new[] { "EmployeeId", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedgerEntries_EmployeeId_Year_Action",
                schema: "personnel",
                table: "LeaveLedgerEntries",
                columns: new[] { "EmployeeId", "Year", "Action" },
                unique: true,
                filter: "[Action] = N'Grant'");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveLedgerEntries_LeaveRequestId_Action",
                schema: "personnel",
                table: "LeaveLedgerEntries",
                columns: new[] { "LeaveRequestId", "Action" },
                unique: true,
                filter: "[LeaveRequestId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveIdempotencyRecords",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "LeaveLedgerEntries",
                schema: "personnel");

            migrationBuilder.DropTable(
                name: "LeaveBalances",
                schema: "personnel");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "personnel",
                table: "LeaveRecords");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                schema: "personnel",
                table: "LeaveRecords");

            migrationBuilder.DropColumn(
                name: "DecidedAtUtc",
                schema: "personnel",
                table: "LeaveRecords");

            migrationBuilder.DropColumn(
                name: "DecisionReason",
                schema: "personnel",
                table: "LeaveRecords");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "personnel",
                table: "LeaveRecords");

            migrationBuilder.DropColumn(
                name: "WorkingDays",
                schema: "personnel",
                table: "LeaveRecords");
        }
    }
}
