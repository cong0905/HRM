using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddHwidToChamCong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hwid",
                table: "ChamCong",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 734, DateTimeKind.Local).AddTicks(1752), new DateTime(2026, 5, 4, 23, 11, 1, 734, DateTimeKind.Local).AddTicks(1739) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6733), new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6725) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6750), new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6750) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6753), new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6753) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6756), new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6756) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6758), new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6758) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6760), new DateTime(2026, 5, 4, 23, 11, 1, 736, DateTimeKind.Local).AddTicks(6760) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 5, 4, 23, 11, 1, 714, DateTimeKind.Local).AddTicks(8881));
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
