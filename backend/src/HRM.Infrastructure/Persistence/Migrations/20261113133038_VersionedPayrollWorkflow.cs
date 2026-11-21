using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersionedPayrollWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"UPDATE p SET ActiveBatchId=b.Id,
ApprovedBatchId=CASE WHEN p.Status IN ('Approved','Published','Locked') THEN b.Id ELSE NULL END,
PublishedBatchId=CASE WHEN p.Status IN ('Published','Locked') THEN b.Id ELSE NULL END
FROM payroll.PayrollPeriods p CROSS APPLY(SELECT TOP(1) Id FROM payroll.CalculationBatches WHERE PeriodId=p.Id ORDER BY Version DESC)b WHERE p.SnapshotRequired=1;");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ActiveBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ActiveBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ApprovedBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ApprovedBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_LockedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "LockedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_PublishedBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "PublishedBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_LockedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "LockedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_CalculationBatches_ActiveBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ActiveBatchId",
                principalSchema: "payroll",
                principalTable: "CalculationBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_CalculationBatches_ApprovedBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ApprovedBatchId",
                principalSchema: "payroll",
                principalTable: "CalculationBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_CalculationBatches_PublishedBatchId",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "PublishedBatchId",
                principalSchema: "payroll",
                principalTable: "CalculationBatches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_LockedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_CalculationBatches_ActiveBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_CalculationBatches_ApprovedBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_CalculationBatches_PublishedBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ActiveBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ApprovedBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_LockedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_PublishedBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ActiveBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ApprovedBatchId",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "LockedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PublishedBatchId",
                schema: "payroll",
                table: "PayrollPeriods");
        }
    }
}
