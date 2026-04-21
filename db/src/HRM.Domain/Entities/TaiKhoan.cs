namespace HRM.Domain.Entities;

public class TaiKhoan
{
    public int MaTaiKhoan { get; set; }
    public int MaNhanVien { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string VaiTro { get; set; } = "Nhân viên";
    public string TrangThai { get; set; } = "Hoạt động";
    public DateTime? LanDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    public NhanVienDTO NhanVien { get; set; } = null!;
}
