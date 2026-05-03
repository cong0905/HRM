using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services
{
    public class PhongVanService : IPhongVanService
    {
        private readonly IPhongVanRepository _phongVanRepo;
        private readonly IUngVienRepository _ungVienRepo;

        public PhongVanService(IPhongVanRepository phongVanRepo, IUngVienRepository ungVienRepo)
        {
            _phongVanRepo = phongVanRepo;
            _ungVienRepo = ungVienRepo;
        }

        public async Task<bool> AddPhongVanAsync(PhongVan phongVan)
        {
            await _phongVanRepo.AddAsync(phongVan);

            // Auto-update UngVien status
            var ungVien = await _ungVienRepo.GetByIdAsync(phongVan.MaUngVien);
            if (ungVien != null)
            {
                ungVien.TrangThai = "Chờ phỏng vấn";
                await _ungVienRepo.UpdateAsync(ungVien);
            }

            return true;
        }

        public async Task<bool> DeletePhongVanAsync(int id)
        {
            try
            {
                var item = await _phongVanRepo.GetByIdAsync(id);
                if (item != null)
                {
                    await _phongVanRepo.DeleteAsync(item);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi chi tiết từ CSDL khi xóa lịch phỏng vấn:\n" + (ex.InnerException?.Message ?? ex.Message));
            }
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

            // Auto-update UngVien status based on Result and Round
            var ungVien = await _ungVienRepo.GetByIdAsync(phongVan.MaUngVien);
            if (ungVien != null)
            {
                if (phongVan.KetQua == "Không đạt" || phongVan.TrangThai == "Đã hủy")
                {
                    ungVien.TrangThai = "Trượt";
                    await _ungVienRepo.UpdateAsync(ungVien);
                }
                else if (phongVan.KetQua == "Đạt")
                {
                    if (phongVan.VongPhongVan >= 3)
                    {
                        ungVien.TrangThai = "Trúng tuyển";
                    }
                    else
                    {
                        ungVien.TrangThai = "Đang phỏng vấn";
                    }
                    await _ungVienRepo.UpdateAsync(ungVien);
                }
            }

            return true;
        }

        public async Task<List<PhongVanDTO>> GetAllAsync()
        {
            var list = await _phongVanRepo.GetAllWithDetailsAsync();
            return list.Select(MapToDTO).ToList();
        }

        public async Task<List<PhongVanDTO>> SearchAsync(string keyword)
        {
            var list = await _phongVanRepo.SearchWithDetailsAsync(keyword);
            return list.Select(MapToDTO).ToList();
        }

        private static PhongVanDTO MapToDTO(PhongVan pv) => new()
        {
            MaPhongVan = pv.MaPhongVan,
            MaUngVien = pv.MaUngVien,
            TenUngVien = pv.UngVien?.HoTen ?? string.Empty,
            NguoiPhongVanId = pv.NguoiPhongVan,
            VongPhongVan = pv.VongPhongVan.ToString(),
            NgayPhongVan = pv.NgayPhongVan,
            DiaDiem = pv.DiaDiem ?? string.Empty,
            NguoiPhongVan = pv.NguoiPhongVanNav?.HoTen ?? string.Empty,
            KetQua = pv.KetQua ?? string.Empty,
            DiemDanhGia = pv.DiemDanhGia?.ToString() ?? string.Empty,
            TrangThai = pv.TrangThai,
            NhanXet = pv.NhanXet ?? string.Empty
        };
    }
}
