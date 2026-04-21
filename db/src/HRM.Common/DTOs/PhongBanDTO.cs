namespace HRM.Common.DTOs;

public class PhongBanDTO
{
    public int MaPhongBan { get; set; }
    public string TenPhongBan { get; set; } = string.Empty;
    public string? MoTaChucNang { get; set; }
    public string? DiaDiemLamViec { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? TenTruongPhong { get; set; }
    public int SoNhanVien { get; set; }
}
