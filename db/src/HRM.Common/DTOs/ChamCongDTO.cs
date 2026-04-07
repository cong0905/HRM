using System;

namespace HRM.Common.DTOs;

public class ChamCongDTO
{
    public int MaChamCong { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public DateTime NgayChamCong { get; set; }
    public TimeSpan? GioVao { get; set; }
    public TimeSpan? GioRa { get; set; }
    public decimal? TongGioLam { get; set; }
    public string? HinhThuc { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
