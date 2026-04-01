namespace HRM.Domain.Entities;

public class LoaiNghiPhep
{
    public int MaLoaiPhep { get; set; }
    public string TenLoaiPhep { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public bool CoLuong { get; set; } = true;

    // Navigation
    public ICollection<DonNghiPhep> DonNghiPheps { get; set; } = new List<DonNghiPhep>();
}
