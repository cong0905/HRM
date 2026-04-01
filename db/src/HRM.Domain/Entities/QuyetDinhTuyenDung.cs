namespace HRM.Domain.Entities;

public class QuyetDinhTuyenDung
{
    public int MaQuyetDinh { get; set; }
    public int MaUngVien { get; set; }
    public string KetQua { get; set; } = string.Empty;
    public DateTime NgayQuyetDinh { get; set; } = DateTime.Now;
    public int? NguoiQuyetDinh { get; set; }
    public decimal? MucLuongDeXuat { get; set; }
    public DateTime? NgayBatDauLamViec { get; set; }
    public bool DaGuiOfferLetter { get; set; } = false;
    public string? PhanHoiUngVien { get; set; }
    public string? GhiChu { get; set; }

    // Navigation
    public UngVien UngVien { get; set; } = null!;
    public NhanVien? NguoiQuyetDinhNav { get; set; }
}
