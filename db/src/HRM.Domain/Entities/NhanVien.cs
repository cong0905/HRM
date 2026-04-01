namespace HRM.Domain.Entities;

public class NhanVien
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
    public int? MaChucVu { get; set; }
    public DateTime NgayVaoLam { get; set; }
    public decimal MucLuong { get; set; }
    public string TrangThai { get; set; } = "Đang làm việc";
    public DateTime? NgayNghiViec { get; set; }
    public string? AnhDaiDien { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public PhongBan? PhongBan { get; set; }
    public ChucVu? ChucVu { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }
    public ICollection<TaiLieuNhanVien> TaiLieus { get; set; } = new List<TaiLieuNhanVien>();
    public ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();
    public ICollection<BangLuong> BangLuongs { get; set; } = new List<BangLuong>();
    public ICollection<DonNghiPhep> DonNghiPheps { get; set; } = new List<DonNghiPhep>();
    public ICollection<ThuongPhat> ThuongPhats { get; set; } = new List<ThuongPhat>();
    public ICollection<ChungChi> ChungChis { get; set; } = new List<ChungChi>();
    public ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
}
