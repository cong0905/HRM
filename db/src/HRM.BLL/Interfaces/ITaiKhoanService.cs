using HRM.Common.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface ITaiKhoanService
{
    Task<List<TaiKhoanDTO>> GetAllAsync();
    Task<List<TaiKhoanDTO>> SearchAsync(string keyword);
    Task UpdateAsync(int id, TaiKhoanUpdateDTO dto);
    Task DeleteAsync(int id);
}
