using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SalaryHistoryAndCalculationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SnapshotId",
                schema: "payroll",
                table: "Payslips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SnapshotRequired",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CalculationBatches",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculationBatches_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalculationBatches_PayrollPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InputReceipts",
                schema: "payroll",
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
                    table.PrimaryKey("PK_InputReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InputReceipts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManualAdjustments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(20,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    VoidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    VoidReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualAdjustments", x => x.Id);
                    table.CheckConstraint("CK_Adjustment_Positive", "[Amount]>0");
                    table.ForeignKey(
                        name: "FK_ManualAdjustments_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManualAdjustments_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManualAdjustments_PayrollPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryHistory",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    MonthlyBase = table.Column<decimal>(type: "decimal(20,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryHistory", x => x.Id);
                    table.CheckConstraint("CK_Salary_Positive", "[MonthlyBase]>0");
                    table.CheckConstraint("CK_Salary_Range", "[EffectiveTo] IS NULL OR [EffectiveTo]>=[EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_SalaryHistory_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryHistory_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalculationSnapshots",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Net = table.Column<decimal>(type: "decimal(20,2)", nullable: false),
                    CanPublish = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculationSnapshots_CalculationBatches_BatchId",
                        column: x => x.BatchId,
                        principalSchema: "payroll",
                        principalTable: "CalculationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalculationSnapshots_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_SnapshotId",
                schema: "payroll",
                table: "Payslips",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculationBatches_CreatedBy",
                schema: "payroll",
                table: "CalculationBatches",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalculationBatches_PeriodId_Version",
                schema: "payroll",
                table: "CalculationBatches",
                columns: new[] { "PeriodId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationSnapshots_BatchId_EmployeeId",
                schema: "payroll",
                table: "CalculationSnapshots",
                columns: new[] { "BatchId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationSnapshots_EmployeeId",
                schema: "payroll",
                table: "CalculationSnapshots",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InputReceipts_UserId_Operation_Key",
                schema: "payroll",
                table: "InputReceipts",
                columns: new[] { "UserId", "Operation", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManualAdjustments_CreatedBy",
                schema: "payroll",
                table: "ManualAdjustments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ManualAdjustments_EmployeeId",
                schema: "payroll",
                table: "ManualAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualAdjustments_PeriodId_EmployeeId",
                schema: "payroll",
                table: "ManualAdjustments",
                columns: new[] { "PeriodId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryHistory_CreatedBy",
                schema: "payroll",
                table: "SalaryHistory",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryHistory_EmployeeId",
                schema: "payroll",
                table: "SalaryHistory",
                column: "EmployeeId",
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryHistory_EmployeeId_EffectiveFrom",
                schema: "payroll",
                table: "SalaryHistory",
                columns: new[] { "EmployeeId", "EffectiveFrom" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payslips_CalculationSnapshots_SnapshotId",
                schema: "payroll",
                table: "Payslips",
                column: "SnapshotId",
                principalSchema: "payroll",
                principalTable: "CalculationSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payslips_CalculationSnapshots_SnapshotId",
                schema: "payroll",
                table: "Payslips");

            migrationBuilder.DropTable(
                name: "CalculationSnapshots",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "InputReceipts",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "ManualAdjustments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "SalaryHistory",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "CalculationBatches",
                schema: "payroll");

            migrationBuilder.DropIndex(
                name: "IX_Payslips_SnapshotId",
                schema: "payroll",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "SnapshotId",
                schema: "payroll",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "SnapshotRequired",
                schema: "payroll",
                table: "PayrollPeriods");
        }
    }
}
