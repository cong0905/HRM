namespace HRM.Domain.Entities;

public class PhongVan
{
    public int MaPhongVan { get; set; }
    public int MaUngVien { get; set; }
    public int VongPhongVan { get; set; } = 1;
    public DateTime NgayPhongVan { get; set; }
    public string? DiaDiem { get; set; }
    public int? NguoiPhongVan { get; set; }
    public string? CauHoiPhongVan { get; set; }
    public string? KetQua { get; set; }
    public decimal? DiemDanhGia { get; set; }
    public string? NhanXet { get; set; }
    public string TrangThai { get; set; } = "Đã lên lịch";

    // Navigation
    public UngVien UngVien { get; set; } = null!;
    public NhanVienDTO? NguoiPhongVanNav { get; set; }
}
