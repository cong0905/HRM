using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class TinTuyenDungConfiguration : IEntityTypeConfiguration<TinTuyenDung>
{
    public void Configure(EntityTypeBuilder<TinTuyenDung> builder)
    {
        builder.ToTable("TinTuyenDung");
        builder.HasKey(e => e.MaTinTuyenDung);
        builder.Property(e => e.ViTriTuyenDung).IsRequired().HasMaxLength(200);
        builder.Property(e => e.MucLuongMin).HasColumnType("decimal(18,2)");
        builder.Property(e => e.MucLuongMax).HasColumnType("decimal(18,2)");
        builder.Property(e => e.DiadiemLamViec).HasMaxLength(200);
        builder.Property(e => e.TrangThai).HasMaxLength(30).HasDefaultValue("Đang tuyển");

        builder.HasOne(e => e.PhongBan).WithMany(p => p.DanhSachTinTuyenDung).HasForeignKey(e => e.MaPhongBan).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiTaoNav).WithMany().HasForeignKey(e => e.NguoiTao).OnDelete(DeleteBehavior.Restrict);
    }
}

public class UngVienConfiguration : IEntityTypeConfiguration<UngVien>
{
    public void Configure(EntityTypeBuilder<UngVien> builder)
    {
        builder.ToTable("UngVien");
        builder.HasKey(e => e.MaUngVien);
        builder.Property(e => e.HoTen).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.SoDienThoai).HasMaxLength(15);
        builder.Property(e => e.DuongDanCV).HasMaxLength(500);
        builder.Property(e => e.DuongDanThuXinViec).HasMaxLength(500);
        builder.Property(e => e.KinhNghiem).HasMaxLength(500);
        builder.Property(e => e.BangCap).HasMaxLength(200);
        builder.Property(e => e.KyNang).HasMaxLength(500);
        builder.Property(e => e.PhanLoai).HasMaxLength(30).HasDefaultValue("Chờ xem xét");
        builder.Property(e => e.TrangThai).HasMaxLength(30).HasDefaultValue("Mới nộp");
        builder.Property(e => e.GhiChu).HasMaxLength(500);
        builder.HasIndex(e => e.MaTinTuyenDung);

        builder.HasOne(e => e.TinTuyenDung).WithMany(t => t.UngViens).HasForeignKey(e => e.MaTinTuyenDung).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PhongVanConfiguration : IEntityTypeConfiguration<PhongVan>
{
    public void Configure(EntityTypeBuilder<PhongVan> builder)
    {
        builder.ToTable("PhongVan");
        builder.HasKey(e => e.MaPhongVan);
        builder.Property(e => e.DiaDiem).HasMaxLength(200);
        builder.Property(e => e.KetQua).HasMaxLength(20);
        builder.Property(e => e.DiemDanhGia).HasColumnType("decimal(5,2)");
        builder.Property(e => e.NhanXet).HasMaxLength(1000);
        builder.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("Đã lên lịch");

        builder.HasOne(e => e.UngVien).WithMany(u => u.PhongVans).HasForeignKey(e => e.MaUngVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiPhongVanNav).WithMany().HasForeignKey(e => e.NguoiPhongVan).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QuyetDinhTuyenDungConfiguration : IEntityTypeConfiguration<QuyetDinhTuyenDung>
{
    public void Configure(EntityTypeBuilder<QuyetDinhTuyenDung> builder)
    {
        builder.ToTable("QuyetDinhTuyenDung");
        builder.HasKey(e => e.MaQuyetDinh);
        builder.Property(e => e.KetQua).IsRequired().HasMaxLength(20);
        builder.Property(e => e.MucLuongDeXuat).HasColumnType("decimal(18,2)");
        builder.Property(e => e.PhanHoiUngVien).HasMaxLength(20);
        builder.Property(e => e.GhiChu).HasMaxLength(500);

        builder.HasOne(e => e.UngVien).WithOne(u => u.QuyetDinh).HasForeignKey<QuyetDinhTuyenDung>(e => e.MaUngVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiQuyetDinhNav).WithMany().HasForeignKey(e => e.NguoiQuyetDinh).OnDelete(DeleteBehavior.Restrict);
    }
}
