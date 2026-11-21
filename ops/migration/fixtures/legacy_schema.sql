-- ============================================================
-- LEGACY HRM DATABASE SCHEMA (REHEARSAL & TEST FIXTURE)
-- Source: Based on legacy HRM_Database.sql
-- ============================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- Clean drop if tables exist in reverse dependency order
IF OBJECT_ID('dbo.BangLuong', 'U') IS NOT NULL DROP TABLE dbo.BangLuong;
IF OBJECT_ID('dbo.ThuongPhat', 'U') IS NOT NULL DROP TABLE dbo.ThuongPhat;
IF OBJECT_ID('dbo.PhuCapNhanVien', 'U') IS NOT NULL DROP TABLE dbo.PhuCapNhanVien;
IF OBJECT_ID('dbo.LoaiPhuCap', 'U') IS NOT NULL DROP TABLE dbo.LoaiPhuCap;
IF OBJECT_ID('dbo.DonNghiPhep', 'U') IS NOT NULL DROP TABLE dbo.DonNghiPhep;
IF OBJECT_ID('dbo.SoNgayPhep', 'U') IS NOT NULL DROP TABLE dbo.SoNgayPhep;
IF OBJECT_ID('dbo.LoaiNghiPhep', 'U') IS NOT NULL DROP TABLE dbo.LoaiNghiPhep;
IF OBJECT_ID('dbo.ChamCong', 'U') IS NOT NULL DROP TABLE dbo.ChamCong;
IF OBJECT_ID('dbo.TaiKhoan', 'U') IS NOT NULL DROP TABLE dbo.TaiKhoan;
IF OBJECT_ID('dbo.NhanVien', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PhongBan DROP CONSTRAINT IF EXISTS FK_PhongBan_TruongPhong;
    DROP TABLE dbo.NhanVien;
END;
IF OBJECT_ID('dbo.ChucVu', 'U') IS NOT NULL DROP TABLE dbo.ChucVu;
IF OBJECT_ID('dbo.PhongBan', 'U') IS NOT NULL DROP TABLE dbo.PhongBan;

-- 1. PhongBan
CREATE TABLE dbo.PhongBan (
    MaPhongBan      INT IDENTITY(1,1) PRIMARY KEY,
    TenPhongBan     NVARCHAR(100)   NOT NULL,
    MoTaChucNang    NVARCHAR(500),
    NgayThanhLap    DATE,
    DiaDiemLamViec  NVARCHAR(200),
    NganSach        DECIMAL(18,2),
    TrangThai       NVARCHAR(20)    NOT NULL DEFAULT N'Hoạt động',
    MaTruongPhong   INT             NULL,
    NgayTao         DATETIME        DEFAULT GETDATE(),
    NgayCapNhat     DATETIME        DEFAULT GETDATE()
);

-- 2. ChucVu
CREATE TABLE dbo.ChucVu (
    MaChucVu    INT IDENTITY(1,1) PRIMARY KEY,
    TenChucVu   NVARCHAR(100)   NOT NULL,
    MoTa        NVARCHAR(500),
    CapBac      INT             DEFAULT 1
);

-- 3. NhanVien
CREATE TABLE dbo.NhanVien (
    MaNhanVien      INT IDENTITY(1,1) PRIMARY KEY,
    MaNV            AS ('NV' + RIGHT('00000' + CAST(MaNhanVien AS VARCHAR(5)), 5)) PERSISTED,
    HoTen           NVARCHAR(100)   NOT NULL,
    NgaySinh        DATE            NOT NULL,
    GioiTinh        NVARCHAR(10),
    CCCD            VARCHAR(12)     NULL,
    DiaChi          NVARCHAR(300),
    SoDienThoai     VARCHAR(15),
    Email           VARCHAR(100)    NULL,
    TinhTrangHonNhan NVARCHAR(20),
    MaPhongBan      INT             NULL,
    MaChucVu        INT             NULL,
    NgayVaoLam      DATE            NOT NULL,
    MucLuong        DECIMAL(18,2)   NOT NULL,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Đang làm việc',
    NgayNghiViec    DATE            NULL,
    AnhDaiDien      NVARCHAR(500),
    NgayTao         DATETIME        DEFAULT GETDATE(),
    NgayCapNhat     DATETIME        DEFAULT GETDATE()
);

-- 4. TaiKhoan
CREATE TABLE dbo.TaiKhoan (
    MaTaiKhoan      INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    TenDangNhap     VARCHAR(50)     NOT NULL,
    MatKhauHash     VARCHAR(256)    NOT NULL,
    VaiTro          NVARCHAR(30)    NOT NULL,
    TrangThai       NVARCHAR(20)    DEFAULT N'Hoạt động',
    LanDangNhapCuoi DATETIME,
    NgayTao         DATETIME        DEFAULT GETDATE()
);

-- 5. ChamCong
CREATE TABLE dbo.ChamCong (
    MaChamCong      INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    NgayChamCong    DATE            NOT NULL,
    GioVao          TIME            NULL,
    GioRa           TIME            NULL,
    TongGioLam      DECIMAL(5,2)    NULL,
    GioLamThem      DECIMAL(5,2)    DEFAULT 0,
    HinhThuc        NVARCHAR(30),
    TrangThai       NVARCHAR(30)    DEFAULT N'Bình thường',
    GhiChu          NVARCHAR(500)
);

-- 6. LoaiNghiPhep
CREATE TABLE dbo.LoaiNghiPhep (
    MaLoaiPhep  INT IDENTITY(1,1) PRIMARY KEY,
    TenLoaiPhep NVARCHAR(100)   NOT NULL,
    MoTa        NVARCHAR(300),
    CoLuong     BIT             DEFAULT 1
);

-- 7. SoNgayPhep
CREATE TABLE dbo.SoNgayPhep (
    MaNhanVien      INT     NOT NULL,
    Nam             INT     NOT NULL,
    TongSoNgayPhep  INT     NOT NULL DEFAULT 12,
    SoNgayDaSuDung  INT     NOT NULL DEFAULT 0,
    PhepNamCuConLai INT     DEFAULT 0,
    CONSTRAINT PK_SoNgayPhep PRIMARY KEY (MaNhanVien, Nam)
);

-- 8. DonNghiPhep
CREATE TABLE dbo.DonNghiPhep (
    MaDonPhep       INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    MaLoaiPhep      INT             NOT NULL,
    NgayBatDau      DATE            NOT NULL,
    NgayKetThuc     DATE            NOT NULL,
    SoNgayNghi      DECIMAL(3,1)    NOT NULL,
    LyDo            NVARCHAR(500)   NOT NULL,
    TrangThai       NVARCHAR(20)    NOT NULL DEFAULT N'Chờ duyệt',
    NguoiPheDuyet   INT             NULL,
    NgayPheDuyet    DATETIME        NULL,
    LyDoTuChoi      NVARCHAR(500),
    NgayTao         DATETIME        DEFAULT GETDATE()
);

-- 9. LoaiPhuCap
CREATE TABLE dbo.LoaiPhuCap (
    MaPhuCap    INT IDENTITY(1,1) PRIMARY KEY,
    TenPhuCap   NVARCHAR(100)   NOT NULL,
    MoTa        NVARCHAR(300),
    SoTien      DECIMAL(18,2)   NOT NULL
);

-- 10. PhuCapNhanVien
CREATE TABLE dbo.PhuCapNhanVien (
    MaNhanVien  INT NOT NULL,
    MaPhuCap    INT NOT NULL,
    NgayApDung  DATE NOT NULL,
    NgayKetThuc DATE NULL,
    CONSTRAINT PK_PhuCapNV PRIMARY KEY (MaNhanVien, MaPhuCap, NgayApDung)
);

-- 11. ThuongPhat
CREATE TABLE dbo.ThuongPhat (
    MaThuongPhat    INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    Loai            NVARCHAR(10)    NOT NULL,
    LoaiChiTiet     NVARCHAR(100),
    LyDo            NVARCHAR(500)   NOT NULL,
    SoTien          DECIMAL(18,2)   NOT NULL,
    NgayApDung      DATE            NOT NULL,
    NguoiPheDuyet   INT             NULL,
    TrangThai       NVARCHAR(20)    DEFAULT N'Chờ duyệt'
);

-- 12. BangLuong
CREATE TABLE dbo.BangLuong (
    MaBangLuong     INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    Thang           INT             NOT NULL,
    Nam             INT             NOT NULL,
    LuongCoBan      DECIMAL(18,2)   NOT NULL,
    TongPhuCap      DECIMAL(18,2)   DEFAULT 0,
    SoNgayLamViec   INT             DEFAULT 0,
    SoGioLamThem    DECIMAL(5,2)    DEFAULT 0,
    TienLamThem     DECIMAL(18,2)   DEFAULT 0,
    TongThuong      DECIMAL(18,2)   DEFAULT 0,
    TongPhat        DECIMAL(18,2)   DEFAULT 0,
    BHXH            DECIMAL(18,2)   DEFAULT 0,
    BHYT            DECIMAL(18,2)   DEFAULT 0,
    BHTN            DECIMAL(18,2)   DEFAULT 0,
    ThueTNCN        DECIMAL(18,2)   DEFAULT 0,
    TongThuNhap     DECIMAL(18,2),
    TongKhauTru     DECIMAL(18,2),
    LuongThucNhan   DECIMAL(18,2),
    NgayTinhLuong   DATETIME        DEFAULT GETDATE(),
    TrangThai       NVARCHAR(20)    DEFAULT N'Chờ duyệt'
);
