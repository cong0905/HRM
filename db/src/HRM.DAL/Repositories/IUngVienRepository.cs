using HRM.Domain.Entities;

namespace HRM.DAL.Repositories

{
    public interface IUngVienRepository : IRepository<UngVien>
    {
        Task<List<UngVien>> SearchAsync(string keyword);
    }
}
