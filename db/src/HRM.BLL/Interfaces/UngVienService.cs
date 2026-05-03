using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Interfaces
{
    public class UngVienService : IUngVienService
    {
        private readonly IUngVienRepository _ungVienRepository;

        public UngVienService(IUngVienRepository ungVienRepository)
        {
            _ungVienRepository = ungVienRepository;
        }

        public async Task<List<UngVien>> GetAllUngVienAsync()
        {
            return await _ungVienRepository.GetAllAsync();
        }

        public async Task<List<UngVien>> SearchUngVienAsync(string keyword)
        {
            return await _ungVienRepository.SearchAsync(keyword);
        }

        public async Task<UngVien> AddUngVienAsync(UngVien ungVien)
        {
            return await _ungVienRepository.AddAsync(ungVien);
        }

        public async Task UpdateUngVienAsync(UngVien ungVien)
        {
            await _ungVienRepository.UpdateAsync(ungVien);
        }

        public async Task<bool> DeleteUngVienAsync(int id)
        {
            try
            {
                var ungVien = await _ungVienRepository.GetByIdAsync(id);
                if (ungVien != null)
                {
                    await _ungVienRepository.DeleteAsync(ungVien);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi chi tiết từ CSDL khi xóa ứng viên:\n" + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        public async Task<HRM.Domain.Entities.UngVien> GetByIdAsync(int id)
        {
            return await _ungVienRepository.GetByIdAsync(id);
        }
    }
}
