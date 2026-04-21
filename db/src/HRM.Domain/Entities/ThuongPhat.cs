namespace HRM.Domain.Entities;

public class ThuongPhat
{
    public int MaThuongPhat { get; set; }
    public int MaNhanVien { get; set; }
    public string Loai { get; set; } = string.Empty;
    public string? LoaiChiTiet { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public decimal SoTien { get; set; }
    public DateTime NgayApDung { get; set; }
    public int? NguoiPheDuyet { get; set; }
    public string TrangThai { get; set; } = "Chờ duyệt";

    // Navigation
    public NhanVienDTO NhanVien { get; set; } = null!;
    public NhanVienDTO? NguoiPheDuyetNav { get; set; }
}
