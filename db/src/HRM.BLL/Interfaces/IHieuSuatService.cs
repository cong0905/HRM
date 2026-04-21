using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IHieuSuatService
{
    Task<List<HieuSuatDTO>> GetAllAsync();
    Task<List<HieuSuatDTO>> GetByNhanVienAsync(int maNhanVien);
    Task<List<HieuSuatDTO>> GetByKyDanhGiaAsync(int maKyDanhGia);
    Task<List<KyDanhGiaDTO>> GetKyDanhGiaAsync();
    Task<KyDanhGiaDTO> CreateKyDanhGiaAsync(KyDanhGiaDTO dto);
    Task UpdateKyDanhGiaAsync(int maKyDanhGia, KyDanhGiaDTO dto);
    Task DeleteKyDanhGiaAsync(int maKyDanhGia);
    Task<HieuSuatDTO> CreateAsync(HieuSuatDTO dto);
    Task UpdateAsync(int maHieuSuat, HieuSuatDTO dto);
    Task DeleteAsync(int maHieuSuat);
}