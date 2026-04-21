namespace HRM.Common.DTOs;

public class TaiKhoanDTO
{
    public int MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime? LanDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; } // Lấy từ navigation
}
