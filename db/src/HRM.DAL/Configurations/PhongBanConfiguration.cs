using System;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class PhongBanConfiguration : IEntityTypeConfiguration<PhongBan>
{
    public void Configure(EntityTypeBuilder<PhongBan> builder)
    {
        builder.ToTable("PhongBan");
        builder.HasKey(e => e.MaPhongBan);
        builder.Property(e => e.TenPhongBan).IsRequired().HasMaxLength(100);
        builder.Property(e => e.MoTaChucNang).HasMaxLength(500);
        builder.Property(e => e.DiaDiemLamViec).HasMaxLength(200);
        builder.Property(e => e.NganSach).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TrangThai).IsRequired().HasMaxLength(20).HasDefaultValue("Hoạt động");

        builder.HasOne(e => e.TruongPhong)
            .WithMany()
            .HasForeignKey(e => e.MaTruongPhong)
            .OnDelete(DeleteBehavior.Restrict);

        // Seeding Phòng ban
        builder.HasData(
            new PhongBan { MaPhongBan = 1, TenPhongBan = "Ban Giám đốc", MoTaChucNang = "Điều hành toàn bộ hoạt động công ty", NgayThanhLap = new DateTime(2020, 1, 1), DiaDiemLamViec = "Tầng 10", TrangThai = "Hoạt động" },
            new PhongBan { MaPhongBan = 2, TenPhongBan = "Phòng Nhân sự", MoTaChucNang = "Quản lý nhân sự, tuyển dụng, đào tạo", NgayThanhLap = new DateTime(2020, 1, 1), DiaDiemLamViec = "Tầng 5", TrangThai = "Hoạt động" },
            new PhongBan { MaPhongBan = 3, TenPhongBan = "Phòng Kế toán", MoTaChucNang = "Quản lý tài chính, kế toán", NgayThanhLap = new DateTime(2020, 1, 1), DiaDiemLamViec = "Tầng 5", TrangThai = "Hoạt động" },
            new PhongBan { MaPhongBan = 4, TenPhongBan = "Phòng Kinh doanh", MoTaChucNang = "Phát triển kinh doanh, bán hàng", NgayThanhLap = new DateTime(2020, 1, 1), DiaDiemLamViec = "Tầng 3", TrangThai = "Hoạt động" },
            new PhongBan { MaPhongBan = 5, TenPhongBan = "Phòng Kỹ thuật", MoTaChucNang = "Phát triển sản phẩm, kỹ thuật", NgayThanhLap = new DateTime(2020, 1, 1), DiaDiemLamViec = "Tầng 4", TrangThai = "Hoạt động" },
            new PhongBan { MaPhongBan = 6, TenPhongBan = "Phòng Marketing", MoTaChucNang = "Marketing, truyền thông", NgayThanhLap = new DateTime(2020, 3, 1), DiaDiemLamViec = "Tầng 3", TrangThai = "Hoạt động" }
        );
    }
}

public class ChucVuConfiguration : IEntityTypeConfiguration<ChucVu>
{
    public void Configure(EntityTypeBuilder<ChucVu> builder)
    {
        builder.ToTable("ChucVu");
        builder.HasKey(e => e.MaChucVu);
        builder.Property(e => e.TenChucVu).IsRequired().HasMaxLength(100);
        builder.Property(e => e.MoTa).HasMaxLength(500);
        builder.Property(e => e.CapBac).HasDefaultValue(1);

        // Seeding Chức vụ
        builder.HasData(
            new ChucVu { MaChucVu = 1, TenChucVu = "Giám đốc", MoTa = "Giám đốc điều hành công ty", CapBac = 6 },
            new ChucVu { MaChucVu = 2, TenChucVu = "Phó Giám đốc", MoTa = "Phó Giám đốc", CapBac = 5 },
            new ChucVu { MaChucVu = 3, TenChucVu = "Trưởng phòng", MoTa = "Trưởng phòng ban", CapBac = 4 },
            new ChucVu { MaChucVu = 4, TenChucVu = "Phó phòng", MoTa = "Phó phòng ban", CapBac = 3 },
            new ChucVu { MaChucVu = 5, TenChucVu = "Trưởng nhóm", MoTa = "Trưởng nhóm làm việc", CapBac = 2 },
            new ChucVu { MaChucVu = 6, TenChucVu = "Nhân viên", MoTa = "Nhân viên", CapBac = 1 }
        );
    }
}
