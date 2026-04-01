namespace HRM.Domain.Entities;

public class LichSuDieuChuyen
{
    public int MaLichSu { get; set; }
    public int MaNhanVien { get; set; }
    public int? MaPhongBanCu { get; set; }
    public int? MaPhongBanMoi { get; set; }
    public int? MaChucVuCu { get; set; }
    public int? MaChucVuMoi { get; set; }
    public decimal? MucLuongCu { get; set; }
    public decimal? MucLuongMoi { get; set; }
    public DateTime NgayThayDoi { get; set; }
    public string? LyDo { get; set; }
    public int? NguoiThucHien { get; set; }

    // Navigation
    public NhanVien NhanVien { get; set; } = null!;
    public PhongBan? PhongBanCu { get; set; }
    public PhongBan? PhongBanMoi { get; set; }
    public ChucVu? ChucVuCu { get; set; }
    public ChucVu? ChucVuMoi { get; set; }
    public NhanVien? NguoiThucHienNav { get; set; }
}
