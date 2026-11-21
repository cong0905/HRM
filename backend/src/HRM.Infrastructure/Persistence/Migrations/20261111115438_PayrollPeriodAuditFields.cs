using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PayrollPeriodAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CalculatedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CalculatedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ApprovedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_CalculatedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "CalculatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_PublishedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "PublishedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_ApprovedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "ApprovedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_CalculatedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "CalculatedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_PublishedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "PublishedBy",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_ApprovedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_CalculatedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_PublishedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ApprovedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_CalculatedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_PublishedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "CalculatedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "CalculatedBy",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                schema: "payroll",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                schema: "payroll",
                table: "PayrollPeriods");
        }
    }
}
