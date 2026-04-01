using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HRM.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChucVu",
                columns: table => new
                {
                    MaChucVu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChucVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CapBac = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucVu", x => x.MaChucVu);
                });

            migrationBuilder.CreateTable(
                name: "ChuongTrinhDaoTao",
                columns: table => new
                {
                    MaDaoTao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhoaHoc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MucTieu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiLuong = table.Column<int>(type: "int", nullable: true),
                    GiangVien = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DiaDiem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChiPhi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SoHocVienToiDa = table.Column<int>(type: "int", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Lên kế hoạch"),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuongTrinhDaoTao", x => x.MaDaoTao);
                });

            migrationBuilder.CreateTable(
                name: "KyDanhGia",
                columns: table => new
                {
                    MaKyDanhGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKyDanhGia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Mở")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KyDanhGia", x => x.MaKyDanhGia);
                });

            migrationBuilder.CreateTable(
                name: "LoaiNghiPhep",
                columns: table => new
                {
                    MaLoaiPhep = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiPhep = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CoLuong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiNghiPhep", x => x.MaLoaiPhep);
                });

            migrationBuilder.CreateTable(
                name: "LoaiPhuCap",
                columns: table => new
                {
                    MaPhuCap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenPhuCap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiPhuCap", x => x.MaPhuCap);
                });

            migrationBuilder.CreateTable(
                name: "BangLuong",
                columns: table => new
                {
                    MaBangLuong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    LuongCoBan = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongPhuCap = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoNgayLamViec = table.Column<int>(type: "int", nullable: false),
                    SoGioLamThem = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TienLamThem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongThuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongPhat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BHXH = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BHYT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BHTN = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ThueTNCN = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TongThuNhap = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TongKhauTru = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    LuongThucNhan = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NgayTinhLuong = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chờ duyệt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BangLuong", x => x.MaBangLuong);
                });

            migrationBuilder.CreateTable(
                name: "ChamCong",
                columns: table => new
                {
                    MaChamCong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    NgayChamCong = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioVao = table.Column<TimeSpan>(type: "time", nullable: true),
                    GioRa = table.Column<TimeSpan>(type: "time", nullable: true),
                    TongGioLam = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GioLamThem = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    HinhThuc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Bình thường"),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChamCong", x => x.MaChamCong);
                });

            migrationBuilder.CreateTable(
                name: "ChinhSach",
                columns: table => new
                {
                    MaChinhSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenChinhSach = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoaiChinhSach = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhamViApDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHieuLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhienBan = table.Column<int>(type: "int", nullable: false),
                    NguoiPheDuyet = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiPheDuyetNavMaNhanVien = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChinhSach", x => x.MaChinhSach);
                });

            migrationBuilder.CreateTable(
                name: "ChungChi",
                columns: table => new
                {
                    MaChungChi = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    TenChungChi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoaiChungChi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ToChucCap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayCap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuongDanFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChungChi", x => x.MaChungChi);
                });

            migrationBuilder.CreateTable(
                name: "DanhGiaDaoTao",
                columns: table => new
                {
                    MaDanhGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDaoTao = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    DiemSo = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    DanhGiaGiangVien = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PhanHoiHocVien = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChatLuongKhoaHoc = table.Column<int>(type: "int", nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaDaoTao", x => x.MaDanhGia);
                    table.ForeignKey(
                        name: "FK_DanhGiaDaoTao_ChuongTrinhDaoTao_MaDaoTao",
                        column: x => x.MaDaoTao,
                        principalTable: "ChuongTrinhDaoTao",
                        principalColumn: "MaDaoTao",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DieuChinhChamCong",
                columns: table => new
                {
                    MaDieuChinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaChamCong = table.Column<int>(type: "int", nullable: false),
                    NguoiDieuChinh = table.Column<int>(type: "int", nullable: false),
                    GioVaoCu = table.Column<TimeSpan>(type: "time", nullable: true),
                    GioRaCu = table.Column<TimeSpan>(type: "time", nullable: true),
                    GioVaoMoi = table.Column<TimeSpan>(type: "time", nullable: true),
                    GioRaMoi = table.Column<TimeSpan>(type: "time", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NgayDieuChinh = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuChinhChamCong", x => x.MaDieuChinh);
                    table.ForeignKey(
                        name: "FK_DieuChinhChamCong_ChamCong_MaChamCong",
                        column: x => x.MaChamCong,
                        principalTable: "ChamCong",
                        principalColumn: "MaChamCong",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonNghiPhep",
                columns: table => new
                {
                    MaDonPhep = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaLoaiPhep = table.Column<int>(type: "int", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoNgayNghi = table.Column<decimal>(type: "decimal(3,1)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chờ duyệt"),
                    NguoiPheDuyet = table.Column<int>(type: "int", nullable: true),
                    NgayPheDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayHuy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoHuy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonNghiPhep", x => x.MaDonPhep);
                    table.ForeignKey(
                        name: "FK_DonNghiPhep_LoaiNghiPhep_MaLoaiPhep",
                        column: x => x.MaLoaiPhep,
                        principalTable: "LoaiNghiPhep",
                        principalColumn: "MaLoaiPhep",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HieuSuatNhanVien",
                columns: table => new
                {
                    MaHieuSuat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaKyDanhGia = table.Column<int>(type: "int", nullable: false),
                    DiemKPI = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    KetQuaCongViec = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TyLeHoanThanhDeadline = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SoGioLamViec = table.Column<decimal>(type: "decimal(7,2)", nullable: true),
                    XepHang = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NguoiDanhGia = table.Column<int>(type: "int", nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HieuSuatNhanVien", x => x.MaHieuSuat);
                    table.ForeignKey(
                        name: "FK_HieuSuatNhanVien_KyDanhGia_MaKyDanhGia",
                        column: x => x.MaKyDanhGia,
                        principalTable: "KyDanhGia",
                        principalColumn: "MaKyDanhGia",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuChinhSach",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaChinhSach = table.Column<int>(type: "int", nullable: false),
                    PhienBanCu = table.Column<int>(type: "int", nullable: true),
                    PhienBanMoi = table.Column<int>(type: "int", nullable: true),
                    NoiDungThayDoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LyDoSuaDoi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NguoiSuaDoi = table.Column<int>(type: "int", nullable: true),
                    NgaySuaDoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChinhSachMaChinhSach = table.Column<int>(type: "int", nullable: false),
                    NguoiSuaDoiNavMaNhanVien = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuChinhSach", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuChinhSach_ChinhSach_ChinhSachMaChinhSach",
                        column: x => x.ChinhSachMaChinhSach,
                        principalTable: "ChinhSach",
                        principalColumn: "MaChinhSach",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichSuDieuChuyen",
                columns: table => new
                {
                    MaLichSu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaPhongBanCu = table.Column<int>(type: "int", nullable: true),
                    MaPhongBanMoi = table.Column<int>(type: "int", nullable: true),
                    MaChucVuCu = table.Column<int>(type: "int", nullable: true),
                    MaChucVuMoi = table.Column<int>(type: "int", nullable: true),
                    MucLuongCu = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MucLuongMoi = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NgayThayDoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiThucHien = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuDieuChuyen", x => x.MaLichSu);
                    table.ForeignKey(
                        name: "FK_LichSuDieuChuyen_ChucVu_MaChucVuCu",
                        column: x => x.MaChucVuCu,
                        principalTable: "ChucVu",
                        principalColumn: "MaChucVu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuDieuChuyen_ChucVu_MaChucVuMoi",
                        column: x => x.MaChucVuMoi,
                        principalTable: "ChucVu",
                        principalColumn: "MaChucVu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    MaNhanVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNV = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true, computedColumnSql: "('NV' + RIGHT('00000' + CAST(MaNhanVien AS VARCHAR(5)), 5))", stored: true),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CCCD = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TinhTrangHonNhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MaPhongBan = table.Column<int>(type: "int", nullable: true),
                    MaChucVu = table.Column<int>(type: "int", nullable: true),
                    NgayVaoLam = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MucLuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Đang làm việc"),
                    NgayNghiViec = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaiKhoanMaTaiKhoan = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.MaNhanVien);
                    table.ForeignKey(
                        name: "FK_NhanVien_ChucVu_MaChucVu",
                        column: x => x.MaChucVu,
                        principalTable: "ChucVu",
                        principalColumn: "MaChucVu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhanCongDaoTao",
                columns: table => new
                {
                    MaPhanCong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaDaoTao = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TyLeThamDu = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    KetQuaKiemTra = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PhanHoi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Đã đăng ký")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCongDaoTao", x => x.MaPhanCong);
                    table.ForeignKey(
                        name: "FK_PhanCongDaoTao_ChuongTrinhDaoTao_MaDaoTao",
                        column: x => x.MaDaoTao,
                        principalTable: "ChuongTrinhDaoTao",
                        principalColumn: "MaDaoTao",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhanCongDaoTao_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhongBan",
                columns: table => new
                {
                    MaPhongBan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenPhongBan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTaChucNang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayThanhLap = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiaDiemLamViec = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NganSach = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Hoạt động"),
                    MaTruongPhong = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongBan", x => x.MaPhongBan);
                    table.ForeignKey(
                        name: "FK_PhongBan_NhanVien_MaTruongPhong",
                        column: x => x.MaTruongPhong,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhuCapNhanVien",
                columns: table => new
                {
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    MaPhuCap = table.Column<int>(type: "int", nullable: false),
                    NgayApDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuCapNhanVien", x => new { x.MaNhanVien, x.MaPhuCap, x.NgayApDung });
                    table.ForeignKey(
                        name: "FK_PhuCapNhanVien_LoaiPhuCap_MaPhuCap",
                        column: x => x.MaPhuCap,
                        principalTable: "LoaiPhuCap",
                        principalColumn: "MaPhuCap",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhuCapNhanVien_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoNgayPhep",
                columns: table => new
                {
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    TongSoNgayPhep = table.Column<int>(type: "int", nullable: false),
                    SoNgayDaSuDung = table.Column<int>(type: "int", nullable: false),
                    PhepNamCuConLai = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoNgayPhep", x => new { x.MaNhanVien, x.Nam });
                    table.ForeignKey(
                        name: "FK_SoNgayPhep_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    TenDangNhap = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MatKhauHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LanDangNhapCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiLieuNhanVien",
                columns: table => new
                {
                    MaTaiLieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    LoaiTaiLieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenTaiLieu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DuongDanFile = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuNhanVien", x => x.MaTaiLieu);
                    table.ForeignKey(
                        name: "FK_TaiLieuNhanVien_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    MaThongBao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiNhan = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NhanVienMaNhanVien = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.MaThongBao);
                    table.ForeignKey(
                        name: "FK_ThongBao_NhanVien_MaNguoiNhan",
                        column: x => x.MaNguoiNhan,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThongBao_NhanVien_NhanVienMaNhanVien",
                        column: x => x.NhanVienMaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien");
                });

            migrationBuilder.CreateTable(
                name: "ThuongPhat",
                columns: table => new
                {
                    MaThuongPhat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LoaiChiTiet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayApDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiPheDuyet = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Chờ duyệt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThuongPhat", x => x.MaThuongPhat);
                    table.ForeignKey(
                        name: "FK_ThuongPhat_NhanVien_MaNhanVien",
                        column: x => x.MaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThuongPhat_NhanVien_NguoiPheDuyet",
                        column: x => x.NguoiPheDuyet,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "XacNhanChinhSach",
                columns: table => new
                {
                    MaXacNhan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaChinhSach = table.Column<int>(type: "int", nullable: false),
                    MaNhanVien = table.Column<int>(type: "int", nullable: false),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChinhSachMaChinhSach = table.Column<int>(type: "int", nullable: false),
                    NhanVienMaNhanVien = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XacNhanChinhSach", x => x.MaXacNhan);
                    table.ForeignKey(
                        name: "FK_XacNhanChinhSach_ChinhSach_ChinhSachMaChinhSach",
                        column: x => x.ChinhSachMaChinhSach,
                        principalTable: "ChinhSach",
                        principalColumn: "MaChinhSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_XacNhanChinhSach_NhanVien_NhanVienMaNhanVien",
                        column: x => x.NhanVienMaNhanVien,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TinTuyenDung",
                columns: table => new
                {
                    MaTinTuyenDung = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ViTriTuyenDung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaPhongBan = table.Column<int>(type: "int", nullable: true),
                    MoTaCongViec = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YeuCauUngVien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SoLuongCanTuyen = table.Column<int>(type: "int", nullable: false),
                    MucLuongMin = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MucLuongMax = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ThoiHanNhanHoSo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiadiemLamViec = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Đang tuyển"),
                    NguoiTao = table.Column<int>(type: "int", nullable: true),
                    NgayDang = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TinTuyenDung", x => x.MaTinTuyenDung);
                    table.ForeignKey(
                        name: "FK_TinTuyenDung_NhanVien_NguoiTao",
                        column: x => x.NguoiTao,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TinTuyenDung_PhongBan_MaPhongBan",
                        column: x => x.MaPhongBan,
                        principalTable: "PhongBan",
                        principalColumn: "MaPhongBan",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UngVien",
                columns: table => new
                {
                    MaUngVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTinTuyenDung = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    DuongDanCV = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DuongDanThuXinViec = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KinhNghiem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BangCap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KyNang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhanLoai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Chờ xem xét"),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Mới nộp"),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayNop = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UngVien", x => x.MaUngVien);
                    table.ForeignKey(
                        name: "FK_UngVien_TinTuyenDung_MaTinTuyenDung",
                        column: x => x.MaTinTuyenDung,
                        principalTable: "TinTuyenDung",
                        principalColumn: "MaTinTuyenDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhongVan",
                columns: table => new
                {
                    MaPhongVan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaUngVien = table.Column<int>(type: "int", nullable: false),
                    VongPhongVan = table.Column<int>(type: "int", nullable: false),
                    NgayPhongVan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiaDiem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NguoiPhongVan = table.Column<int>(type: "int", nullable: true),
                    CauHoiPhongVan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KetQua = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiemDanhGia = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    NhanXet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Đã lên lịch")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongVan", x => x.MaPhongVan);
                    table.ForeignKey(
                        name: "FK_PhongVan_NhanVien_NguoiPhongVan",
                        column: x => x.NguoiPhongVan,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhongVan_UngVien_MaUngVien",
                        column: x => x.MaUngVien,
                        principalTable: "UngVien",
                        principalColumn: "MaUngVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuyetDinhTuyenDung",
                columns: table => new
                {
                    MaQuyetDinh = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaUngVien = table.Column<int>(type: "int", nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NgayQuyetDinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiQuyetDinh = table.Column<int>(type: "int", nullable: true),
                    MucLuongDeXuat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NgayBatDauLamViec = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaGuiOfferLetter = table.Column<bool>(type: "bit", nullable: false),
                    PhanHoiUngVien = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyetDinhTuyenDung", x => x.MaQuyetDinh);
                    table.ForeignKey(
                        name: "FK_QuyetDinhTuyenDung_NhanVien_NguoiQuyetDinh",
                        column: x => x.NguoiQuyetDinh,
                        principalTable: "NhanVien",
                        principalColumn: "MaNhanVien",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuyetDinhTuyenDung_UngVien_MaUngVien",
                        column: x => x.MaUngVien,
                        principalTable: "UngVien",
                        principalColumn: "MaUngVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ChucVu",
                columns: new[] { "MaChucVu", "CapBac", "MoTa", "TenChucVu" },
                values: new object[,]
                {
                    { 1, 6, "Giám đốc điều hành công ty", "Giám đốc" },
                    { 2, 5, "Phó Giám đốc", "Phó Giám đốc" },
                    { 3, 4, "Trưởng phòng ban", "Trưởng phòng" },
                    { 4, 3, "Phó phòng ban", "Phó phòng" },
                    { 5, 2, "Trưởng nhóm làm việc", "Trưởng nhóm" },
                    { 6, 1, "Nhân viên", "Nhân viên" }
                });

            migrationBuilder.InsertData(
                table: "LoaiNghiPhep",
                columns: new[] { "MaLoaiPhep", "CoLuong", "MoTa", "TenLoaiPhep" },
                values: new object[,]
                {
                    { 1, true, "Nghỉ phép hàng năm theo quy định", "Phép năm" },
                    { 2, true, "Nghỉ ốm có giấy xác nhận y tế", "Phép ốm" },
                    { 3, false, "Nghỉ không hưởng lương", "Phép không lương" },
                    { 4, true, "Nghỉ do việc cá nhân", "Nghỉ việc riêng" },
                    { 5, true, "Nghỉ thai sản theo quy định", "Nghỉ thai sản" }
                });

            migrationBuilder.InsertData(
                table: "LoaiPhuCap",
                columns: new[] { "MaPhuCap", "MoTa", "SoTien", "TenPhuCap" },
                values: new object[,]
                {
                    { 1, "Hỗ trợ tiền ăn trưa hàng tháng", 730000m, "Phụ cấp ăn trưa" },
                    { 2, "Hỗ trợ chi phí đi lại", 500000m, "Phụ cấp xăng xe" },
                    { 3, "Hỗ trợ chi phí liên lạc", 300000m, "Phụ cấp điện thoại" },
                    { 4, "Phụ cấp theo vị trí công việc", 1000000m, "Phụ cấp vị trí" },
                    { 5, "Phụ cấp cho vị trí quản lý", 2000000m, "Phụ cấp trách nhiệm" }
                });

            migrationBuilder.InsertData(
                table: "PhongBan",
                columns: new[] { "MaPhongBan", "DiaDiemLamViec", "MaTruongPhong", "MoTaChucNang", "NganSach", "NgayCapNhat", "NgayTao", "NgayThanhLap", "TenPhongBan", "TrangThai" },
                values: new object[,]
                {
                    { 1, "Tầng 10", null, "Điều hành toàn bộ hoạt động công ty", null, new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2186), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2178), new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Ban Giám đốc", "Hoạt động" },
                    { 2, "Tầng 5", null, "Quản lý nhân sự, tuyển dụng, đào tạo", null, new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2197), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2196), new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Nhân sự", "Hoạt động" },
                    { 3, "Tầng 5", null, "Quản lý tài chính, kế toán", null, new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2199), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2199), new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Kế toán", "Hoạt động" },
                    { 4, "Tầng 3", null, "Phát triển kinh doanh, bán hàng", null, new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2201), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2201), new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Kinh doanh", "Hoạt động" },
                    { 5, "Tầng 4", null, "Phát triển sản phẩm, kỹ thuật", null, new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2203), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2203), new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Kỹ thuật", "Hoạt động" },
                    { 6, "Tầng 3", null, "Marketing, truyền thông", null, new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2205), new DateTime(2026, 3, 31, 21, 45, 54, 784, DateTimeKind.Local).AddTicks(2204), new DateTime(2020, 3, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Phòng Marketing", "Hoạt động" }
                });

            migrationBuilder.InsertData(
                table: "NhanVien",
                columns: new[] { "MaNhanVien", "AnhDaiDien", "CCCD", "DiaChi", "Email", "GioiTinh", "HoTen", "MaChucVu", "MaPhongBan", "MucLuong", "NgayCapNhat", "NgayNghiViec", "NgaySinh", "NgayTao", "NgayVaoLam", "SoDienThoai", "TaiKhoanMaTaiKhoan", "TinhTrangHonNhan", "TrangThai" },
                values: new object[] { 1, null, "000000000001", "Hà Nội", "admin@hrm.com", "Nam", "Quản trị viên", 1, 1, 50000000m, new DateTime(2026, 3, 31, 21, 45, 54, 782, DateTimeKind.Local).AddTicks(2960), null, new DateTime(1990, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 31, 21, 45, 54, 782, DateTimeKind.Local).AddTicks(2948), new DateTime(2020, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "0900000001", null, null, "Đang làm việc" });

            migrationBuilder.InsertData(
                table: "TaiKhoan",
                columns: new[] { "MaTaiKhoan", "LanDangNhapCuoi", "MaNhanVien", "MatKhauHash", "NgayTao", "TenDangNhap", "TrangThai", "VaiTro" },
                values: new object[] { 1, null, 1, "$2a$11$KWj.ShrxV4ZnlRTKcJZmWODKnW5JlDBA.n3P2pu68ifqc06xQcCve", new DateTime(2026, 3, 31, 21, 45, 54, 760, DateTimeKind.Local).AddTicks(8348), "admin", "Hoạt động", "Admin" });

            migrationBuilder.CreateIndex(
                name: "IX_BangLuong_MaNhanVien_Thang_Nam",
                table: "BangLuong",
                columns: new[] { "MaNhanVien", "Thang", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BangLuong_Thang_Nam",
                table: "BangLuong",
                columns: new[] { "Thang", "Nam" });

            migrationBuilder.CreateIndex(
                name: "IX_ChamCong_MaNhanVien",
                table: "ChamCong",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ChamCong_MaNhanVien_NgayChamCong",
                table: "ChamCong",
                columns: new[] { "MaNhanVien", "NgayChamCong" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChamCong_NgayChamCong",
                table: "ChamCong",
                column: "NgayChamCong");

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSach_NguoiPheDuyetNavMaNhanVien",
                table: "ChinhSach",
                column: "NguoiPheDuyetNavMaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ChungChi_MaNhanVien",
                table: "ChungChi",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ChungChi_NgayHetHan",
                table: "ChungChi",
                column: "NgayHetHan");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaDaoTao_MaDaoTao",
                table: "DanhGiaDaoTao",
                column: "MaDaoTao");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaDaoTao_MaNhanVien",
                table: "DanhGiaDaoTao",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhChamCong_MaChamCong",
                table: "DieuChinhChamCong",
                column: "MaChamCong");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhChamCong_NguoiDieuChinh",
                table: "DieuChinhChamCong",
                column: "NguoiDieuChinh");

            migrationBuilder.CreateIndex(
                name: "IX_DonNghiPhep_MaLoaiPhep",
                table: "DonNghiPhep",
                column: "MaLoaiPhep");

            migrationBuilder.CreateIndex(
                name: "IX_DonNghiPhep_MaNhanVien",
                table: "DonNghiPhep",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_DonNghiPhep_NguoiPheDuyet",
                table: "DonNghiPhep",
                column: "NguoiPheDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_DonNghiPhep_TrangThai",
                table: "DonNghiPhep",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_HieuSuatNhanVien_MaKyDanhGia",
                table: "HieuSuatNhanVien",
                column: "MaKyDanhGia");

            migrationBuilder.CreateIndex(
                name: "IX_HieuSuatNhanVien_MaNhanVien",
                table: "HieuSuatNhanVien",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_HieuSuatNhanVien_NguoiDanhGia",
                table: "HieuSuatNhanVien",
                column: "NguoiDanhGia");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuChinhSach_ChinhSachMaChinhSach",
                table: "LichSuChinhSach",
                column: "ChinhSachMaChinhSach");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuChinhSach_NguoiSuaDoiNavMaNhanVien",
                table: "LichSuChinhSach",
                column: "NguoiSuaDoiNavMaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDieuChuyen_MaChucVuCu",
                table: "LichSuDieuChuyen",
                column: "MaChucVuCu");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDieuChuyen_MaChucVuMoi",
                table: "LichSuDieuChuyen",
                column: "MaChucVuMoi");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDieuChuyen_MaNhanVien",
                table: "LichSuDieuChuyen",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDieuChuyen_MaPhongBanCu",
                table: "LichSuDieuChuyen",
                column: "MaPhongBanCu");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDieuChuyen_MaPhongBanMoi",
                table: "LichSuDieuChuyen",
                column: "MaPhongBanMoi");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDieuChuyen_NguoiThucHien",
                table: "LichSuDieuChuyen",
                column: "NguoiThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_CCCD",
                table: "NhanVien",
                column: "CCCD",
                unique: true,
                filter: "[CCCD] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_Email",
                table: "NhanVien",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_HoTen",
                table: "NhanVien",
                column: "HoTen");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaChucVu",
                table: "NhanVien",
                column: "MaChucVu");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaPhongBan",
                table: "NhanVien",
                column: "MaPhongBan");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_TaiKhoanMaTaiKhoan",
                table: "NhanVien",
                column: "TaiKhoanMaTaiKhoan");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_TrangThai",
                table: "NhanVien",
                column: "TrangThai");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongDaoTao_MaDaoTao_MaNhanVien",
                table: "PhanCongDaoTao",
                columns: new[] { "MaDaoTao", "MaNhanVien" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhanCongDaoTao_MaNhanVien",
                table: "PhanCongDaoTao",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_PhongBan_MaTruongPhong",
                table: "PhongBan",
                column: "MaTruongPhong");

            migrationBuilder.CreateIndex(
                name: "IX_PhongVan_MaUngVien",
                table: "PhongVan",
                column: "MaUngVien");

            migrationBuilder.CreateIndex(
                name: "IX_PhongVan_NguoiPhongVan",
                table: "PhongVan",
                column: "NguoiPhongVan");

            migrationBuilder.CreateIndex(
                name: "IX_PhuCapNhanVien_MaPhuCap",
                table: "PhuCapNhanVien",
                column: "MaPhuCap");

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhTuyenDung_MaUngVien",
                table: "QuyetDinhTuyenDung",
                column: "MaUngVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuyetDinhTuyenDung_NguoiQuyetDinh",
                table: "QuyetDinhTuyenDung",
                column: "NguoiQuyetDinh");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_MaNhanVien",
                table: "TaiKhoan",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuNhanVien_MaNhanVien",
                table: "TaiLieuNhanVien",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_MaNguoiNhan",
                table: "ThongBao",
                column: "MaNguoiNhan");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NhanVienMaNhanVien",
                table: "ThongBao",
                column: "NhanVienMaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ThuongPhat_MaNhanVien",
                table: "ThuongPhat",
                column: "MaNhanVien");

            migrationBuilder.CreateIndex(
                name: "IX_ThuongPhat_NguoiPheDuyet",
                table: "ThuongPhat",
                column: "NguoiPheDuyet");

            migrationBuilder.CreateIndex(
                name: "IX_TinTuyenDung_MaPhongBan",
                table: "TinTuyenDung",
                column: "MaPhongBan");

            migrationBuilder.CreateIndex(
                name: "IX_TinTuyenDung_NguoiTao",
                table: "TinTuyenDung",
                column: "NguoiTao");

            migrationBuilder.CreateIndex(
                name: "IX_UngVien_MaTinTuyenDung",
                table: "UngVien",
                column: "MaTinTuyenDung");

            migrationBuilder.CreateIndex(
                name: "IX_XacNhanChinhSach_ChinhSachMaChinhSach",
                table: "XacNhanChinhSach",
                column: "ChinhSachMaChinhSach");

            migrationBuilder.CreateIndex(
                name: "IX_XacNhanChinhSach_NhanVienMaNhanVien",
                table: "XacNhanChinhSach",
                column: "NhanVienMaNhanVien");

            migrationBuilder.AddForeignKey(
                name: "FK_BangLuong_NhanVien_MaNhanVien",
                table: "BangLuong",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChamCong_NhanVien_MaNhanVien",
                table: "ChamCong",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChinhSach_NhanVien_NguoiPheDuyetNavMaNhanVien",
                table: "ChinhSach",
                column: "NguoiPheDuyetNavMaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien");

            migrationBuilder.AddForeignKey(
                name: "FK_ChungChi_NhanVien_MaNhanVien",
                table: "ChungChi",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DanhGiaDaoTao_NhanVien_MaNhanVien",
                table: "DanhGiaDaoTao",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DieuChinhChamCong_NhanVien_NguoiDieuChinh",
                table: "DieuChinhChamCong",
                column: "NguoiDieuChinh",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonNghiPhep_NhanVien_MaNhanVien",
                table: "DonNghiPhep",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonNghiPhep_NhanVien_NguoiPheDuyet",
                table: "DonNghiPhep",
                column: "NguoiPheDuyet",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HieuSuatNhanVien_NhanVien_MaNhanVien",
                table: "HieuSuatNhanVien",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HieuSuatNhanVien_NhanVien_NguoiDanhGia",
                table: "HieuSuatNhanVien",
                column: "NguoiDanhGia",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuChinhSach_NhanVien_NguoiSuaDoiNavMaNhanVien",
                table: "LichSuChinhSach",
                column: "NguoiSuaDoiNavMaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien");

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuDieuChuyen_NhanVien_MaNhanVien",
                table: "LichSuDieuChuyen",
                column: "MaNhanVien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuDieuChuyen_NhanVien_NguoiThucHien",
                table: "LichSuDieuChuyen",
                column: "NguoiThucHien",
                principalTable: "NhanVien",
                principalColumn: "MaNhanVien",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuDieuChuyen_PhongBan_MaPhongBanCu",
                table: "LichSuDieuChuyen",
                column: "MaPhongBanCu",
                principalTable: "PhongBan",
                principalColumn: "MaPhongBan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LichSuDieuChuyen_PhongBan_MaPhongBanMoi",
                table: "LichSuDieuChuyen",
                column: "MaPhongBanMoi",
                principalTable: "PhongBan",
                principalColumn: "MaPhongBan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NhanVien_PhongBan_MaPhongBan",
                table: "NhanVien",
                column: "MaPhongBan",
                principalTable: "PhongBan",
                principalColumn: "MaPhongBan",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NhanVien_TaiKhoan_TaiKhoanMaTaiKhoan",
                table: "NhanVien",
                column: "TaiKhoanMaTaiKhoan",
                principalTable: "TaiKhoan",
                principalColumn: "MaTaiKhoan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhongBan_NhanVien_MaTruongPhong",
                table: "PhongBan");

            migrationBuilder.DropForeignKey(
                name: "FK_TaiKhoan_NhanVien_MaNhanVien",
                table: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "BangLuong");

            migrationBuilder.DropTable(
                name: "ChungChi");

            migrationBuilder.DropTable(
                name: "DanhGiaDaoTao");

            migrationBuilder.DropTable(
                name: "DieuChinhChamCong");

            migrationBuilder.DropTable(
                name: "DonNghiPhep");

            migrationBuilder.DropTable(
                name: "HieuSuatNhanVien");

            migrationBuilder.DropTable(
                name: "LichSuChinhSach");

            migrationBuilder.DropTable(
                name: "LichSuDieuChuyen");

            migrationBuilder.DropTable(
                name: "PhanCongDaoTao");

            migrationBuilder.DropTable(
                name: "PhongVan");

            migrationBuilder.DropTable(
                name: "PhuCapNhanVien");

            migrationBuilder.DropTable(
                name: "QuyetDinhTuyenDung");

            migrationBuilder.DropTable(
                name: "SoNgayPhep");

            migrationBuilder.DropTable(
                name: "TaiLieuNhanVien");

            migrationBuilder.DropTable(
                name: "ThongBao");

            migrationBuilder.DropTable(
                name: "ThuongPhat");

            migrationBuilder.DropTable(
                name: "XacNhanChinhSach");

            migrationBuilder.DropTable(
                name: "ChamCong");

            migrationBuilder.DropTable(
                name: "LoaiNghiPhep");

            migrationBuilder.DropTable(
                name: "KyDanhGia");

            migrationBuilder.DropTable(
                name: "ChuongTrinhDaoTao");

            migrationBuilder.DropTable(
                name: "LoaiPhuCap");

            migrationBuilder.DropTable(
                name: "UngVien");

            migrationBuilder.DropTable(
                name: "ChinhSach");

            migrationBuilder.DropTable(
                name: "TinTuyenDung");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "ChucVu");

            migrationBuilder.DropTable(
                name: "PhongBan");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
