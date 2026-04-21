using HRM.Common.Constants;

namespace HRM.Domain.Entities;

public class DonNghiPhep
{
    public int MaDonPhep { get; set; }
    public int MaNhanVien { get; set; }
    public int MaLoaiPhep { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public decimal SoNgayNghi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string TrangThai { get; set; } = DonNghiPhepTrangThai.ChoDuyet;
    public int? NguoiPheDuyet { get; set; }
    public DateTime? NgayPheDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayHuy { get; set; }
    public string? LyDoHuy { get; set; }

    // Navigation
    public NhanVien NhanVien { get; set; } = null!;
    public LoaiNghiPhep LoaiNghiPhep { get; set; } = null!;
    public NhanVien? NguoiPheDuyetNav { get; set; }
}
