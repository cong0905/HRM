using HRM.Domain.Entities;

namespace HRM.BLL.Interfaces
{
    public interface IUngVienService
    {
        Task<List<UngVien>> GetAllUngVienAsync();
        Task<List<UngVien>> SearchUngVienAsync(string keyword);
        Task<UngVien> AddUngVienAsync(UngVien ungVien);
        Task UpdateUngVienAsync(UngVien ungVien);
        Task<bool> DeleteUngVienAsync(int id);
        Task<HRM.Domain.Entities.UngVien> GetByIdAsync(int id);
    }
}
