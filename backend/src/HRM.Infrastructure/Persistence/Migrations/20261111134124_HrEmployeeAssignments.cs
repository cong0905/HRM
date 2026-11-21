using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HrEmployeeAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HrEmployeeAssignments",
                schema: "personnel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HrUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HrEmployeeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_AspNetUsers_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_AspNetUsers_HrUserId",
                        column: x => x.HrUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HrEmployeeAssignments_EmployeeProfiles_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "personnel",
                        principalTable: "EmployeeProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_AssignedBy",
                schema: "personnel",
                table: "HrEmployeeAssignments",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_EmployeeId",
                schema: "personnel",
                table: "HrEmployeeAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_HrUserId",
                schema: "personnel",
                table: "HrEmployeeAssignments",
                column: "HrUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HrEmployeeAssignments_HrUserId_EmployeeId",
                schema: "personnel",
                table: "HrEmployeeAssignments",
                columns: new[] { "HrUserId", "EmployeeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HrEmployeeAssignments",
                schema: "personnel");
        }
    }
}
