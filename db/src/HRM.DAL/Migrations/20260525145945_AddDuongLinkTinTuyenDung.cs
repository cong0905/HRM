using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddDuongLinkTinTuyenDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DuongLinkTin",
                table: "TinTuyenDung",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 444, DateTimeKind.Local).AddTicks(5714), new DateTime(2026, 5, 25, 21, 59, 45, 444, DateTimeKind.Local).AddTicks(5706) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2408), new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2401) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2415), new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2415) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2417), new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2417) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2419), new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2419) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2421), new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2421) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2423), new DateTime(2026, 5, 25, 21, 59, 45, 446, DateTimeKind.Local).AddTicks(2422) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 5, 25, 21, 59, 45, 418, DateTimeKind.Local).AddTicks(1230));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuongLinkTin",
                table: "TinTuyenDung");

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
    }
}
