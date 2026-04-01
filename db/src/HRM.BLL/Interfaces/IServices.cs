using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IAuthService
{
    Task<UserSessionDTO?> LoginAsync(LoginDTO loginDto);
    Task<bool> ChangePasswordAsync(int maNhanVien, string oldPassword, string newPassword);
}

public interface INhanVienService
{
    Task<List<NhanVienDTO>> GetAllAsync();
    Task<NhanVienDTO?> GetByIdAsync(int id);
    Task<List<NhanVienDTO>> SearchAsync(string keyword);
    Task<NhanVienDTO> CreateAsync(NhanVienCreateDTO dto);
    Task UpdateAsync(int id, NhanVienCreateDTO dto);
    Task DeleteAsync(int id);
}

public interface IPhongBanService
{
    Task<List<PhongBanDTO>> GetAllAsync();
    Task<PhongBanDTO?> GetByIdAsync(int id);
    Task CreateAsync(PhongBanDTO dto);
    Task UpdateAsync(PhongBanDTO dto);
    Task DeleteAsync(int id);
}

public interface IChamCongService
{
    Task<ChamCongDTO?> CheckInAsync(int maNhanVien);
    Task<ChamCongDTO?> CheckOutAsync(int maNhanVien);
    Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
}

public interface IDonNghiPhepService
{
    Task<List<DonNghiPhepDTO>> GetByNhanVienAsync(int maNhanVien);
    Task<List<DonNghiPhepDTO>> GetPendingAsync();
    Task CreateAsync(int maNhanVien, int maLoaiPhep, DateTime ngayBatDau, DateTime ngayKetThuc, string lyDo);
    Task ApproveAsync(int maDonPhep, int nguoiDuyet);
    Task RejectAsync(int maDonPhep, int nguoiDuyet, string lyDoTuChoi);
    Task CancelAsync(int maDonPhep, string lyDoHuy);
}
