using HRM.BLL.Interfaces;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services
{
    public class PhongVanService : IPhongVanService
    {
        private readonly IPhongVanRepository _phongVanRepo;

        public async Task<bool> AddPhongVanAsync(PhongVan phongVan)
        {
            await _phongVanRepo.AddAsync(phongVan);
            return true;
        }

        public PhongVanService(IPhongVanRepository phongVanRepo)
        {
            _phongVanRepo = phongVanRepo;
        }

        public async Task<bool> DeletePhongVanAsync(int id)
        {
            var item = await _phongVanRepo.GetByIdAsync(id);
            if (item != null)
            {
                await _phongVanRepo.DeleteAsync(item);
                return true;
            }
            return false;
        }

        //public async Task<List<PhongVan>> GetAllPhongVansAsync()
        //{
        //    return await _phongVanRepo.GetAllAsync();
        //}

        public async Task<PhongVan> GetPhongVanByIdAsync(int id)
        {
            return await _phongVanRepo.GetByIdAsync(id);
        }

        public async Task<bool> UpdatePhongVanAsync(PhongVan phongVan)
        {
            await _phongVanRepo.UpdateAsync(phongVan);
            return true;
        }

        public async Task<List<PhongVan>> GetAllAsync()
        {
            return await _phongVanRepo.GetAllAsync();
        }
    }
}
