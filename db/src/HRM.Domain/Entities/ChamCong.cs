namespace HRM.Domain.Entities;

public class ChamCong
{
    public int MaChamCong { get; set; }
    public int MaNhanVien { get; set; }
    public DateTime NgayChamCong { get; set; }
    public TimeSpan? GioVao { get; set; }
    public TimeSpan? GioRa { get; set; }
    public decimal? TongGioLam { get; set; }
    public decimal GioLamThem { get; set; } = 0;
    public string? HinhThuc { get; set; }
    public string TrangThai { get; set; } = "Bình thường";
    public string? GhiChu { get; set; }

    // Navigation
    public NhanVienDTO NhanVien { get; set; } = null!;
    public ICollection<DieuChinhChamCong> DieuChinhs { get; set; } = new List<DieuChinhChamCong>();
}
