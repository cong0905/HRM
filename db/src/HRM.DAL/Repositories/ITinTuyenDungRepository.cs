using HRM.Domain.Entities;

namespace HRM.DAL.Repositories
{
    public interface ITinTuyenDungRepository : IRepository<TinTuyenDung>
    {
        Task<bool> DeleteTinTuyenDungAsync(int id);
    }
}