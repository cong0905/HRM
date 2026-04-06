using HRM.Common.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HRM.BLL.Interfaces;

public interface INhanVienService
{
    Task<List<NhanVienDTO>> GetAllAsync();
    Task<NhanVienDTO?> GetByIdAsync(int id);
    Task<List<NhanVienDTO>> SearchAsync(string keyword);
    Task<NhanVienDTO> CreateAsync(NhanVienCreateDTO dto);
    Task UpdateAsync(int id, NhanVienCreateDTO dto);
    Task DeleteAsync(int id);
}
