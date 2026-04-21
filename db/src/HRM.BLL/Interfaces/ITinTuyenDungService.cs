using HRM.Common.DTOs;
using HRM.Domain.Entities;

namespace HRM.BLL.Interfaces
{
    public interface ITinTuyenDungService
    {
        Task<List<TinTuyenDungDTO>> GetAllAsync();
        Task<TinTuyenDung?> GetByIdAsync(int id);
        Task<bool> AddTinTuyenDungAsync(TinTuyenDung entity);
        Task<bool> UpdateTinTuyenDungAsync(TinTuyenDung entity);
        Task<bool> DeleteTinTuyenDungAsync(int id);
    }
}
