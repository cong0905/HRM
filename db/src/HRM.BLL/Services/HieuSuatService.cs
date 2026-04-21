using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class HieuSuatService : IHieuSuatService
{
    private readonly IRepository<HieuSuatNhanVien> _hieuSuatRepo;
    private readonly INhanVienRepository _nhanVienRepo;
    private readonly IRepository<KyDanhGia> _kyDanhGiaRepo;

    public HieuSuatService(
        IRepository<HieuSuatNhanVien> hieuSuatRepo,
        INhanVienRepository nhanVienRepo,
        IRepository<KyDanhGia> kyDanhGiaRepo)
    {
        _hieuSuatRepo = hieuSuatRepo;
        _nhanVienRepo = nhanVienRepo;
        _kyDanhGiaRepo = kyDanhGiaRepo;
    }

    public async Task<List<HieuSuatDTO>> GetAllAsync()
    {
        var list = await _hieuSuatRepo.GetAllAsync();
        return await MapListAsync(list);
    }

    public async Task<List<HieuSuatDTO>> GetByNhanVienAsync(int maNhanVien)
    {
        var list = await _hieuSuatRepo.FindAsync(x => x.MaNhanVien == maNhanVien);
        return await MapListAsync(list);
    }

    public async Task<List<HieuSuatDTO>> GetByKyDanhGiaAsync(int maKyDanhGia)
    {
        var list = await _hieuSuatRepo.FindAsync(x => x.MaKyDanhGia == maKyDanhGia);
        return await MapListAsync(list);
    }

    public async Task<List<KyDanhGiaDTO>> GetKyDanhGiaAsync()
    {
        var periods = await _kyDanhGiaRepo.GetAllAsync();
        return periods
            .OrderByDescending(x => x.NgayBatDau)
            .ThenBy(x => x.TenKyDanhGia)
            .Select(x => new KyDanhGiaDTO
            {
                MaKyDanhGia = x.MaKyDanhGia,
                TenKyDanhGia = x.TenKyDanhGia,
                NgayBatDau = x.NgayBatDau,
                NgayKetThuc = x.NgayKetThuc
            })
            .ToList();
    }

    public async Task<HieuSuatDTO> CreateAsync(HieuSuatDTO dto)
    {
        await EnsureReferencesExistAsync(dto.MaNhanVien, dto.MaKyDanhGia, dto.NguoiDanhGia);

        var entity = new HieuSuatNhanVien
        {
            MaNhanVien = dto.MaNhanVien,
            MaKyDanhGia = dto.MaKyDanhGia,
            DiemKPI = dto.DiemKPI,
            KetQuaCongViec = dto.KetQuaCongViec,
            TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline,
            SoGioLamViec = dto.SoGioLamViec,
            XepHang = string.IsNullOrWhiteSpace(dto.XepHang) ? null : dto.XepHang.Trim(),
            NguoiDanhGia = dto.NguoiDanhGia,
            NgayDanhGia = dto.NgayDanhGia == default ? DateTime.Now : dto.NgayDanhGia,
            GhiChu = dto.GhiChu
        };

        var created = await _hieuSuatRepo.AddAsync(entity);
        return await MapAsync(created);
    }

    public async Task UpdateAsync(int maHieuSuat, HieuSuatDTO dto)
    {
        var entity = await _hieuSuatRepo.GetByIdAsync(maHieuSuat);
        if (entity == null)
            throw new Exception("Không tìm thấy bản ghi hiệu suất.");

        await EnsureReferencesExistAsync(dto.MaNhanVien, dto.MaKyDanhGia, dto.NguoiDanhGia);

        entity.MaNhanVien = dto.MaNhanVien;
        entity.MaKyDanhGia = dto.MaKyDanhGia;
        entity.DiemKPI = dto.DiemKPI;
        entity.KetQuaCongViec = dto.KetQuaCongViec;
        entity.TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline;
        entity.SoGioLamViec = dto.SoGioLamViec;
        entity.XepHang = string.IsNullOrWhiteSpace(dto.XepHang) ? null : dto.XepHang.Trim();
        entity.NguoiDanhGia = dto.NguoiDanhGia;
        entity.NgayDanhGia = dto.NgayDanhGia == default ? entity.NgayDanhGia : dto.NgayDanhGia;
        entity.GhiChu = dto.GhiChu;

        await _hieuSuatRepo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int maHieuSuat)
    {
        var entity = await _hieuSuatRepo.GetByIdAsync(maHieuSuat);
        if (entity == null)
            throw new Exception("Không tìm thấy bản ghi hiệu suất.");

        await _hieuSuatRepo.DeleteAsync(entity);
    }

    private async Task<List<HieuSuatDTO>> MapListAsync(IEnumerable<HieuSuatNhanVien> list)
    {
        var employees = await _nhanVienRepo.GetAllWithDetailsAsync();
        var employeeMap = employees.ToDictionary(x => x.MaNhanVien);

        var periods = await _kyDanhGiaRepo.GetAllAsync();
        var periodMap = periods.ToDictionary(x => x.MaKyDanhGia);

        return list
            .OrderByDescending(x => x.NgayDanhGia)
            .Select(x => MapToDto(x, employeeMap, periodMap))
            .ToList();
    }

    private async Task<HieuSuatDTO> MapAsync(HieuSuatNhanVien entity)
    {
        var employees = await _nhanVienRepo.GetAllWithDetailsAsync();
        var employeeMap = employees.ToDictionary(x => x.MaNhanVien);

        var periods = await _kyDanhGiaRepo.GetAllAsync();
        var periodMap = periods.ToDictionary(x => x.MaKyDanhGia);

        return MapToDto(entity, employeeMap, periodMap);
    }

    private static HieuSuatDTO MapToDto(
        HieuSuatNhanVien entity,
        IReadOnlyDictionary<int, NhanVien> employeeMap,
        IReadOnlyDictionary<int, KyDanhGia> periodMap)
    {
        employeeMap.TryGetValue(entity.MaNhanVien, out var employee);
        periodMap.TryGetValue(entity.MaKyDanhGia, out var period);

        NhanVien? nguoiDanhGia = null;
        if (entity.NguoiDanhGia.HasValue)
        {
            employeeMap.TryGetValue(entity.NguoiDanhGia.Value, out nguoiDanhGia);
        }

        var score = entity.DiemKPI ?? 0m;

        return new HieuSuatDTO
        {
            MaHieuSuat = entity.MaHieuSuat,
            MaNhanVien = entity.MaNhanVien,
            TenNhanVien = employee?.HoTen,
            MaKyDanhGia = entity.MaKyDanhGia,
            TenKyDanhGia = period?.TenKyDanhGia,
            DiemKPI = entity.DiemKPI,
            KetQuaCongViec = entity.KetQuaCongViec,
            TyLeHoanThanhDeadline = entity.TyLeHoanThanhDeadline,
            SoGioLamViec = entity.SoGioLamViec,
            XepHang = entity.XepHang,
            NguoiDanhGia = entity.NguoiDanhGia,
            TenNguoiDanhGia = nguoiDanhGia?.HoTen,
            NgayDanhGia = entity.NgayDanhGia,
            GhiChu = entity.GhiChu,
            TenPhongBan = employee?.PhongBan?.TenPhongBan,
            TenChucVu = employee?.ChucVu?.TenChucVu,
            HieuSuat = score
        };
    }

    private async Task EnsureReferencesExistAsync(int maNhanVien, int maKyDanhGia, int? nguoiDanhGia)
    {
        var nhanVien = await _nhanVienRepo.GetByIdAsync(maNhanVien);
        if (nhanVien == null)
            throw new Exception("Không tìm thấy nhân viên.");

        var kyDanhGia = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);
        if (kyDanhGia == null)
            throw new Exception("Không tìm thấy kỳ đánh giá.");

        if (nguoiDanhGia.HasValue)
        {
            var nguoi = await _nhanVienRepo.GetByIdAsync(nguoiDanhGia.Value);
            if (nguoi == null)
                throw new Exception("Không tìm thấy người đánh giá.");
        }
    }
}