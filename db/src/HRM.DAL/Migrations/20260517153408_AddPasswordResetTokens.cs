using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    TaiKhoanMaTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_TaiKhoan_TaiKhoanMaTaiKhoan",
                        column: x => x.TaiKhoanMaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan");
                });

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 485, DateTimeKind.Local).AddTicks(3502), new DateTime(2026, 5, 17, 22, 34, 7, 485, DateTimeKind.Local).AddTicks(3486) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2403), new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2385) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2432), new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2432) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2434), new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2433) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2435), new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2435) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2437), new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2436) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2438), new DateTime(2026, 5, 17, 22, 34, 7, 487, DateTimeKind.Local).AddTicks(2438) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 5, 17, 22, 34, 7, 466, DateTimeKind.Local).AddTicks(5448));

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TaiKhoanMaTaiKhoan",
                table: "PasswordResetTokens",
                column: "TaiKhoanMaTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 671, DateTimeKind.Local).AddTicks(5960), new DateTime(2026, 5, 2, 17, 53, 16, 671, DateTimeKind.Local).AddTicks(5948) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4878), new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4854) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4906), new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4906) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4909), new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4909) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4911), new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4910) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4913), new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4912) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4916), new DateTime(2026, 5, 2, 17, 53, 16, 674, DateTimeKind.Local).AddTicks(4916) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 5, 2, 17, 53, 16, 622, DateTimeKind.Local).AddTicks(2192));
        }
    }
}
