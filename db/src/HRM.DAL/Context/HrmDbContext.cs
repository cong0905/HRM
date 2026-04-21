using Microsoft.EntityFrameworkCore;
using HRM.Domain.Entities;

namespace HRM.DAL.Context;

public class HrmDbContext : DbContext
{
    public HrmDbContext(DbContextOptions<HrmDbContext> options) : base(options) { }

    // Module 1: Nhân viên
    public DbSet<NhanVien> NhanVien => Set<NhanVien>();
    public DbSet<TaiLieuNhanVien> TaiLieuNhanVien => Set<TaiLieuNhanVien>();
    public DbSet<LichSuDieuChuyen> LichSuDieuChuyen => Set<LichSuDieuChuyen>();

    // Module 2: Chấm công
    public DbSet<ChamCong> ChamCong => Set<ChamCong>();
    public DbSet<DieuChinhChamCong> DieuChinhChamCong => Set<DieuChinhChamCong>();

    // Module 3: Lương
    public DbSet<LoaiPhuCap> LoaiPhuCap => Set<LoaiPhuCap>();
    public DbSet<PhuCapNhanVien> PhuCapNhanVien => Set<PhuCapNhanVien>();
    public DbSet<ThuongPhat> ThuongPhat => Set<ThuongPhat>();
    public DbSet<BangLuong> BangLuong => Set<BangLuong>();

    // Module 4: Nghỉ phép
    public DbSet<LoaiNghiPhep> LoaiNghiPhep => Set<LoaiNghiPhep>();
    public DbSet<SoNgayPhep> SoNgayPhep => Set<SoNgayPhep>();
    public DbSet<DonNghiPhep> DonNghiPhep => Set<DonNghiPhep>();

    // Module 5: Hiệu suất
    public DbSet<KyDanhGia> KyDanhGia => Set<KyDanhGia>();
    public DbSet<HieuSuatNhanVien> HieuSuatNhanVien => Set<HieuSuatNhanVien>();

    // Module 6: Đào tạo
    public DbSet<ChuongTrinhDaoTao> ChuongTrinhDaoTao => Set<ChuongTrinhDaoTao>();
    public DbSet<PhanCongDaoTao> PhanCongDaoTao => Set<PhanCongDaoTao>();
    public DbSet<DanhGiaDaoTao> DanhGiaDaoTao => Set<DanhGiaDaoTao>();
    public DbSet<ChungChi> ChungChi => Set<ChungChi>();

    // Module 7: Phòng ban
    public DbSet<PhongBan> PhongBan => Set<PhongBan>();
    public DbSet<ChucVu> ChucVu => Set<ChucVu>();

    // Module 8: Tuyển dụng
    public DbSet<TinTuyenDung> TinTuyenDung => Set<TinTuyenDung>();
    public DbSet<UngVien> UngVien => Set<UngVien>();
    public DbSet<PhongVan> PhongVan => Set<PhongVan>();
    public DbSet<QuyetDinhTuyenDung> QuyetDinhTuyenDung => Set<QuyetDinhTuyenDung>();

    // Module 9: Chính sách
    public DbSet<ChinhSach> ChinhSach => Set<ChinhSach>();
    public DbSet<LichSuChinhSach> LichSuChinhSach => Set<LichSuChinhSach>();
    public DbSet<XacNhanChinhSach> XacNhanChinhSach => Set<XacNhanChinhSach>();

    // Hệ thống
    public DbSet<TaiKhoan> TaiKhoan => Set<TaiKhoan>();
    public DbSet<ThongBao> ThongBao => Set<ThongBao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply tất cả configurations từ assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HrmDbContext).Assembly);
    }
}
