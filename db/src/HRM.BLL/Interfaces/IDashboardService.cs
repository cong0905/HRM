using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDTO> GetSummaryAsync();
    Task<List<PhongBanThongKeDTO>> GetNhanVienTheoPhongBanAsync();
    Task<List<TangTruongNhanSuDTO>> GetTangTruongNhanSuAsync(int soThang = 6);
    Task<List<HoatDongGanDayDTO>> GetHoatDongGanDayAsync(int top = 5);
    Task<List<ThongBaoDashboardDTO>> GetThongBaoAsync();
}
