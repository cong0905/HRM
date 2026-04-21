namespace HRM.Domain.Entities;

public class XacNhanChinhSach
{
    public int MaXacNhan { get; set; }
    public int MaChinhSach { get; set; }
    public int MaNhanVien { get; set; }
    public bool DaDoc { get; set; } = false;
    public DateTime? NgayXacNhan { get; set; }

    // Navigation
    public ChinhSach ChinhSach { get; set; } = null!;
    public NhanVienDTO NhanVien { get; set; } = null!;
}
