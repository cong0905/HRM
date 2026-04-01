namespace HRM.Domain.Entities;

public class ThongBao
{
    public int MaThongBao { get; set; }
    public int MaNguoiNhan { get; set; }
    public string TieuDe { get; set; } = string.Empty;
    public string? NoiDung { get; set; }
    public string? LoaiThongBao { get; set; }
    public bool DaDoc { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    public NhanVien NguoiNhan { get; set; } = null!;
}
