namespace HRM.Common.DTOs;

public class NhanVienDTO
{
    public int MaNhanVien { get; set; }
    public string? MaNV { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public DateTime NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public string? CCCD { get; set; }
    public string? DiaChi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? TinhTrangHonNhan { get; set; }
    public int? MaPhongBan { get; set; }
    public string? TenPhongBan { get; set; }
    public int? MaChucVu { get; set; }
    public string? TenChucVu { get; set; }
    public DateTime NgayVaoLam { get; set; }
    public decimal MucLuong { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? AnhDaiDien { get; set; }
}

public class NhanVienCreateDTO
{
    public string HoTen { get; set; } = string.Empty;
    public DateTime NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public string? CCCD { get; set; }
    public string? DiaChi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? TinhTrangHonNhan { get; set; }
    public int? MaPhongBan { get; set; }
    public int? MaChucVu { get; set; }
    public DateTime NgayVaoLam { get; set; }
    public decimal MucLuong { get; set; }
    public string TrangThai { get; set; } = "Đang làm việc";
}
