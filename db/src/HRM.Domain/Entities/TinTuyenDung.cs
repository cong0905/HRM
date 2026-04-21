namespace HRM.Domain.Entities;

public class TinTuyenDung
{
    public int MaTinTuyenDung { get; set; }
    public string ViTriTuyenDung { get; set; } = string.Empty;
    public int? MaPhongBan { get; set; }
    public string? MoTaCongViec { get; set; }
    public string? YeuCauUngVien { get; set; }
    public int SoLuongCanTuyen { get; set; } = 1;
    public decimal? MucLuongMin { get; set; }
    public decimal? MucLuongMax { get; set; }
    public DateTime? ThoiHanNhanHoSo { get; set; }
    public string? DiadiemLamViec { get; set; }
    public string TrangThai { get; set; } = "Đang tuyển";
    public int? NguoiTao { get; set; }
    public DateTime NgayDang { get; set; } = DateTime.Now;

    // Navigation
    public PhongBan? PhongBan { get; set; }
    public NhanVien? NguoiTaoNav { get; set; }
    public ICollection<UngVien> UngViens { get; set; } = new List<UngVien>();
}
