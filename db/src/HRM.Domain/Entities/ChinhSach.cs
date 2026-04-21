namespace HRM.Domain.Entities;

public class ChinhSach
{
    public int MaChinhSach { get; set; }
    public string TenChinhSach { get; set; } = string.Empty;
    public string? LoaiChinhSach { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public string? PhamViApDung { get; set; }
    public DateTime NgayHieuLuc { get; set; }
    public DateTime? NgayHetHieuLuc { get; set; }
    public int PhienBan { get; set; } = 1;
    public int? NguoiPheDuyet { get; set; }
    public string TrangThai { get; set; } = "Bản nháp";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public NhanVien? NguoiPheDuyetNav { get; set; }
    public ICollection<LichSuChinhSach> LichSus { get; set; } = new List<LichSuChinhSach>();
    public ICollection<XacNhanChinhSach> XacNhans { get; set; } = new List<XacNhanChinhSach>();
}
