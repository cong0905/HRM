using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.Next.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_Departments_DepartmentId",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeProfiles_PositionId",
                schema: "personnel",
                table: "EmployeeProfiles",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_Departments_DepartmentId",
                schema: "personnel",
                table: "EmployeeProfiles",
                column: "DepartmentId",
                principalSchema: "personnel",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_Positions_PositionId",
                schema: "personnel",
                table: "EmployeeProfiles",
                column: "PositionId",
                principalSchema: "personnel",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_Departments_DepartmentId",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeProfiles_Positions_PositionId",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeProfiles_PositionId",
                schema: "personnel",
                table: "EmployeeProfiles");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeProfiles_Departments_DepartmentId",
                schema: "personnel",
                table: "EmployeeProfiles",
                column: "DepartmentId",
                principalSchema: "personnel",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
