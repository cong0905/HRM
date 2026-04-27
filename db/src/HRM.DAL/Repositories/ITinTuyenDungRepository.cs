using HRM.Domain.Entities;

namespace HRM.DAL.Repositories
{
    public interface ITinTuyenDungRepository : IRepository<TinTuyenDung>
    {
        Task<bool> DeleteTinTuyenDungAsync(int id);
        Task<List<TinTuyenDung>> GetAllWithDetailsAsync();
        Task<List<TinTuyenDung>> SearchWithDetailsAsync(string keyword);
    }
}