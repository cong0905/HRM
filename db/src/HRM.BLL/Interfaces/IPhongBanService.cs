using HRM.Common.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface IPhongBanService
{
    Task<List<PhongBanDTO>> GetAllAsync();
    Task<PhongBanDTO?> GetByIdAsync(int id);
    Task CreateAsync(PhongBanDTO dto);
    Task UpdateAsync(PhongBanDTO dto);
    Task DeleteAsync(int id);
    Task<List<PhongBanDTO>> SearchAsync(string keyword);
}
