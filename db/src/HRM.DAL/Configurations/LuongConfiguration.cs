using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class LoaiPhuCapConfiguration : IEntityTypeConfiguration<LoaiPhuCap>
{
    public void Configure(EntityTypeBuilder<LoaiPhuCap> builder)
    {
        builder.ToTable("LoaiPhuCap");
        builder.HasKey(e => e.MaPhuCap);
        builder.Property(e => e.TenPhuCap).IsRequired().HasMaxLength(100);
        builder.Property(e => e.MoTa).HasMaxLength(300);
        builder.Property(e => e.SoTien).HasColumnType("decimal(18,2)");

        // Seeding Loại phụ cấp
        builder.HasData(
            new LoaiPhuCap { MaPhuCap = 1, TenPhuCap = "Phụ cấp ăn trưa", MoTa = "Hỗ trợ tiền ăn trưa hàng tháng", SoTien = 730000 },
            new LoaiPhuCap { MaPhuCap = 2, TenPhuCap = "Phụ cấp xăng xe", MoTa = "Hỗ trợ chi phí đi lại", SoTien = 500000 },
            new LoaiPhuCap { MaPhuCap = 3, TenPhuCap = "Phụ cấp điện thoại", MoTa = "Hỗ trợ chi phí liên lạc", SoTien = 300000 },
            new LoaiPhuCap { MaPhuCap = 4, TenPhuCap = "Phụ cấp vị trí", MoTa = "Phụ cấp theo vị trí công việc", SoTien = 1000000 },
            new LoaiPhuCap { MaPhuCap = 5, TenPhuCap = "Phụ cấp trách nhiệm", MoTa = "Phụ cấp cho vị trí quản lý", SoTien = 2000000 }
        );
    }
}

public class PhuCapNhanVienConfiguration : IEntityTypeConfiguration<PhuCapNhanVien>
{
    public void Configure(EntityTypeBuilder<PhuCapNhanVien> builder)
    {
        builder.ToTable("PhuCapNhanVien");
        builder.HasKey(e => new { e.MaNhanVien, e.MaPhuCap, e.NgayApDung });

        builder.HasOne(e => e.NhanVien).WithMany().HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.LoaiPhuCap).WithMany(l => l.PhuCapNhanViens).HasForeignKey(e => e.MaPhuCap).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ThuongPhatConfiguration : IEntityTypeConfiguration<ThuongPhat>
{
    public void Configure(EntityTypeBuilder<ThuongPhat> builder)
    {
        builder.ToTable("ThuongPhat");
        builder.HasKey(e => e.MaThuongPhat);
        builder.Property(e => e.Loai).IsRequired().HasMaxLength(10);
        builder.Property(e => e.LoaiChiTiet).HasMaxLength(100);
        builder.Property(e => e.LyDo).IsRequired().HasMaxLength(500);
        builder.Property(e => e.SoTien).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("Chờ duyệt");

        builder.HasOne(e => e.NhanVien).WithMany(n => n.ThuongPhats).HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiPheDuyetNav).WithMany().HasForeignKey(e => e.NguoiPheDuyet).OnDelete(DeleteBehavior.Restrict);
    }
}

public class BangLuongConfiguration : IEntityTypeConfiguration<BangLuong>
{
    public void Configure(EntityTypeBuilder<BangLuong> builder)
    {
        builder.ToTable("BangLuong");
        builder.HasKey(e => e.MaBangLuong);
        builder.Property(e => e.LuongCoBan).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TongPhuCap).HasColumnType("decimal(18,2)");
        builder.Property(e => e.SoGioLamThem).HasColumnType("decimal(5,2)");
        builder.Property(e => e.TienLamThem).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TongThuong).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TongPhat).HasColumnType("decimal(18,2)");
        builder.Property(e => e.BHXH).HasColumnType("decimal(18,2)");
        builder.Property(e => e.BHYT).HasColumnType("decimal(18,2)");
        builder.Property(e => e.BHTN).HasColumnType("decimal(18,2)");
        builder.Property(e => e.ThueTNCN).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TongThuNhap).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TongKhauTru).HasColumnType("decimal(18,2)");
        builder.Property(e => e.LuongThucNhan).HasColumnType("decimal(18,2)");
        builder.Property(e => e.TrangThai).HasMaxLength(20).HasDefaultValue("Chờ duyệt");

        builder.HasIndex(e => new { e.Thang, e.Nam });
        builder.HasIndex(e => new { e.MaNhanVien, e.Thang, e.Nam }).IsUnique();

        builder.HasOne(e => e.NhanVien).WithMany(n => n.BangLuongs).HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
    }
}
