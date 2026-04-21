namespace HRM.Common.DTOs;

public class BangLuongDTO
{
    public int MaBangLuong { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public decimal LuongCoBan { get; set; }
    public decimal TongPhuCap { get; set; }
    public int SoNgayLamViec { get; set; }
    public decimal SoGioLamThem { get; set; }
    public decimal TienLamThem { get; set; }
    public decimal TongThuong { get; set; }
    public decimal TongPhat { get; set; }
    public decimal BHXH { get; set; }
    public decimal BHYT { get; set; }
    public decimal BHTN { get; set; }
    public decimal ThueTNCN { get; set; }
    public decimal? TongThuNhap { get; set; }
    public decimal? TongKhauTru { get; set; }
    public decimal? LuongThucNhan { get; set; }
    public DateTime NgayTinhLuong { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
