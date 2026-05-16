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
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 393, DateTimeKind.Local).AddTicks(2438), new DateTime(2026, 5, 5, 13, 2, 54, 393, DateTimeKind.Local).AddTicks(2427) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7651), new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7647) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7659), new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7659) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7661), new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7661) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7663), new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7662) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7664), new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7664) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7666), new DateTime(2026, 5, 5, 13, 2, 54, 394, DateTimeKind.Local).AddTicks(7666) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 5, 5, 13, 2, 54, 367, DateTimeKind.Local).AddTicks(8652));
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
