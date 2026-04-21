namespace HRM.Common.DTOs;

public class DonNghiPhepDTO
{
    public int MaDonPhep { get; set; }
    public int MaNhanVien { get; set; }
    public string? MaNV { get; set; }
    public string? TenNhanVien { get; set; }
    public string? TenPhongBan { get; set; }
    public string? TenChucVu { get; set; }
    public string? TenLoaiPhep { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public decimal SoNgayNghi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? TenNguoiDuyet { get; set; }
    public DateTime NgayTao { get; set; }
}
