-- ============================================================
-- CƠ SỞ DỮ LIỆU HỆ THỐNG QUẢN LÝ NHÂN SỰ (HRM)
-- Thiết kế dựa trên tài liệu MoTa_UseCase_HRM
-- ============================================================

-- Xóa database cũ nếu tồn tại
DROP DATABASE IF EXISTS HRM_System;
GO

CREATE DATABASE HRM_System;
GO

USE HRM_System;
GO

-- ============================================================
-- MODULE 7: QUẢN LÝ PHÒNG BAN
-- ============================================================

CREATE TABLE PhongBan (
    MaPhongBan      INT IDENTITY(1,1) PRIMARY KEY,
    TenPhongBan     NVARCHAR(100)   NOT NULL,
    MoTaChucNang    NVARCHAR(500),
    NgayThanhLap    DATE,
    DiaDiemLamViec  NVARCHAR(200),
    NganSach        DECIMAL(18,2),
    TrangThai       NVARCHAR(20)    NOT NULL DEFAULT N'Hoạt động',
    -- TruongPhong sẽ được thêm FK sau khi tạo bảng NhanVien
    MaTruongPhong   INT             NULL,
    NgayTao         DATETIME        DEFAULT GETDATE(),
    NgayCapNhat     DATETIME        DEFAULT GETDATE()
);

CREATE TABLE ChucVu (
    MaChucVu    INT IDENTITY(1,1) PRIMARY KEY,
    TenChucVu   NVARCHAR(100)   NOT NULL,
    MoTa        NVARCHAR(500),
    CapBac      INT             DEFAULT 1
);

-- ============================================================
-- MODULE 1: QUẢN LÝ THÔNG TIN NHÂN VIÊN
-- ============================================================

CREATE TABLE NhanVien (
    MaNhanVien      INT IDENTITY(1,1) PRIMARY KEY,
    MaNV            AS ('NV' + RIGHT('00000' + CAST(MaNhanVien AS VARCHAR(5)), 5)) PERSISTED,
    HoTen           NVARCHAR(100)   NOT NULL,
    NgaySinh        DATE            NOT NULL,
    GioiTinh        NVARCHAR(10),
    CCCD            VARCHAR(12)     UNIQUE,
    DiaChi          NVARCHAR(300),
    SoDienThoai     VARCHAR(15),
    Email           VARCHAR(100)    UNIQUE,
    TinhTrangHonNhan NVARCHAR(20),
    MaPhongBan      INT,
    MaChucVu        INT,
    NgayVaoLam      DATE            NOT NULL,
    MucLuong        DECIMAL(18,2)   NOT NULL,
    TrangThai       NVARCHAR(30)    NOT NULL DEFAULT N'Đang làm việc',
    NgayNghiViec    DATE            NULL,
    AnhDaiDien      NVARCHAR(500),
    NgayTao         DATETIME        DEFAULT GETDATE(),
    NgayCapNhat     DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_NhanVien_PhongBan FOREIGN KEY (MaPhongBan) REFERENCES PhongBan(MaPhongBan),
    CONSTRAINT FK_NhanVien_ChucVu FOREIGN KEY (MaChucVu) REFERENCES ChucVu(MaChucVu),
    CONSTRAINT CK_TrangThai_NV CHECK (TrangThai IN (N'Đang làm việc', N'Nghỉ việc', N'Nghỉ phép dài hạn', N'Thử việc'))
);

-- Thêm FK TruongPhong cho PhongBan
ALTER TABLE PhongBan
ADD CONSTRAINT FK_PhongBan_TruongPhong FOREIGN KEY (MaTruongPhong) REFERENCES NhanVien(MaNhanVien);

-- Tài liệu đính kèm nhân viên (bằng cấp, CV, hợp đồng lao động)
CREATE TABLE TaiLieuNhanVien (
    MaTaiLieu       INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    LoaiTaiLieu     NVARCHAR(50)    NOT NULL,
    TenTaiLieu      NVARCHAR(200)   NOT NULL,
    DuongDanFile    NVARCHAR(500)   NOT NULL,
    NgayTaiLen      DATETIME        DEFAULT GETDATE(),
    GhiChu          NVARCHAR(500),

    CONSTRAINT FK_TaiLieu_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_LoaiTaiLieu CHECK (LoaiTaiLieu IN (N'Bằng cấp', N'CV', N'Hợp đồng lao động', N'Chứng chỉ', N'Khác'))
);

-- Lịch sử điều chuyển phòng ban / chức vụ
CREATE TABLE LichSuDieuChuyen (
    MaLichSu        INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    MaPhongBanCu    INT,
    MaPhongBanMoi   INT,
    MaChucVuCu      INT,
    MaChucVuMoi     INT,
    MucLuongCu      DECIMAL(18,2),
    MucLuongMoi     DECIMAL(18,2),
    NgayThayDoi     DATE            NOT NULL,
    LyDo            NVARCHAR(500),
    NguoiThucHien   INT,

    CONSTRAINT FK_LSDC_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_LSDC_PhongBanCu FOREIGN KEY (MaPhongBanCu) REFERENCES PhongBan(MaPhongBan),
    CONSTRAINT FK_LSDC_PhongBanMoi FOREIGN KEY (MaPhongBanMoi) REFERENCES PhongBan(MaPhongBan),
    CONSTRAINT FK_LSDC_ChucVuCu FOREIGN KEY (MaChucVuCu) REFERENCES ChucVu(MaChucVu),
    CONSTRAINT FK_LSDC_ChucVuMoi FOREIGN KEY (MaChucVuMoi) REFERENCES ChucVu(MaChucVu),
    CONSTRAINT FK_LSDC_NguoiThucHien FOREIGN KEY (NguoiThucHien) REFERENCES NhanVien(MaNhanVien)
);

-- ============================================================
-- MODULE 2: QUẢN LÝ CHẤM CÔNG
-- ============================================================

CREATE TABLE ChamCong (
    MaChamCong      INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    NgayChamCong    DATE            NOT NULL,
    GioVao          TIME,
    GioRa           TIME,
    TongGioLam      DECIMAL(5,2),
    GioLamThem      DECIMAL(5,2)    DEFAULT 0,
    HinhThuc        NVARCHAR(30),
    TrangThai       NVARCHAR(30)    DEFAULT N'Bình thường',
    GhiChu          NVARCHAR(500),

    CONSTRAINT FK_ChamCong_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_HinhThuc_CC CHECK (HinhThuc IN (N'Thẻ từ', N'Vân tay', N'Khuôn mặt', N'Mobile', N'Khác')),
    CONSTRAINT CK_TrangThai_CC CHECK (TrangThai IN (N'Bình thường', N'Đi muộn', N'Về sớm', N'Đi muộn và về sớm', N'Vắng mặt', N'Công tác', N'Nghỉ phép')),
    CONSTRAINT UQ_ChamCong UNIQUE (MaNhanVien, NgayChamCong)
);

CREATE TABLE DieuChinhChamCong (
    MaDieuChinh     INT IDENTITY(1,1) PRIMARY KEY,
    MaChamCong      INT             NOT NULL,
    NguoiDieuChinh  INT             NOT NULL,
    GioVaoCu        TIME,
    GioRaCu         TIME,
    GioVaoMoi       TIME,
    GioRaMoi        TIME,
    LyDo            NVARCHAR(500)   NOT NULL,
    NgayDieuChinh   DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_DCCC_ChamCong FOREIGN KEY (MaChamCong) REFERENCES ChamCong(MaChamCong),
    CONSTRAINT FK_DCCC_NguoiDieuChinh FOREIGN KEY (NguoiDieuChinh) REFERENCES NhanVien(MaNhanVien)
);

-- ============================================================
-- MODULE 3: QUẢN LÝ LƯƠNG
-- ============================================================

CREATE TABLE LoaiPhuCap (
    MaPhuCap    INT IDENTITY(1,1) PRIMARY KEY,
    TenPhuCap   NVARCHAR(100)   NOT NULL,
    MoTa        NVARCHAR(300),
    SoTien      DECIMAL(18,2)   NOT NULL
);

CREATE TABLE PhuCapNhanVien (
    MaNhanVien  INT NOT NULL,
    MaPhuCap    INT NOT NULL,
    NgayApDung  DATE NOT NULL,
    NgayKetThuc DATE,

    CONSTRAINT PK_PhuCapNV PRIMARY KEY (MaNhanVien, MaPhuCap, NgayApDung),
    CONSTRAINT FK_PCNV_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_PCNV_PhuCap FOREIGN KEY (MaPhuCap) REFERENCES LoaiPhuCap(MaPhuCap)
);

CREATE TABLE ThuongPhat (
    MaThuongPhat    INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    Loai            NVARCHAR(10)    NOT NULL,
    LoaiChiTiet     NVARCHAR(100),
    LyDo            NVARCHAR(500)   NOT NULL,
    SoTien          DECIMAL(18,2)   NOT NULL,
    NgayApDung      DATE            NOT NULL,
    NguoiPheDuyet   INT,
    TrangThai       NVARCHAR(20)    DEFAULT N'Chờ duyệt',

    CONSTRAINT FK_TP_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_TP_NguoiPheDuyet FOREIGN KEY (NguoiPheDuyet) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_Loai_TP CHECK (Loai IN (N'Thưởng', N'Phạt'))
);

CREATE TABLE BangLuong (
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
    TrangThai       NVARCHAR(20)    DEFAULT N'Chờ duyệt',

    CONSTRAINT FK_BL_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT UQ_BangLuong UNIQUE (MaNhanVien, Thang, Nam),
    CONSTRAINT CK_Thang CHECK (Thang BETWEEN 1 AND 12)
);

-- ============================================================
-- MODULE 4: QUẢN LÝ NGHỈ PHÉP
-- ============================================================

CREATE TABLE LoaiNghiPhep (
    MaLoaiPhep  INT IDENTITY(1,1) PRIMARY KEY,
    TenLoaiPhep NVARCHAR(100)   NOT NULL,
    MoTa        NVARCHAR(300),
    CoLuong     BIT             DEFAULT 1
);

CREATE TABLE SoNgayPhep (
    MaNhanVien      INT     NOT NULL,
    Nam             INT     NOT NULL,
    TongSoNgayPhep  INT     NOT NULL DEFAULT 12,
    SoNgayDaSuDung  INT     NOT NULL DEFAULT 0,
    SoNgayConLai    AS (TongSoNgayPhep - SoNgayDaSuDung),
    PhepNamCuConLai INT     DEFAULT 0,

    CONSTRAINT PK_SoNgayPhep PRIMARY KEY (MaNhanVien, Nam),
    CONSTRAINT FK_SNP_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE DonNghiPhep (
    MaDonPhep       INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    MaLoaiPhep      INT             NOT NULL,
    NgayBatDau      DATE            NOT NULL,
    NgayKetThuc     DATE            NOT NULL,
    SoNgayNghi      DECIMAL(3,1)    NOT NULL,
    LyDo            NVARCHAR(500)   NOT NULL,
    TrangThai       NVARCHAR(20)    NOT NULL DEFAULT N'Chờ duyệt',
    NguoiPheDuyet   INT,
    NgayPheDuyet    DATETIME,
    LyDoTuChoi      NVARCHAR(500),
    NgayTao         DATETIME        DEFAULT GETDATE(),
    NgayHuy         DATETIME,
    LyDoHuy         NVARCHAR(500),

    CONSTRAINT FK_DNP_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_DNP_LoaiPhep FOREIGN KEY (MaLoaiPhep) REFERENCES LoaiNghiPhep(MaLoaiPhep),
    CONSTRAINT FK_DNP_NguoiDuyet FOREIGN KEY (NguoiPheDuyet) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_TrangThai_DNP CHECK (TrangThai IN (N'Chờ duyệt', N'Đã duyệt', N'Từ chối', N'Đã hủy')),
    CONSTRAINT CK_NgayNghi CHECK (NgayKetThuc >= NgayBatDau)
);

-- ============================================================
-- MODULE 5: BÁO CÁO VÀ THỐNG KÊ - HIỆU SUẤT NHÂN VIÊN
-- ============================================================

CREATE TABLE KyDanhGia (
    MaKyDanhGia     INT IDENTITY(1,1) PRIMARY KEY,
    TenKyDanhGia    NVARCHAR(100)   NOT NULL,
    NgayBatDau      DATE            NOT NULL,
    NgayKetThuc     DATE            NOT NULL,
    TrangThai       NVARCHAR(20)    DEFAULT N'Mở'
);

CREATE TABLE HieuSuatNhanVien (
    MaHieuSuat          INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien          INT             NOT NULL,
    MaKyDanhGia         INT             NOT NULL,
    DiemKPI             DECIMAL(5,2),
    KetQuaCongViec      NVARCHAR(1000),
    TyLeHoanThanhDeadline DECIMAL(5,2),
    SoGioLamViec        DECIMAL(7,2),
    XepHang             NVARCHAR(20),
    NguoiDanhGia        INT,
    NgayDanhGia         DATETIME        DEFAULT GETDATE(),
    GhiChu              NVARCHAR(500),

    CONSTRAINT FK_HS_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT FK_HS_KyDanhGia FOREIGN KEY (MaKyDanhGia) REFERENCES KyDanhGia(MaKyDanhGia),
    CONSTRAINT FK_HS_NguoiDanhGia FOREIGN KEY (NguoiDanhGia) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_XepHang CHECK (XepHang IN (N'Xuất sắc', N'Tốt', N'Khá', N'Trung bình', N'Yếu'))
);

-- ============================================================
-- MODULE 6: QUẢN LÝ ĐÀO TẠO
-- ============================================================

CREATE TABLE ChuongTrinhDaoTao (
    MaDaoTao        INT IDENTITY(1,1) PRIMARY KEY,
    TenKhoaHoc      NVARCHAR(200)   NOT NULL,
    MucTieu         NVARCHAR(1000),
    NoiDung         NVARCHAR(MAX),
    ThoiLuong       INT,
    GiangVien       NVARCHAR(200),
    DiaDiem         NVARCHAR(200),
    ChiPhi          DECIMAL(18,2),
    SoHocVienToiDa  INT,
    NgayBatDau      DATE,
    NgayKetThuc     DATE,
    TrangThai       NVARCHAR(30)    DEFAULT N'Lên kế hoạch',
    NgayTao         DATETIME        DEFAULT GETDATE(),

    CONSTRAINT CK_TrangThai_DT CHECK (TrangThai IN (N'Lên kế hoạch', N'Đang diễn ra', N'Hoàn thành', N'Hủy'))
);

CREATE TABLE PhanCongDaoTao (
    MaPhanCong      INT IDENTITY(1,1) PRIMARY KEY,
    MaDaoTao        INT             NOT NULL,
    MaNhanVien      INT             NOT NULL,
    NgayDangKy      DATETIME        DEFAULT GETDATE(),
    TyLeThamDu      DECIMAL(5,2)    DEFAULT 0,
    KetQuaKiemTra   DECIMAL(5,2),
    PhanHoi         NVARCHAR(1000),
    TrangThai       NVARCHAR(30)    DEFAULT N'Đã đăng ký',

    CONSTRAINT FK_PCDT_DaoTao FOREIGN KEY (MaDaoTao) REFERENCES ChuongTrinhDaoTao(MaDaoTao),
    CONSTRAINT FK_PCDT_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT UQ_PhanCongDT UNIQUE (MaDaoTao, MaNhanVien),
    CONSTRAINT CK_TrangThai_PCDT CHECK (TrangThai IN (N'Đã đăng ký', N'Đang học', N'Hoàn thành', N'Không đạt', N'Hủy'))
);

CREATE TABLE DanhGiaDaoTao (
    MaDanhGia       INT IDENTITY(1,1) PRIMARY KEY,
    MaDaoTao        INT             NOT NULL,
    MaNhanVien      INT             NOT NULL,
    DiemSo          DECIMAL(5,2),
    DanhGiaGiangVien NVARCHAR(1000),
    PhanHoiHocVien  NVARCHAR(1000),
    ChatLuongKhoaHoc INT,
    NgayDanhGia     DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_DGDT_DaoTao FOREIGN KEY (MaDaoTao) REFERENCES ChuongTrinhDaoTao(MaDaoTao),
    CONSTRAINT FK_DGDT_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_ChatLuong CHECK (ChatLuongKhoaHoc BETWEEN 1 AND 5)
);

CREATE TABLE ChungChi (
    MaChungChi      INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL,
    TenChungChi     NVARCHAR(200)   NOT NULL,
    LoaiChungChi    NVARCHAR(50),
    ToChucCap       NVARCHAR(200),
    NgayCap         DATE            NOT NULL,
    NgayHetHan      DATE,
    DuongDanFile    NVARCHAR(500),
    GhiChu          NVARCHAR(500),

    CONSTRAINT FK_CC_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_LoaiCC CHECK (LoaiChungChi IN (N'Chuyên môn', N'Ngoại ngữ', N'Tin học', N'Kỹ năng mềm', N'Khác'))
);

-- ============================================================
-- MODULE 8: QUẢN LÝ TUYỂN DỤNG
-- ============================================================

CREATE TABLE TinTuyenDung (
    MaTinTuyenDung  INT IDENTITY(1,1) PRIMARY KEY,
    ViTriTuyenDung  NVARCHAR(200)   NOT NULL,
    MaPhongBan      INT,
    MoTaCongViec    NVARCHAR(MAX),
    YeuCauUngVien   NVARCHAR(MAX),
    SoLuongCanTuyen INT             NOT NULL DEFAULT 1,
    MucLuongMin     DECIMAL(18,2),
    MucLuongMax     DECIMAL(18,2),
    ThoiHanNhanHoSo DATE,
    DiadiemLamViec  NVARCHAR(200),
    TrangThai       NVARCHAR(30)    DEFAULT N'Đang tuyển',
    NguoiTao        INT,
    NgayDang        DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_TTD_PhongBan FOREIGN KEY (MaPhongBan) REFERENCES PhongBan(MaPhongBan),
    CONSTRAINT FK_TTD_NguoiTao FOREIGN KEY (NguoiTao) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_TrangThai_TTD CHECK (TrangThai IN (N'Đang tuyển', N'Tạm dừng', N'Đã đóng', N'Hoàn thành'))
);

CREATE TABLE UngVien (
    MaUngVien       INT IDENTITY(1,1) PRIMARY KEY,
    MaTinTuyenDung  INT             NOT NULL,
    HoTen           NVARCHAR(100)   NOT NULL,
    Email           VARCHAR(100),
    SoDienThoai     VARCHAR(15),
    DuongDanCV      NVARCHAR(500),
    DuongDanThuXinViec NVARCHAR(500),
    KinhNghiem      NVARCHAR(500),
    BangCap         NVARCHAR(200),
    KyNang          NVARCHAR(500),
    PhanLoai        NVARCHAR(30)    DEFAULT N'Chờ xem xét',
    TrangThai       NVARCHAR(30)    DEFAULT N'Mới nộp',
    GhiChu          NVARCHAR(500),
    NgayNop         DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_UV_TinTuyenDung FOREIGN KEY (MaTinTuyenDung) REFERENCES TinTuyenDung(MaTinTuyenDung),
    CONSTRAINT CK_PhanLoai_UV CHECK (PhanLoai IN (N'Chờ xem xét', N'Phù hợp', N'Cần xem xét thêm', N'Không phù hợp')),
    CONSTRAINT CK_TrangThai_UV CHECK (TrangThai IN (N'Mới nộp', N'Đang sàng lọc', N'Chờ phỏng vấn', N'Đã phỏng vấn', N'Trúng tuyển', N'Không đạt', N'Từ chối offer'))
);

CREATE TABLE PhongVan (
    MaPhongVan      INT IDENTITY(1,1) PRIMARY KEY,
    MaUngVien       INT             NOT NULL,
    VongPhongVan    INT             DEFAULT 1,
    NgayPhongVan    DATETIME        NOT NULL,
    DiaDiem         NVARCHAR(200),
    NguoiPhongVan   INT,
    CauHoiPhongVan  NVARCHAR(MAX),
    KetQua          NVARCHAR(20),
    DiemDanhGia     DECIMAL(5,2),
    NhanXet         NVARCHAR(1000),
    TrangThai       NVARCHAR(20)    DEFAULT N'Đã lên lịch',

    CONSTRAINT FK_PV_UngVien FOREIGN KEY (MaUngVien) REFERENCES UngVien(MaUngVien),
    CONSTRAINT FK_PV_NguoiPV FOREIGN KEY (NguoiPhongVan) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_KetQua_PV CHECK (KetQua IN (N'Đạt', N'Không đạt', N'Chờ xem xét')),
    CONSTRAINT CK_TrangThai_PV CHECK (TrangThai IN (N'Đã lên lịch', N'Đã phỏng vấn', N'Hủy'))
);

CREATE TABLE QuyetDinhTuyenDung (
    MaQuyetDinh     INT IDENTITY(1,1) PRIMARY KEY,
    MaUngVien       INT             NOT NULL,
    KetQua          NVARCHAR(20)    NOT NULL,
    NgayQuyetDinh   DATETIME        DEFAULT GETDATE(),
    NguoiQuyetDinh  INT,
    MucLuongDeXuat  DECIMAL(18,2),
    NgayBatDauLamViec DATE,
    DaGuiOfferLetter BIT            DEFAULT 0,
    PhanHoiUngVien  NVARCHAR(20),
    GhiChu          NVARCHAR(500),

    CONSTRAINT FK_QDTD_UngVien FOREIGN KEY (MaUngVien) REFERENCES UngVien(MaUngVien),
    CONSTRAINT FK_QDTD_NguoiQD FOREIGN KEY (NguoiQuyetDinh) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_KetQua_QD CHECK (KetQua IN (N'Trúng tuyển', N'Không đạt')),
    CONSTRAINT CK_PhanHoi_UV CHECK (PhanHoiUngVien IN (N'Chấp nhận', N'Từ chối', N'Chờ phản hồi'))
);

-- ============================================================
-- MODULE 9: QUẢN LÝ CHÍNH SÁCH
-- ============================================================

CREATE TABLE ChinhSach (
    MaChinhSach     INT IDENTITY(1,1) PRIMARY KEY,
    TenChinhSach    NVARCHAR(200)   NOT NULL,
    LoaiChinhSach   NVARCHAR(100),
    NoiDung         NVARCHAR(MAX)   NOT NULL,
    PhamViApDung    NVARCHAR(200),
    NgayHieuLuc     DATE            NOT NULL,
    NgayHetHieuLuc  DATE,
    PhienBan        INT             DEFAULT 1,
    NguoiPheDuyet   INT,
    TrangThai       NVARCHAR(20)    DEFAULT N'Bản nháp',
    NgayTao         DATETIME        DEFAULT GETDATE(),
    NgayCapNhat     DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_CS_NguoiDuyet FOREIGN KEY (NguoiPheDuyet) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_LoaiCS CHECK (LoaiChinhSach IN (N'Lương thưởng', N'Phúc lợi', N'Quy định làm việc', N'Nội quy', N'Khác')),
    CONSTRAINT CK_TrangThai_CS CHECK (TrangThai IN (N'Bản nháp', N'Đã công bố', N'Hết hiệu lực'))
);

CREATE TABLE LichSuChinhSach (
    MaLichSu        INT IDENTITY(1,1) PRIMARY KEY,
    MaChinhSach     INT             NOT NULL,
    PhienBanCu      INT,
    PhienBanMoi     INT,
    NoiDungThayDoi  NVARCHAR(MAX),
    LyDoSuaDoi      NVARCHAR(500)   NOT NULL,
    NguoiSuaDoi     INT,
    NgaySuaDoi      DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_LSCS_ChinhSach FOREIGN KEY (MaChinhSach) REFERENCES ChinhSach(MaChinhSach),
    CONSTRAINT FK_LSCS_NguoiSua FOREIGN KEY (NguoiSuaDoi) REFERENCES NhanVien(MaNhanVien)
);

CREATE TABLE XacNhanChinhSach (
    MaXacNhan       INT IDENTITY(1,1) PRIMARY KEY,
    MaChinhSach     INT             NOT NULL,
    MaNhanVien      INT             NOT NULL,
    DaDoc           BIT             DEFAULT 0,
    NgayXacNhan     DATETIME,

    CONSTRAINT FK_XNCS_ChinhSach FOREIGN KEY (MaChinhSach) REFERENCES ChinhSach(MaChinhSach),
    CONSTRAINT FK_XNCS_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT UQ_XacNhanCS UNIQUE (MaChinhSach, MaNhanVien)
);

-- ============================================================
-- QUẢN LÝ TÀI KHOẢN HỆ THỐNG
-- ============================================================

CREATE TABLE TaiKhoan (
    MaTaiKhoan      INT IDENTITY(1,1) PRIMARY KEY,
    MaNhanVien      INT             NOT NULL UNIQUE,
    TenDangNhap     VARCHAR(50)     NOT NULL UNIQUE,
    MatKhauHash     VARCHAR(256)    NOT NULL,
    VaiTro          NVARCHAR(30)    NOT NULL,
    TrangThai       NVARCHAR(20)    DEFAULT N'Hoạt động',
    LanDangNhapCuoi DATETIME,
    NgayTao         DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_TK_NhanVien FOREIGN KEY (MaNhanVien) REFERENCES NhanVien(MaNhanVien),
    CONSTRAINT CK_VaiTro CHECK (VaiTro IN (N'Admin', N'Phòng Nhân sự', N'Quản lý', N'Nhân viên'))
);

-- ============================================================
-- BẢNG THÔNG BÁO HỆ THỐNG
-- ============================================================

CREATE TABLE ThongBao (
    MaThongBao      INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiNhan     INT             NOT NULL,
    TieuDe          NVARCHAR(200)   NOT NULL,
    NoiDung         NVARCHAR(MAX),
    LoaiThongBao    NVARCHAR(50),
    DaDoc           BIT             DEFAULT 0,
    NgayTao         DATETIME        DEFAULT GETDATE(),

    CONSTRAINT FK_TB_NguoiNhan FOREIGN KEY (MaNguoiNhan) REFERENCES NhanVien(MaNhanVien)
);

-- ============================================================
-- CÁC INDEX TĂNG HIỆU NĂNG TRUY VẤN
-- ============================================================

CREATE INDEX IX_NhanVien_PhongBan ON NhanVien(MaPhongBan);
CREATE INDEX IX_NhanVien_ChucVu ON NhanVien(MaChucVu);
CREATE INDEX IX_NhanVien_TrangThai ON NhanVien(TrangThai);
CREATE INDEX IX_NhanVien_HoTen ON NhanVien(HoTen);
CREATE INDEX IX_ChamCong_NgayChamCong ON ChamCong(NgayChamCong);
CREATE INDEX IX_ChamCong_NhanVien ON ChamCong(MaNhanVien);
CREATE INDEX IX_BangLuong_ThangNam ON BangLuong(Thang, Nam);
CREATE INDEX IX_DonNghiPhep_TrangThai ON DonNghiPhep(TrangThai);
CREATE INDEX IX_DonNghiPhep_NhanVien ON DonNghiPhep(MaNhanVien);
CREATE INDEX IX_UngVien_TinTuyenDung ON UngVien(MaTinTuyenDung);
CREATE INDEX IX_ChungChi_NgayHetHan ON ChungChi(NgayHetHan);
CREATE INDEX IX_ThongBao_NguoiNhan ON ThongBao(MaNguoiNhan, DaDoc);

-- ============================================================
-- DỮ LIỆU MẪU
-- ============================================================

-- Chức vụ
INSERT INTO ChucVu (TenChucVu, MoTa, CapBac) VALUES
(N'Giám đốc', N'Giám đốc điều hành công ty', 6),
(N'Phó Giám đốc', N'Phó Giám đốc', 5),
(N'Trưởng phòng', N'Trưởng phòng ban', 4),
(N'Phó phòng', N'Phó phòng ban', 3),
(N'Trưởng nhóm', N'Trưởng nhóm làm việc', 2),
(N'Nhân viên', N'Nhân viên', 1);

-- Phòng ban
INSERT INTO PhongBan (TenPhongBan, MoTaChucNang, NgayThanhLap, DiaDiemLamViec, TrangThai) VALUES
(N'Ban Giám đốc', N'Điều hành toàn bộ hoạt động công ty', '2020-01-01', N'Tầng 10', N'Hoạt động'),
(N'Phòng Nhân sự', N'Quản lý nhân sự, tuyển dụng, đào tạo', '2020-01-01', N'Tầng 5', N'Hoạt động'),
(N'Phòng Kế toán', N'Quản lý tài chính, kế toán', '2020-01-01', N'Tầng 5', N'Hoạt động'),
(N'Phòng Kinh doanh', N'Phát triển kinh doanh, bán hàng', '2020-01-01', N'Tầng 3', N'Hoạt động'),
(N'Phòng Kỹ thuật', N'Phát triển sản phẩm, kỹ thuật', '2020-01-01', N'Tầng 4', N'Hoạt động'),
(N'Phòng Marketing', N'Marketing, truyền thông', '2020-03-01', N'Tầng 3', N'Hoạt động');

-- Loại nghỉ phép
INSERT INTO LoaiNghiPhep (TenLoaiPhep, MoTa, CoLuong) VALUES
(N'Phép năm', N'Nghỉ phép hàng năm theo quy định', 1),
(N'Phép ốm', N'Nghỉ ốm có giấy xác nhận y tế', 1),
(N'Phép không lương', N'Nghỉ không hưởng lương', 0),
(N'Nghỉ việc riêng', N'Nghỉ do việc cá nhân', 1),
(N'Nghỉ thai sản', N'Nghỉ thai sản theo quy định', 1);

-- Phụ cấp
INSERT INTO LoaiPhuCap (TenPhuCap, MoTa, SoTien) VALUES
(N'Phụ cấp ăn trưa', N'Hỗ trợ tiền ăn trưa hàng tháng', 730000),
(N'Phụ cấp xăng xe', N'Hỗ trợ chi phí đi lại', 500000),
(N'Phụ cấp điện thoại', N'Hỗ trợ chi phí liên lạc', 300000),
(N'Phụ cấp vị trí', N'Phụ cấp theo vị trí công việc', 1000000),
(N'Phụ cấp trách nhiệm', N'Phụ cấp cho vị trí quản lý', 2000000);

PRINT N'=== TẠO CƠ SỞ DỮ LIỆU HRM THÀNH CÔNG ===';
GO
