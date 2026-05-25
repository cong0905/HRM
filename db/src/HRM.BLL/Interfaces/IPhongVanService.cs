using HRM.Domain.Entities;
using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces
{
    public interface IPhongVanService
    {
        Task<PhongVan> GetPhongVanByIdAsync(int id);
        Task<bool> AddPhongVanAsync(PhongVan phongVan);
        Task<bool> UpdatePhongVanAsync(PhongVan phongVan);
        Task<bool> DeletePhongVanAsync(int id);
     Task<List<PhongVanDTO>> GetAllAsync();
        Task<List<PhongVanDTO>> SearchAsync(string keyword);
    }
}