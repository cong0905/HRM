using HRM.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface IDonNghiPhepService
{
    Task<List<DonNghiPhepDTO>> GetByNhanVienAsync(int maNhanVien);
    Task<List<DonNghiPhepDTO>> GetPendingAsync();
    /// <summary>Toàn bộ đơn (mới nhất trước), lọc theo nhân viên và/hoặc khoảng ngày nghỉ.</summary>
    Task<List<DonNghiPhepDTO>> GetAllTraCuuAsync(string? keyword, DateTime? tuNgay, DateTime? denNgay);
    Task<List<LoaiNghiPhepDTO>> GetLoaiNghiPhepAsync();
    Task<SoNgayPhepDTO> GetSoNgayPhepAsync(int maNhanVien, int nam);
    Task CreateAsync(int maNhanVien, int maLoaiPhep, DateTime ngayBatDau, DateTime ngayKetThuc, string lyDo);
    Task ApproveAsync(int maDonPhep, int nguoiDuyet);
    Task RejectAsync(int maDonPhep, int nguoiDuyet, string lyDoTuChoi);
    Task CancelAsync(int maDonPhep, int maNhanVienChuDon, string lyDoHuy);
}
