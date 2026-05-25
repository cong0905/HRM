using HRM.Common.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface ITaiKhoanService
{
    Task<List<TaiKhoanDTO>> GetAllAsync();
    Task<List<TaiKhoanDTO>> SearchAsync(string? keyword, string? role = null, string? status = null);
    Task CreateAsync(RegisterDTO dto);
    Task UpdateAsync(int id, TaiKhoanUpdateDTO dto);
    Task ChangePasswordAsync(int maTaiKhoan, string matKhauMoi);
    Task DeleteAsync(int id);
}
