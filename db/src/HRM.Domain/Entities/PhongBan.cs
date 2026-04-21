namespace HRM.Domain.Entities;

public class PhongBan
{
    public int MaPhongBan { get; set; }
    public string TenPhongBan { get; set; } = string.Empty;
    public string? MoTaChucNang { get; set; }
    public DateTime? NgayThanhLap { get; set; }
    public string? DiaDiemLamViec { get; set; }
    public decimal? NganSach { get; set; }
    public string TrangThai { get; set; } = "Hoạt động";
    public int? MaTruongPhong { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public NhanVienDTO? TruongPhong { get; set; }
    public ICollection<NhanVienDTO> DanhSachNhanVien { get; set; } = new List<NhanVienDTO>();
    public ICollection<TinTuyenDung> DanhSachTinTuyenDung { get; set; } = new List<TinTuyenDung>();
}
