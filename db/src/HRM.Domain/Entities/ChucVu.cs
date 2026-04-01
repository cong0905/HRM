namespace HRM.Domain.Entities;

public class ChucVu
{
    public int MaChucVu { get; set; }
    public string TenChucVu { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int CapBac { get; set; } = 1;

    // Navigation
    public ICollection<NhanVien> DanhSachNhanVien { get; set; } = new List<NhanVien>();
}
