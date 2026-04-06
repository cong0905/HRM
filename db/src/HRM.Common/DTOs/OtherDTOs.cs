namespace HRM.Common.DTOs;

public class PhongBanDTO
{
    public int MaPhongBan { get; set; }
    public string TenPhongBan { get; set; } = string.Empty;
    public string? MoTaChucNang { get; set; }
    public string? DiaDiemLamViec { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string? TenTruongPhong { get; set; }
    public int SoNhanVien { get; set; }
}

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

public class DonNghiPhepDTO
{
    public int MaDonPhep { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public string? TenLoaiPhep { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public decimal SoNgayNghi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? TenNguoiDuyet { get; set; }
    public DateTime NgayTao { get; set; }
}

public class BangLuongDTO
{
    public int MaBangLuong { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public decimal LuongCoBan { get; set; }
    public decimal TongPhuCap { get; set; }
    public decimal TongThuong { get; set; }
    public decimal TongPhat { get; set; }
    public decimal? TongThuNhap { get; set; }
    public decimal? TongKhauTru { get; set; }
    public decimal? LuongThucNhan { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public class LoginDTO
{
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
}

public class UserSessionDTO
{
    public int MaNhanVien { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string? TenPhongBan { get; set; }
}

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

public class TaiKhoanUpdateDTO
{
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? MatKhauMoi { get; set; } // Nếu không rỗng thì sẽ reset mật khẩu
}
