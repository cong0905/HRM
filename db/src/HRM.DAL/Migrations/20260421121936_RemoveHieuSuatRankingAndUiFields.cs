using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHieuSuatRankingAndUiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "XepHang",
                table: "HieuSuatNhanVien");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XepHang",
                table: "HieuSuatNhanVien",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 223, DateTimeKind.Local).AddTicks(9017), new DateTime(2026, 4, 21, 19, 9, 29, 223, DateTimeKind.Local).AddTicks(8998) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2579), new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2568) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2594), new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2594) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2596), new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2595) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2597), new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2597) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2599), new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2599) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2601), new DateTime(2026, 4, 21, 19, 9, 29, 226, DateTimeKind.Local).AddTicks(2600) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 4, 21, 19, 9, 29, 200, DateTimeKind.Local).AddTicks(4481));
        }
    }
}
