using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyHieuSuatAndRemoveEvaluator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HieuSuatNhanVien_NhanVien_NguoiDanhGia",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropIndex(
                name: "IX_HieuSuatNhanVien_MaNhanVien",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropIndex(
                name: "IX_HieuSuatNhanVien_NguoiDanhGia",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "HieuSuatNhanVien");

            migrationBuilder.DropColumn(
                name: "NguoiDanhGia",
                table: "HieuSuatNhanVien");

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

            migrationBuilder.Sql(@"
                ;WITH DuplicateRows AS (
                    SELECT
                        MaHieuSuat,
                        ROW_NUMBER() OVER (
                            PARTITION BY MaNhanVien, MaKyDanhGia
                            ORDER BY MaHieuSuat DESC
                        ) AS rn
                    FROM HieuSuatNhanVien
                )
                DELETE FROM HieuSuatNhanVien
                WHERE MaHieuSuat IN (
                    SELECT MaHieuSuat
                    FROM DuplicateRows
                    WHERE rn > 1
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_HieuSuatNhanVien_MaNhanVien_MaKyDanhGia",
                table: "HieuSuatNhanVien",
                columns: new[] { "MaNhanVien", "MaKyDanhGia" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HieuSuatNhanVien_MaNhanVien_MaKyDanhGia",
                table: "HieuSuatNhanVien");

            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "HieuSuatNhanVien",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NguoiDanhGia",
                table: "HieuSuatNhanVien",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "NhanVien",
                keyColumn: "MaNhanVien",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 782, DateTimeKind.Local).AddTicks(2960), new DateTime(2026, 3, 31, 21, 45, 54, 782, DateTimeKind.Local).AddTicks(2948) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 1,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2186), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2178) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 2,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2197), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2196) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 3,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2199), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2199) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 4,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2201), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2201) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 5,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2203), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2203) });

            migrationBuilder.UpdateData(
                table: "PhongBan",
                keyColumn: "MaPhongBan",
                keyValue: 6,
                columns: new[] { "NgayCapNhat", "NgayTao" },
                values: new object[] { new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2205), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2204) });

            migrationBuilder.UpdateData(
                table: "TaiKhoan",
                keyColumn: "MaTaiKhoan",
                keyValue: 1,
                column: "NgayTao",
                value: new DateTime(2026, 3, 31, 21, 45, 54, 760, DateTimeKind.Local).AddTicks(8348));

            migrationBuilder.CreateIndex(
                name: "IX_HieuSuatNhanVien_MaNhanVien",
                table: "HieuSuatNhanVien",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_HieuSuatNhanVien_NguoiDanhGia",
                table: "HieuSuatNhanVien",
                column: "NguoiDanhGia");

            migrationBuilder.AddForeignKey(
                name: "FK_HieuSuatNhanVien_NhanVien_NguoiDanhGia",
                table: "HieuSuatNhanVien",
                column: "NguoiDanhGia",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
