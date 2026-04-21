namespace HRM.Common.DTOs;

public class UserSessionDTO
{
    public int MaNhanVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string? TenPhongBan { get; set; }
}
