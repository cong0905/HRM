using System;
using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations;

public class NhanVienConfiguration : IEntityTypeConfiguration<NhanVienDTO>
{
    public void Configure(EntityTypeBuilder<NhanVienDTO> builder)
    {
        builder.ToTable("NhanVien");
        builder.HasKey(e => e.MaNhanVien);

        builder.Property(e => e.MaNV).HasMaxLength(10)
            .HasComputedColumnSql("('NV' + RIGHT('00000' + CAST(MaNhanVien AS VARCHAR(5)), 5))", stored: true);

        builder.Property(e => e.HoTen).IsRequired().HasMaxLength(100);
        builder.Property(e => e.NgaySinh).IsRequired();
        builder.Property(e => e.GioiTinh).HasMaxLength(10);
        builder.Property(e => e.CCCD).HasMaxLength(12);
        builder.Property(e => e.DiaChi).HasMaxLength(300);
        builder.Property(e => e.SoDienThoai).HasMaxLength(15);
        builder.Property(e => e.Email).HasMaxLength(100);
        builder.Property(e => e.TinhTrangHonNhan).HasMaxLength(20);
        builder.Property(e => e.MucLuong).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(e => e.TrangThai).IsRequired().HasMaxLength(30).HasDefaultValue("Đang làm việc");
        builder.Property(e => e.AnhDaiDien).HasMaxLength(500);

        builder.HasIndex(e => e.CCCD).IsUnique().HasFilter("[CCCD] IS NOT NULL");
        builder.HasIndex(e => e.Email).IsUnique().HasFilter("[Email] IS NOT NULL");
        builder.HasIndex(e => e.MaPhongBan);
        builder.HasIndex(e => e.MaChucVu);
        builder.HasIndex(e => e.TrangThai);
        builder.HasIndex(e => e.HoTen);

        builder.HasOne(e => e.PhongBan)
            .WithMany(p => p.DanhSachNhanVien)
            .HasForeignKey(e => e.MaPhongBan)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ChucVu)
            .WithMany(c => c.DanhSachNhanVien)
            .HasForeignKey(e => e.MaChucVu)
            .OnDelete(DeleteBehavior.Restrict);

        // Seeding Nhân viên Admin
        builder.HasData(new NhanVienDTO
        {
            MaNhanVien = 1,
            HoTen = "Quản trị viên",
            NgaySinh = new DateTime(1990, 1, 1),
            GioiTinh = "Nam",
            CCCD = "000000000001",
            DiaChi = "Hà Nội",
            SoDienThoai = "0900000001",
            Email = "admin@hrm.com",
            MaPhongBan = 1, // Ban Giám đốc
            MaChucVu = 1,   // Giám đốc
            NgayVaoLam = new DateTime(2020, 1, 1),
            MucLuong = 50000000,
            TrangThai = "Đang làm việc"
        });
    }
}

public class TaiLieuNhanVienConfiguration : IEntityTypeConfiguration<TaiLieuNhanVien>
{
    public void Configure(EntityTypeBuilder<TaiLieuNhanVien> builder)
    {
        builder.ToTable("TaiLieuNhanVien");
        builder.HasKey(e => e.MaTaiLieu);
        builder.Property(e => e.LoaiTaiLieu).IsRequired().HasMaxLength(50);
        builder.Property(e => e.TenTaiLieu).IsRequired().HasMaxLength(200);
        builder.Property(e => e.DuongDanFile).IsRequired().HasMaxLength(500);
        builder.Property(e => e.GhiChu).HasMaxLength(500);

        builder.HasOne(e => e.NhanVien)
            .WithMany(n => n.TaiLieus)
            .HasForeignKey(e => e.MaNhanVien)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class LichSuDieuChuyenConfiguration : IEntityTypeConfiguration<LichSuDieuChuyen>
{
    public void Configure(EntityTypeBuilder<LichSuDieuChuyen> builder)
    {
        builder.ToTable("LichSuDieuChuyen");
        builder.HasKey(e => e.MaLichSu);
        builder.Property(e => e.MucLuongCu).HasColumnType("decimal(18,2)");
        builder.Property(e => e.MucLuongMoi).HasColumnType("decimal(18,2)");
        builder.Property(e => e.LyDo).HasMaxLength(500);

        builder.HasOne(e => e.NhanVien).WithMany().HasForeignKey(e => e.MaNhanVien).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PhongBanCu).WithMany().HasForeignKey(e => e.MaPhongBanCu).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PhongBanMoi).WithMany().HasForeignKey(e => e.MaPhongBanMoi).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ChucVuCu).WithMany().HasForeignKey(e => e.MaChucVuCu).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ChucVuMoi).WithMany().HasForeignKey(e => e.MaChucVuMoi).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.NguoiThucHienNav).WithMany().HasForeignKey(e => e.NguoiThucHien).OnDelete(DeleteBehavior.Restrict);
    }
}
