using HRM.Domain.Entities;

namespace HRM.BLL.Interfaces
{
    public interface IPhongVanService
    {
        Task<PhongVan> GetPhongVanByIdAsync(int id);
        Task<bool> AddPhongVanAsync(PhongVan phongVan);
        Task<bool> UpdatePhongVanAsync(PhongVan phongVan);
        Task<bool> DeletePhongVanAsync(int id);
        Task<List<PhongVan>> GetAllAsync();
    }
}