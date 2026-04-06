using HRM.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface IChamCongService
{
    Task<ChamCongDTO?> CheckInAsync(int maNhanVien);
    Task<ChamCongDTO?> CheckOutAsync(int maNhanVien);
    Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
}
