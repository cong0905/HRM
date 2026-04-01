using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class LoaiNghiPhepConfiguration : IEntityTypeConfiguration<LoaiNghiPhep>
{
    public void Configure(EntityTypeBuilder<LoaiNghiPhep> builder)
    {
        builder.ToTable("LoaiNghiPhep");
        builder.HasKey(e => e.MaLoaiPhep);
        builder.Property(e => e.TenLoaiPhep).IsRequired().HasMaxLength(100);
        builder.Property(e => e.MoTa).HasMaxLength(300);

        // Seeding Loại nghỉ phép
        builder.HasData(
            new LoaiNghiPhep { MaLoaiPhep = 1, TenLoaiPhep = "Phép năm", MoTa = "Nghỉ phép hàng năm theo quy định", CoLuong = true },
            new LoaiNghiPhep { MaLoaiPhep = 2, TenLoaiPhep = "Phép ốm", MoTa = "Nghỉ ốm có giấy xác nhận y tế", CoLuong = true },
            new LoaiNghiPhep { MaLoaiPhep = 3, TenLoaiPhep = "Phép không lương", MoTa = "Nghỉ không hưởng lương", CoLuong = false },
            new LoaiNghiPhep { MaLoaiPhep = 4, TenLoaiPhep = "Nghỉ việc riêng", MoTa = "Nghỉ do việc cá nhân", CoLuong = true },
            new LoaiNghiPhep { MaLoaiPhep = 5, TenLoaiPhep = "Nghỉ thai sản", MoTa = "Nghỉ thai sản theo quy định", CoLuong = true }
        );
    }
}

public class SoNgayPhepConfiguration : IEntityTypeConfiguration<SoNgayPhep>
{
    public void Configure(EntityTypeBuilder<SoNgayPhep> builder)
    {
        builder.ToTable("SoNgayPhep");
        builder.HasKey(e => new { e.MaNhanVien, e.Nam });
        builder.Ignore(e => e.SoNgayConLai); // Computed in app, not DB for EF Core

        builder.HasOne(e => e.NhanVien).WithMany().HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DonNghiPhepConfiguration : IEntityTypeConfiguration<DonNghiPhep>
{
    public void Configure(EntityTypeBuilder<DonNghiPhep> builder)
    {
        builder.ToTable("DonNghiPhep");
        builder.HasKey(e => e.MaDonPhep);
        builder.Property(e => e.SoNgayNghi).HasColumnType("decimal(3,1)");
        builder.Property(e => e.LyDo).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TrangThai).IsRequired().HasMaxLength(20).HasDefaultValue("Chờ duyệt");
        builder.Property(e => e.LyDoTuChoi).HasMaxLength(500);
        builder.Property(e => e.LyDoHuy).HasMaxLength(500);

        builder.HasIndex(e => e.TrangThai);
        builder.HasIndex(e => e.MaNhanVien);

        builder.HasOne(e => e.NhanVien).WithMany(n => n.DonNghiPheps).HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.LoaiNghiPhep).WithMany(l => l.DonNghiPheps).HasForeignKey(e => e.MaLoaiPhep).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiPheDuyetNav).WithMany().HasForeignKey(e => e.NguoiPheDuyet).OnDelete(DeleteBehavior.Restrict);
    }
}
