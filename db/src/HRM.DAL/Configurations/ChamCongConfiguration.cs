using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class ChamCongConfiguration : IEntityTypeConfiguration<ChamCong>
{
    public void Configure(EntityTypeBuilder<ChamCong> builder)
    {
        builder.ToTable("ChamCong");
        builder.HasKey(e => e.MaChamCong);
        builder.Property(e => e.TongGioLam).HasColumnType("decimal(5,2)");
        builder.Property(e => e.GioLamThem).HasColumnType("decimal(5,2)").HasDefaultValue(0m);
        builder.Property(e => e.HinhThuc).HasMaxLength(30);
        builder.Property(e => e.TrangThai).HasMaxLength(30).HasDefaultValue("Bình thường");
        builder.Property(e => e.GhiChu).HasMaxLength(500);
        builder.Property(e => e.Hwid).HasMaxLength(128);

        builder.HasIndex(e => e.NgayChamCong);
        builder.HasIndex(e => e.MaNhanVien);
        builder.HasIndex(e => new { e.MaNhanVien, e.NgayChamCong }).IsUnique();

        builder.HasOne(e => e.NhanVien)
            .WithMany(n => n.ChamCongs)
            .HasForeignKey(e => e.MaNhanVien)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DieuChinhChamCongConfiguration : IEntityTypeConfiguration<DieuChinhChamCong>
{
    public void Configure(EntityTypeBuilder<DieuChinhChamCong> builder)
    {
        builder.ToTable("DieuChinhChamCong");
        builder.HasKey(e => e.MaDieuChinh);
        builder.Property(e => e.LyDo).IsRequired().HasMaxLength(500);

        builder.HasOne(e => e.ChamCong)
            .WithMany(c => c.DieuChinhs)
            .HasForeignKey(e => e.MaChamCong)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.NguoiDieuChinhNav)
            .WithMany()
            .HasForeignKey(e => e.NguoiDieuChinh)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
