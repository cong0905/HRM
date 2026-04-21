namespace HRM.Domain.Entities;

public class PhuCapNhanVien
{
    public int MaNhanVien { get; set; }
    public int MaPhuCap { get; set; }
    public DateTime NgayApDung { get; set; }
    public DateTime? NgayKetThuc { get; set; }

    // Navigation
    public NhanVien NhanVien { get; set; } = null!;
    public LoaiPhuCap LoaiPhuCap { get; set; } = null!;
}
