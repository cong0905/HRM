using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class KyDanhGiaConfiguration : IEntityTypeConfiguration<KyDanhGia>
{
    public void Configure(EntityTypeBuilder<KyDanhGia> builder)
    {
        builder.ToTable("KyDanhGia");
        builder.HasKey(e => e.MaKyDanhGia);
        builder.Property(e => e.TenKyDanhGia).IsRequired().HasMaxLength(100);
        builder.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("Mở");
    }
}

public class HieuSuatNhanVienConfiguration : IEntityTypeConfiguration<HieuSuatNhanVien>
{
    public void Configure(EntityTypeBuilder<HieuSuatNhanVien> builder)
    {
        builder.ToTable("HieuSuatNhanVien");
        builder.HasKey(e => e.MaHieuSuat);
        builder.Property(e => e.DiemKPI).HasColumnType("decimal(5,2)");
        builder.Property(e => e.KetQuaCongViec).HasMaxLength(1000);
        builder.Property(e => e.TyLeHoanThanhDeadline).HasColumnType("decimal(5,2)");
        builder.Property(e => e.SoGioLamViec).HasColumnType("decimal(7,2)");
        builder.Property(e => e.XepHang).HasMaxLength(20);
        builder.Property(e => e.GhiChu).HasMaxLength(500);

        builder.HasOne(e => e.NhanVien).WithMany().HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.KyDanhGia).WithMany(k => k.HieuSuats).HasForeignKey(e => e.MaKyDanhGia).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiDanhGiaNav).WithMany().HasForeignKey(e => e.NguoiDanhGia).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ChuongTrinhDaoTaoConfiguration : IEntityTypeConfiguration<ChuongTrinhDaoTao>
{
    public void Configure(EntityTypeBuilder<ChuongTrinhDaoTao> builder)
    {
        builder.ToTable("ChuongTrinhDaoTao");
        builder.HasKey(e => e.MaDaoTao);
        builder.Property(e => e.TenKhoaHoc).IsRequired().HasMaxLength(200);
        builder.Property(e => e.MucTieu).HasMaxLength(1000);
        builder.Property(e => e.GiangVien).HasMaxLength(200);
        builder.Property(e => e.DiaDiem).HasMaxLength(200);
        builder.Property(e => e.ChiPhi).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TrangThai).HasMaxLength(30).HasDefaultValue("Lên kế hoạch");
    }
}

public class PhanCongDaoTaoConfiguration : IEntityTypeConfiguration<PhanCongDaoTao>
{
    public void Configure(EntityTypeBuilder<PhanCongDaoTao> builder)
    {
        builder.ToTable("PhanCongDaoTao");
        builder.HasKey(e => e.MaPhanCong);
        builder.Property(e => e.TyLeThamDu).HasColumnType("decimal(5,2)");
        builder.Property(e => e.KetQuaKiemTra).HasColumnType("decimal(5,2)");
        builder.Property(e => e.PhanHoi).HasMaxLength(1000);
        builder.Property(e => e.TrangThai).HasMaxLength(30).HasDefaultValue("Đã đăng ký");
        builder.HasIndex(e => new { e.MaDaoTao, e.MaNhanVien }).IsUnique();

        builder.HasOne(e => e.ChuongTrinhDaoTao).WithMany(c => c.PhanCongs).HasForeignKey(e => e.MaDaoTao).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NhanVien).WithMany().HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DanhGiaDaoTaoConfiguration : IEntityTypeConfiguration<DanhGiaDaoTao>
{
    public void Configure(EntityTypeBuilder<DanhGiaDaoTao> builder)
    {
        builder.ToTable("DanhGiaDaoTao");
        builder.HasKey(e => e.MaDanhGia);
        builder.Property(e => e.DiemSo).HasColumnType("decimal(5,2)");
        builder.Property(e => e.DanhGiaGiangVien).HasMaxLength(1000);
        builder.Property(e => e.PhanHoiHocVien).HasMaxLength(1000);

        builder.HasOne(e => e.ChuongTrinhDaoTao).WithMany(c => c.DanhGias).HasForeignKey(e => e.MaDaoTao).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NhanVien).WithMany().HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ChungChiConfiguration : IEntityTypeConfiguration<ChungChi>
{
    public void Configure(EntityTypeBuilder<ChungChi> builder)
    {
        builder.ToTable("ChungChi");
        builder.HasKey(e => e.MaChungChi);
        builder.Property(e => e.TenChungChi).IsRequired().HasMaxLength(200);
        builder.Property(e => e.LoaiChungChi).HasMaxLength(50);
        builder.Property(e => e.ToChucCap).HasMaxLength(200);
        builder.Property(e => e.DuongDanFile).HasMaxLength(500);
        builder.Property(e => e.GhiChu).HasMaxLength(500);
        builder.HasIndex(e => e.NgayHetHan);

        builder.HasOne(e => e.NhanVien).WithMany(n => n.ChungChis).HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
    }
}
