namespace HRM.Common.DTOs;

public class HieuSuatDTO
{
    public int MaHieuSuat { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public int MaKyDanhGia { get; set; }
    public string? TenKyDanhGia { get; set; }
    public decimal? DiemKPI { get; set; }
    public decimal? TyLeHoanThanhDeadline { get; set; }
    public decimal? SoGioLamViec { get; set; }
    
    public decimal? DiemChuyenCan { get; set; }
    public decimal? TyLeDiLam { get; set; }
    public decimal? TyLeGioLam { get; set; }
    public decimal? TyLeDungGio { get; set; }
    public string? NhanXetCuaQuanLy { get; set; }

    public DateTime NgayDanhGia { get; set; }
    public decimal DiemHieuSuatTong { get; set; }
    public string TrangThaiHoanThanh { get; set; } = "Chưa đánh giá";
    public decimal HeSoLuongHieuSuat { get; set; }
    public decimal LuongDuKien { get; set; }
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
    public string TrangThai { get; set; } = "Mở";
}