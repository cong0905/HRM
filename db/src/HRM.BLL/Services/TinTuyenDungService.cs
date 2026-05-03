using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services
{

    public class TinTuyenDungService : ITinTuyenDungService
    {
        private readonly ITinTuyenDungRepository _repository;

        // Tiêm Repository vào để làm việc với DB
        public TinTuyenDungService(ITinTuyenDungRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<TinTuyenDungDTO>> GetAllAsync()
        {
            var listEntity = await _repository.GetAllWithDetailsAsync();
            await AutoCloseExpiredPostingsAsync(listEntity);
            return MapToDTOList(listEntity);
        }

        public async Task<List<TinTuyenDungDTO>> SearchAsync(string keyword)
        {
            var listEntity = await _repository.SearchWithDetailsAsync(keyword);
            await AutoCloseExpiredPostingsAsync(listEntity);
            return MapToDTOList(listEntity);
        }

        private static List<TinTuyenDungDTO> MapToDTOList(List<TinTuyenDung> listEntity)
        {
            return listEntity.Select(e => new TinTuyenDungDTO
            {
                MaTinTuyenDung = e.MaTinTuyenDung,
                ViTriTuyenDung = e.ViTriTuyenDung,
                SoLuongCanTuyen = e.SoLuongCanTuyen,
                ThoiHanNhanHoSo = e.ThoiHanNhanHoSo ?? DateTime.MinValue,
                TrangThai = e.TrangThai,
                TenPhongBan = e.PhongBan != null ? e.PhongBan.TenPhongBan : "Chưa rõ",
                MucLuongMin = e.MucLuongMin,
                MucLuongMax = e.MucLuongMax,
                MoTaCongViec = e.MoTaCongViec,
                YeuCauUngVien = e.YeuCauUngVien
            }).ToList();
        }

        private async Task AutoCloseExpiredPostingsAsync(List<TinTuyenDung> listEntity)
        {
            bool isChanged = false;
            var today = DateTime.Now.Date;
            foreach (var item in listEntity)
            {
                if (item.ThoiHanNhanHoSo.HasValue && item.ThoiHanNhanHoSo.Value.Date < today && item.TrangThai == "Mở")
                {
                    item.TrangThai = "Đóng";
                    isChanged = true;
                }
            }
            if (isChanged)
            {
                await _repository.SaveChangesAsync();
            }
        }

        public async Task<TinTuyenDung?> GetByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> AddTinTuyenDungAsync(TinTuyenDung entity)
        {
            var result = await _repository.AddAsync(entity);
            return result != null;
        }

        public async Task<bool> UpdateTinTuyenDungAsync(TinTuyenDung entity)
        {
            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteTinTuyenDungAsync(int id)
        {
            return await _repository.DeleteTinTuyenDungAsync(id);
        }
    }
}

