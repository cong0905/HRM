namespace HRM.Common.DTOs;

/// <summary>Tổng hợp 4 thẻ thống kê đầu Dashboard.</summary>
public class DashboardSummaryDTO
{
    public int TongNhanVien { get; set; }
    public int DiLamHomNay { get; set; }
    public int NghiPhepHomNay { get; set; }
    public int DiMuonHomNay { get; set; }

    /// <summary>% tăng/giảm NV so với tháng trước.</summary>
    public decimal PhanTramTangNV { get; set; }
    /// <summary>% đi làm trên tổng NV.</summary>
    public decimal PhanTramDiLam { get; set; }
    /// <summary>Chênh lệch nghỉ phép so với hôm qua (+/-).</summary>
    public int ChenhLechNghiPhep { get; set; }
    /// <summary>Chênh lệch đi muộn so với hôm qua (+/-).</summary>
    public int ChenhLechDiMuon { get; set; }
}

/// <summary>Số nhân viên theo phòng ban — dùng vẽ Donut chart.</summary>
public class PhongBanThongKeDTO
{
    public string TenPhongBan { get; set; } = string.Empty;
    public int SoNhanVien { get; set; }
    public decimal PhanTram { get; set; }
}

/// <summary>Số nhân viên theo tháng — dùng vẽ Line chart.</summary>
public class TangTruongNhanSuDTO
{
    public string TenThang { get; set; } = string.Empty;
    public int SoNhanVien { get; set; }
}

/// <summary>Một mục hoạt động gần đây trên Dashboard.</summary>
public class HoatDongGanDayDTO
{
    public string TenNhanVien { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public string ThoiGian { get; set; } = string.Empty;
    /// <summary>"Check-in" | "Chờ duyệt" | "Thông báo"</summary>
    public string LoaiHoatDong { get; set; } = string.Empty;
}

/// <summary>Một mục thông báo Dashboard (đơn nghỉ chờ duyệt, NV chưa chấm công…).</summary>
public class ThongBaoDashboardDTO
{
    public string NoiDung { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public string Icon { get; set; } = string.Empty;
}
