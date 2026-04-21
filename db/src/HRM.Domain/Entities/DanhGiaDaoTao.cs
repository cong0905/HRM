namespace HRM.Domain.Entities;

public class DanhGiaDaoTao
{
    public int MaDanhGia { get; set; }
    public int MaDaoTao { get; set; }
    public int MaNhanVien { get; set; }
    public decimal? DiemSo { get; set; }
    public string? DanhGiaGiangVien { get; set; }
    public string? PhanHoiHocVien { get; set; }
    public int? ChatLuongKhoaHoc { get; set; }
    public DateTime NgayDanhGia { get; set; } = DateTime.Now;

    // Navigation
    public ChuongTrinhDaoTao ChuongTrinhDaoTao { get; set; } = null!;
    public NhanVienDTO NhanVien { get; set; } = null!;
}
