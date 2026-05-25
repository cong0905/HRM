using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddChamCongHwid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hwid",
                table: "ChamCong",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hwid",
                table: "ChamCong");

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 515, DateTimeKind.Local).AddTicks(8005), new DateTime(2026, 4, 21, 19, 19, 35, 515, DateTimeKind.Local).AddTicks(7981) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(156), new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(131) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(181), new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(181) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(184), new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(183) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(186), new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(185) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(188), new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(187) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(190), new DateTime(2026, 4, 21, 19, 19, 35, 520, DateTimeKind.Local).AddTicks(190) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 4, 21, 19, 19, 35, 478, DateTimeKind.Local).AddTicks(5638));
        }
    }
}
