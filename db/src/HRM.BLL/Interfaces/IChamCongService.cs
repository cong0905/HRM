using HRM.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface IChamCongService
{
    Task<ChamCongDTO?> CheckInAsync(int maNhanVien);
    Task<ChamCongDTO?> CheckOutAsync(int maNhanVien);
    Task<bool> IsCurrentNetworkAllowedAsync();
    Task<List<ChamCongWhitelistDTO>> GetWhitelistAsync();
    Task AddWhitelistAsync(ChamCongWhitelistCreateDTO dto);
    Task RemoveWhitelistAsync(int maWhitelist);
    Task<ChamCongDTO> AddByAdminAsync(int maNhanVien, ChamCongAdminUpdateDTO dto);
    Task<ChamCongDTO?> GetTodayAsync(int maNhanVien);
    /// <summary>Nhân viên có đơn nghỉ đã duyệt trùng ngày (không chấm công vào/tan ca).</summary>
    Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay);
    Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCongDTO>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay);
    Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAsync(int maNhanVien, int year, int month);
    Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAllAsync(int year, int month);
    Task UpdateByAdminAsync(int maChamCong, ChamCongAdminUpdateDTO dto);
}
