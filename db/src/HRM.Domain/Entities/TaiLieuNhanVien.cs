namespace HRM.Domain.Entities;

public class TaiLieuNhanVien
{
    public int MaTaiLieu { get; set; }
    public int MaNhanVien { get; set; }
    public string LoaiTaiLieu { get; set; } = string.Empty;
    public string TenTaiLieu { get; set; } = string.Empty;
    public string DuongDanFile { get; set; } = string.Empty;
    public DateTime NgayTaiLen { get; set; } = DateTime.Now;
    public string? GhiChu { get; set; }

    // Navigation
    public NhanVienDTO NhanVien { get; set; } = null!;
}
