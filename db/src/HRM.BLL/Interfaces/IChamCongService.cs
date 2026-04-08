using HRM.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface IChamCongService
{
    Task<ChamCongDTO?> CheckInAsync(int maNhanVien);
    Task<ChamCongDTO?> CheckOutAsync(int maNhanVien);
    Task<ChamCongDTO?> GetTodayAsync(int maNhanVien);
    Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCongDTO>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay);
    Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAsync(int maNhanVien, int year, int month);
    Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAllAsync(int year, int month);
    Task UpdateByAdminAsync(int maChamCong, ChamCongAdminUpdateDTO dto);
}
