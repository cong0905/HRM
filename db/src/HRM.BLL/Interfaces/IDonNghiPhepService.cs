using HRM.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface IDonNghiPhepService
{
    Task<List<DonNghiPhepDTO>> GetByNhanVienAsync(int maNhanVien);
    Task<List<DonNghiPhepDTO>> GetPendingAsync();
    Task CreateAsync(int maNhanVien, int maLoaiPhep, DateTime ngayBatDau, DateTime ngayKetThuc, string lyDo);
    Task ApproveAsync(int maDonPhep, int nguoiDuyet);
    Task RejectAsync(int maDonPhep, int nguoiDuyet, string lyDoTuChoi);
    Task CancelAsync(int maDonPhep, string lyDoHuy);
}
