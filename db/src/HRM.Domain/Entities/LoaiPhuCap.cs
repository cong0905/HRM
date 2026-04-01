namespace HRM.Domain.Entities;

public class LoaiPhuCap
{
    public int MaPhuCap { get; set; }
    public string TenPhuCap { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public decimal SoTien { get; set; }

    // Navigation
    public ICollection<PhuCapNhanVien> PhuCapNhanViens { get; set; } = new List<PhuCapNhanVien>();
}
