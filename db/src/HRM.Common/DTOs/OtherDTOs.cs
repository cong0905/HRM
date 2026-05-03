namespace HRM.Common.DTOs;

public class ChamCongAdminUpdateDTO
{
    public DateTime NgayChamCong { get; set; }
    public TimeSpan? GioVao { get; set; }
    public TimeSpan? GioRa { get; set; }
    public string? HinhThuc { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}

public class ChamCongWhitelistDTO
{
    public int MaWhitelist { get; set; }
    public string Rule { get; set; } = string.Empty; // IP hoặc CIDR, ví dụ: 192.168.1.0/24
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
}

public class ChamCongWhitelistCreateDTO
{
    public string Rule { get; set; } = string.Empty;
    public string? GhiChu { get; set; }
}
