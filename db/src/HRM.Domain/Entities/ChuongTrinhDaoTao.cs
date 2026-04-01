namespace HRM.Domain.Entities;

public class ChuongTrinhDaoTao
{
    public int MaDaoTao { get; set; }
    public string TenKhoaHoc { get; set; } = string.Empty;
    public string? MucTieu { get; set; }
    public string? NoiDung { get; set; }
    public int? ThoiLuong { get; set; }
    public string? GiangVien { get; set; }
    public string? DiaDiem { get; set; }
    public decimal? ChiPhi { get; set; }
    public int? SoHocVienToiDa { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public string TrangThai { get; set; } = "Lên kế hoạch";
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<PhanCongDaoTao> PhanCongs { get; set; } = new List<PhanCongDaoTao>();
    public ICollection<DanhGiaDaoTao> DanhGias { get; set; } = new List<DanhGiaDaoTao>();
}
