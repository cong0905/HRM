using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HRM.Domain.Entities;

namespace HRM.DAL.Configurations
{
    public class ChinhSachHeThongConfiguration : 
        IEntityTypeConfiguration<TaiKhoan>,
        IEntityTypeConfiguration<ChinhSach>,
        IEntityTypeConfiguration<LichSuChinhSach>,
        IEntityTypeConfiguration<XacNhanChinhSach>,
        IEntityTypeConfiguration<ThongBao>
    {
        public void Configure(EntityTypeBuilder<TaiKhoan> builder)
        {
            builder.ToTable("TaiKhoan");
            builder.HasKey(t => t.MaTaiKhoan);
            builder.Property(t => t.TenDangNhap).IsRequired().HasMaxLength(50);
            builder.Property(t => t.MatKhauHash).IsRequired().HasMaxLength(255);
            builder.Property(t => t.VaiTro).HasMaxLength(20);
            builder.Property(t => t.TrangThai).HasMaxLength(20);

            builder.HasOne(t => t.NhanVien)
                   .WithMany()
                   .HasForeignKey(t => t.MaNhanVien)
                   .OnDelete(DeleteBehavior.Restrict);

            // Seeding Tài khoản Admin
            builder.HasData(new TaiKhoan
            {
                MaTaiKhoan = 1,
                MaNhanVien = 1,
                TenDangNhap = "admin",
                MatKhauHash = "$2a$11$KWj.ShrxV4ZnlRTKcJZmWODKnW5JlDBA.n3P2pu68ifqc06xQcCve", // Password: admin123
                VaiTro = "Admin",
                TrangThai = "Hoạt động"
            });
        }

        public void Configure(EntityTypeBuilder<ChinhSach> builder)
        {
            builder.ToTable("ChinhSach");
            builder.HasKey(c => c.MaChinhSach);
            builder.Property(c => c.TenChinhSach).IsRequired().HasMaxLength(200);
        }

        public void Configure(EntityTypeBuilder<LichSuChinhSach> builder)
        {
            builder.ToTable("LichSuChinhSach");
            builder.HasKey(l => l.MaLichSu);
            builder.Property(l => l.NoiDungThayDoi).HasMaxLength(500);
        }

        public void Configure(EntityTypeBuilder<XacNhanChinhSach> builder)
        {
            builder.ToTable("XacNhanChinhSach");
            builder.HasKey(x => x.MaXacNhan);
        }

        public void Configure(EntityTypeBuilder<ThongBao> builder)
        {
            builder.ToTable("ThongBao");
            builder.HasKey(t => t.MaThongBao);
            builder.Property(t => t.TieuDe).IsRequired().HasMaxLength(200);
            builder.Property(t => t.NoiDung).IsRequired();
            builder.Property(t => t.LoaiThongBao).HasMaxLength(50);

            builder.HasOne(t => t.NguoiNhan)
                   .WithMany()
                   .HasForeignKey(t => t.MaNguoiNhan)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
