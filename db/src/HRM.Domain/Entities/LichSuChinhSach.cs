namespace HRM.Domain.Entities;

public class LichSuChinhSach
{
    public int MaLichSu { get; set; }
    public int MaChinhSach { get; set; }
    public int? PhienBanCu { get; set; }
    public int? PhienBanMoi { get; set; }
    public string? NoiDungThayDoi { get; set; }
    public string LyDoSuaDoi { get; set; } = string.Empty;
    public int? NguoiSuaDoi { get; set; }
    public DateTime NgaySuaDoi { get; set; } = DateTime.Now;

    // Navigation
    public ChinhSach ChinhSach { get; set; } = null!;
    public NhanVien? NguoiSuaDoiNav { get; set; }
}
