namespace HRM.Common.DTOs;

public class BangLuongDTO
{
    public int MaBangLuong { get; set; }
    public int MaNhanVien { get; set; }
    public string? TenNhanVien { get; set; }
    public int Thang { get; set; }
    public int Nam { get; set; }
    public decimal LuongCoBan { get; set; }
    /// <summary>Điểm hiệu suất (KPI + deadline) dùng khi tính lương tháng.</summary>
    public decimal? DiemHieuSuat { get; set; }
    /// <summary>Hệ số điều chỉnh lương từ hiệu suất (−20% … +30%).</summary>
    public decimal HeSoLuongHieuSuat { get; set; }
    /// <summary>Lương CB × (1 + hệ số hiệu suất).</summary>
    public decimal LuongCoBanSauHieuSuat { get; set; }
    public decimal TongPhuCap { get; set; }
    public int SoNgayLamViec { get; set; }
    public decimal SoGioLamThem { get; set; }
    public decimal TienLamThem { get; set; }
    public decimal TongThuong { get; set; }
    public decimal TongPhat { get; set; }

    /// <summary>Chỉ dùng hiển thị màn Thưởng/phạt: thưởng − phạt.</summary>
    public decimal ThucNhanThuongPhat => TongThuong - TongPhat;
    public decimal BHXH { get; set; }
    public decimal BHYT { get; set; }
    public decimal BHTN { get; set; }
    public decimal ThueTNCN { get; set; }
    public decimal? TongThuNhap { get; set; }
    public decimal? TongKhauTru { get; set; }
    public decimal? LuongThucNhan { get; set; }
    public DateTime NgayTinhLuong { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}
