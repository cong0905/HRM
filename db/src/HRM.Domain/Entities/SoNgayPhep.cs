namespace HRM.Domain.Entities;

public class SoNgayPhep
{
    public int MaNhanVien { get; set; }
    public int Nam { get; set; }
    public int TongSoNgayPhep { get; set; } = 12;
    public int SoNgayDaSuDung { get; set; } = 0;
    public int SoNgayConLai => TongSoNgayPhep - SoNgayDaSuDung;
    public int PhepNamCuConLai { get; set; } = 0;

    // Navigation
    public NhanVien NhanVien { get; set; } = null!;
}
