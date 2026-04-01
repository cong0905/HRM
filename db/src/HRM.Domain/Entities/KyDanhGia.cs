namespace HRM.Domain.Entities;

public class KyDanhGia
{
    public int MaKyDanhGia { get; set; }
    public string TenKyDanhGia { get; set; } = string.Empty;
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public string TrangThai { get; set; } = "Mở";

    // Navigation
    public ICollection<HieuSuatNhanVien> HieuSuats { get; set; } = new List<HieuSuatNhanVien>();
}
