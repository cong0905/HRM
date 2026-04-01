namespace HRM.Domain.Entities;

public class UngVien
{
    public int MaUngVien { get; set; }
    public int MaTinTuyenDung { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DuongDanCV { get; set; }
    public string? DuongDanThuXinViec { get; set; }
    public string? KinhNghiem { get; set; }
    public string? BangCap { get; set; }
    public string? KyNang { get; set; }
    public string PhanLoai { get; set; } = "Chờ xem xét";
    public string TrangThai { get; set; } = "Mới nộp";
    public string? GhiChu { get; set; }
    public DateTime NgayNop { get; set; } = DateTime.Now;

    // Navigation
    public TinTuyenDung TinTuyenDung { get; set; } = null!;
    public ICollection<PhongVan> PhongVans { get; set; } = new List<PhongVan>();
    public QuyetDinhTuyenDung? QuyetDinh { get; set; }
}
