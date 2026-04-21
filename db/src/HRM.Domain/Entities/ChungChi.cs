namespace HRM.Domain.Entities;

public class ChungChi
{
    public int MaChungChi { get; set; }
    public int MaNhanVien { get; set; }
    public string TenChungChi { get; set; } = string.Empty;
    public string? LoaiChungChi { get; set; }
    public string? ToChucCap { get; set; }
    public DateTime NgayCap { get; set; }
    public DateTime? NgayHetHan { get; set; }
    public string? DuongDanFile { get; set; }
    public string? GhiChu { get; set; }

    // Navigation
    public NhanVien NhanVien { get; set; } = null!;
}
