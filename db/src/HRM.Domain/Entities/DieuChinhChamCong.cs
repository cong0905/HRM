namespace HRM.Domain.Entities;

public class DieuChinhChamCong
{
    public int MaDieuChinh { get; set; }
    public int MaChamCong { get; set; }
    public int NguoiDieuChinh { get; set; }
    public TimeSpan? GioVaoCu { get; set; }
    public TimeSpan? GioRaCu { get; set; }
    public TimeSpan? GioVaoMoi { get; set; }
    public TimeSpan? GioRaMoi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public DateTime NgayDieuChinh { get; set; } = DateTime.Now;

    // Navigation
    public ChamCong ChamCong { get; set; } = null!;
    public NhanVien NguoiDieuChinhNav { get; set; } = null!;
}
