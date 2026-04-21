namespace HRM.Domain.Entities;

public class BangLuong
{
    public int MaBangLuong { get; set; }
    public int MaNhanVien { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public decimal LuongCoBan { get; set; }
    public decimal TongPhuCap { get; set; } = 0;
    public int SoNgayLamViec { get; set; } = 0;
    public decimal SoGioLamThem { get; set; } = 0;
    public decimal TienLamThem { get; set; } = 0;
    public decimal TongThuong { get; set; } = 0;
    public decimal TongPhat { get; set; } = 0;
    public decimal BHXH { get; set; } = 0;
    public decimal BHYT { get; set; } = 0;
    public decimal BHTN { get; set; } = 0;
    public decimal ThueTNCN { get; set; } = 0;
    public decimal? TongThuNhap { get; set; }
    public decimal? TongKhauTru { get; set; }
    public decimal? LuongThucNhan { get; set; }
    public DateTime NgayTinhLuong { get; set; } = DateTime.Now;
    public string TrangThai { get; set; } = "Chờ duyệt";

    // Navigation
    public NhanVienDTO NhanVien { get; set; } = null!;
}
