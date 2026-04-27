using HRM.Domain.Entities;

namespace HRM.DAL.Repositories
{
    public interface IPhongVanRepository : IRepository<PhongVan>
    {
        Task<List<PhongVan>> GetAllWithDetailsAsync();
        Task<List<PhongVan>> SearchWithDetailsAsync(string keyword);
    }
}
