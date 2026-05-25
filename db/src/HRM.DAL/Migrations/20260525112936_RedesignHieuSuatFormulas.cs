using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RedesignHieuSuatFormulas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "KetQuaCongViec",
                table: "HieuSuatNhanVien",
                newName: "NhanXetCuaQuanLy");

            migrationBuilder.AddColumn<decimal>(
                name: "DiemChuyenCan",
                table: "HieuSuatNhanVien",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TyLeDiLam",
                table: "HieuSuatNhanVien",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TyLeDungGio",
                table: "HieuSuatNhanVien",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TyLeGioLam",
                table: "HieuSuatNhanVien",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 84, DateTimeKind.Local).AddTicks(8342), new DateTime(2026, 5, 25, 18, 29, 36, 84, DateTimeKind.Local).AddTicks(8326) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4148), new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4145) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4157), new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4157) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4159), new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4158) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4160), new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4160) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4162), new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4161) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4163), new DateTime(2026, 5, 25, 18, 29, 36, 86, DateTimeKind.Local).AddTicks(4163) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 5, 25, 18, 29, 36, 67, DateTimeKind.Local).AddTicks(9318));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiemChuyenCan",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropColumn(
                name: "TyLeDiLam",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropColumn(
                name: "TyLeDungGio",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropColumn(
                name: "TyLeGioLam",
                table: "HieuSuatNhanVien");

            migrationBuilder.RenameColumn(
                name: "NhanXetCuaQuanLy",
                table: "HieuSuatNhanVien",
                newName: "KetQuaCongViec");

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
        }
    }
}
