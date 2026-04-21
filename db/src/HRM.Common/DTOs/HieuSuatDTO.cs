namespace HRM.Common.DTOs;

public class HieuSuatDTO
{
    public int MaHieuSuat { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public int MaKyDanhGia { get; set; }
    public string? TenKyDanhGia { get; set; }
    public decimal? DiemKPI { get; set; }
    public string? KetQuaCongViec { get; set; }
    public decimal? TyLeHoanThanhDeadline { get; set; }
    public decimal? SoGioLamViec { get; set; }
    public string? XepHang { get; set; }
    public int? NguoiDanhGia { get; set; }
    public string? TenNguoiDanhGia { get; set; }
    public DateTime NgayDanhGia { get; set; }
    public string? GhiChu { get; set; }
    public string? TenPhongBan { get; set; }
    public string? TenChucVu { get; set; }
    public decimal HieuSuat { get; set; }
}
public class HieuSuatKyDanhGiaDTO
{
    public int MaKyDanhGia { get; set; }
    public string TenKyDanhGia { get; set; } = string.Empty;
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public List<HieuSuatDTO> DanhSachHieuSuat { get; set; } = new List<HieuSuatDTO>();
    
}
public class KyDanhGiaDTO
{
    public int MaKyDanhGia { get; set; }
    public string TenKyDanhGia { get; set; } = string.Empty;
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
}