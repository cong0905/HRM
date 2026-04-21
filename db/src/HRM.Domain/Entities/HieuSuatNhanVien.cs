namespace HRM.Domain.Entities;

public class HieuSuatNhanVien
{
    public int MaHieuSuat { get; set; }
    public int MaNhanVien { get; set; }
    public int MaKyDanhGia { get; set; }
    public decimal? DiemKPI { get; set; }
    public string? KetQuaCongViec { get; set; }
    public decimal? TyLeHoanThanhDeadline { get; set; }
    public decimal? SoGioLamViec { get; set; }
    public string? XepHang { get; set; }
    public int? NguoiDanhGia { get; set; }
    public DateTime NgayDanhGia { get; set; } = DateTime.Now;
    public string? GhiChu { get; set; }

    // Navigation
    public NhanVien NhanVien { get; set; } = null!;
    public KyDanhGia KyDanhGia { get; set; } = null!;
    public NhanVien? NguoiDanhGiaNav { get; set; }
}
