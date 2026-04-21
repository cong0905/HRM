namespace HRM.Domain.Entities;

public class PhanCongDaoTao
{
    public int MaPhanCong { get; set; }
    public int MaDaoTao { get; set; }
    public int MaNhanVien { get; set; }
    public DateTime NgayDangKy { get; set; } = DateTime.Now;
    public decimal TyLeThamDu { get; set; } = 0;
    public decimal? KetQuaKiemTra { get; set; }
    public string? PhanHoi { get; set; }
    public string TrangThai { get; set; } = "Đã đăng ký";

    // Navigation
    public ChuongTrinhDaoTao ChuongTrinhDaoTao { get; set; } = null!;
    public NhanVien NhanVien { get; set; } = null!;
}
