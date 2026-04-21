using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class HieuSuatService : IHieuSuatService
{
    private const string TrangThaiKyDaKhoa = "Đã khóa";
    private const decimal GioChuanThang = 160m;

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
        if (list.Count > 0)
            return await MapListAsync(list);

        var employees = await _nhanVienRepo.GetAllWithDetailsAsync();
        var periods = await _kyDanhGiaRepo.GetAllAsync();
        var latestPeriod = periods
            .OrderByDescending(x => x.NgayBatDau)
            .FirstOrDefault();

        return employees
            .OrderBy(x => x.HoTen)
            .Select(e => CreateDefaultDto(e, latestPeriod))
            .ToList();
    }

    public async Task<List<HieuSuatDTO>> GetByNhanVienAsync(int maNhanVien)
    {
        var list = await _hieuSuatRepo.FindAsync(x => x.MaNhanVien == maNhanVien);
        return await MapListAsync(list);
    }

    public async Task<List<HieuSuatDTO>> GetByKyDanhGiaAsync(int maKyDanhGia)
    {
        var list = await _hieuSuatRepo.FindAsync(x => x.MaKyDanhGia == maKyDanhGia);
        var mapped = await MapListAsync(list);

        var employees = await _nhanVienRepo.GetAllWithDetailsAsync();
        var period = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);

        var existingEmployeeIds = mapped
            .Select(x => x.MaNhanVien)
            .ToHashSet();

        var missing = employees
            .Where(e => !existingEmployeeIds.Contains(e.MaNhanVien))
            .OrderBy(e => e.HoTen)
            .Select(e => CreateDefaultDto(e, period))
            .ToList();

        mapped.AddRange(missing);
        return mapped
            .OrderBy(x => x.TenNhanVien)
            .ThenByDescending(x => x.NgayDanhGia)
            .ToList();
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
                NgayKetThuc = x.NgayKetThuc,
                TrangThai = x.TrangThai
            })
            .ToList();
    }

    public async Task<KyDanhGiaDTO> CreateKyDanhGiaAsync(KyDanhGiaDTO dto)
    {
        ValidateKyDanhGia(dto);

        var existing = await _kyDanhGiaRepo.FindAsync(x => x.TenKyDanhGia == dto.TenKyDanhGia.Trim());
        if (existing.Count > 0)
            throw new Exception("Tên kỳ đánh giá đã tồn tại.");

        var entity = await _kyDanhGiaRepo.AddAsync(new KyDanhGia
        {
            TenKyDanhGia = dto.TenKyDanhGia.Trim(),
            NgayBatDau = dto.NgayBatDau.Date,
            NgayKetThuc = dto.NgayKetThuc.Date,
            TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "Mở" : dto.TrangThai.Trim()
        });

        return new KyDanhGiaDTO
        {
            MaKyDanhGia = entity.MaKyDanhGia,
            TenKyDanhGia = entity.TenKyDanhGia,
            NgayBatDau = entity.NgayBatDau,
            NgayKetThuc = entity.NgayKetThuc,
            TrangThai = entity.TrangThai
        };
    }

    public async Task UpdateKyDanhGiaAsync(int maKyDanhGia, KyDanhGiaDTO dto)
    {
        ValidateKyDanhGia(dto);

        var entity = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);
        if (entity == null)
            throw new Exception("Không tìm thấy kỳ đánh giá.");

        var duplicate = await _kyDanhGiaRepo.FindAsync(x => x.TenKyDanhGia == dto.TenKyDanhGia.Trim() && x.MaKyDanhGia != maKyDanhGia);
        if (duplicate.Count > 0)
            throw new Exception("Tên kỳ đánh giá đã tồn tại.");

        entity.TenKyDanhGia = dto.TenKyDanhGia.Trim();
        entity.NgayBatDau = dto.NgayBatDau.Date;
        entity.NgayKetThuc = dto.NgayKetThuc.Date;
        entity.TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? entity.TrangThai : dto.TrangThai.Trim();

        await _kyDanhGiaRepo.UpdateAsync(entity);
    }

    public async Task DeleteKyDanhGiaAsync(int maKyDanhGia)
    {
        var entity = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);
        if (entity == null)
            throw new Exception("Không tìm thấy kỳ đánh giá.");

        var linked = await _hieuSuatRepo.CountAsync(x => x.MaKyDanhGia == maKyDanhGia);
        if (linked > 0)
            throw new Exception("Kỳ đánh giá đã có dữ liệu hiệu suất, không thể xóa.");

        await _kyDanhGiaRepo.DeleteAsync(entity);
    }

    public async Task<HieuSuatDTO> CreateAsync(HieuSuatDTO dto)
    {
        var kyDanhGia = await EnsureReferencesExistAsync(dto.MaNhanVien, dto.MaKyDanhGia);
        EnsurePeriodOpenForWrite(kyDanhGia);

        var existed = await _hieuSuatRepo.CountAsync(x =>
            x.MaNhanVien == dto.MaNhanVien
            && x.MaKyDanhGia == dto.MaKyDanhGia);
        if (existed > 0)
            throw new Exception("Nhân viên đã có bản ghi hiệu suất trong kỳ đánh giá này.");

        var ngayDanhGia = dto.NgayDanhGia == default ? DateTime.Now : dto.NgayDanhGia;
        ValidateNgayDanhGiaTrongKy(ngayDanhGia, kyDanhGia);
        ValidateScoreRanges(dto);

        var finalScore = CalculateFinalScore(dto);
        var ketQuaCongViec = string.IsNullOrWhiteSpace(dto.KetQuaCongViec)
            ? EvaluateCompletionStatus(dto.TyLeHoanThanhDeadline, finalScore)
            : dto.KetQuaCongViec.Trim();

        var entity = new HieuSuatNhanVien
        {
            MaNhanVien = dto.MaNhanVien,
            MaKyDanhGia = dto.MaKyDanhGia,
            DiemKPI = dto.DiemKPI,
            KetQuaCongViec = ketQuaCongViec,
            TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline,
            SoGioLamViec = dto.SoGioLamViec,
            NgayDanhGia = ngayDanhGia
        };

        var created = await _hieuSuatRepo.AddAsync(entity);
        return await MapAsync(created);
    }

    public async Task UpdateAsync(int maHieuSuat, HieuSuatDTO dto)
    {
        var entity = await _hieuSuatRepo.GetByIdAsync(maHieuSuat);
        if (entity == null)
            throw new Exception("Không tìm thấy bản ghi hiệu suất.");

        var kyDanhGia = await EnsureReferencesExistAsync(dto.MaNhanVien, dto.MaKyDanhGia);
        EnsurePeriodOpenForWrite(kyDanhGia);

        var duplicate = await _hieuSuatRepo.CountAsync(x =>
            x.MaHieuSuat != maHieuSuat
            && x.MaNhanVien == dto.MaNhanVien
            && x.MaKyDanhGia == dto.MaKyDanhGia);
        if (duplicate > 0)
            throw new Exception("Nhân viên đã có bản ghi hiệu suất trong kỳ đánh giá này.");

        var ngayDanhGia = dto.NgayDanhGia == default ? entity.NgayDanhGia : dto.NgayDanhGia;
        ValidateNgayDanhGiaTrongKy(ngayDanhGia, kyDanhGia);
        ValidateScoreRanges(dto);

        var finalScore = CalculateFinalScore(dto);
        var ketQuaCongViec = string.IsNullOrWhiteSpace(dto.KetQuaCongViec)
            ? EvaluateCompletionStatus(dto.TyLeHoanThanhDeadline, finalScore)
            : dto.KetQuaCongViec.Trim();

        entity.MaNhanVien = dto.MaNhanVien;
        entity.MaKyDanhGia = dto.MaKyDanhGia;
        entity.DiemKPI = dto.DiemKPI;
        entity.KetQuaCongViec = ketQuaCongViec;
        entity.TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline;
        entity.SoGioLamViec = dto.SoGioLamViec;
        entity.NgayDanhGia = ngayDanhGia;

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

        var score = CalculateFinalScore(entity.DiemKPI, entity.TyLeHoanThanhDeadline);
        var trangThaiHoanThanh = EvaluateCompletionStatus(entity.TyLeHoanThanhDeadline, score);
        var heSoLuong = CalculateSalaryCoefficient(score, entity.TyLeHoanThanhDeadline);
        var luongDuKien = CalculateProjectedSalary(employee?.MucLuong ?? 0m, heSoLuong, entity.SoGioLamViec);

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
            NgayDanhGia = entity.NgayDanhGia,
            HieuSuat = score ?? 0m,
            TrangThaiHoanThanh = trangThaiHoanThanh,
            HeSoLuongHieuSuat = heSoLuong,
            LuongDuKien = luongDuKien
        };
    }

    private static HieuSuatDTO CreateDefaultDto(NhanVien employee, KyDanhGia? period)
    {
        return new HieuSuatDTO
        {
            MaHieuSuat = 0,
            MaNhanVien = employee.MaNhanVien,
            TenNhanVien = employee.HoTen,
            MaKyDanhGia = period?.MaKyDanhGia ?? 0,
            TenKyDanhGia = period?.TenKyDanhGia ?? "Chưa có kỳ đánh giá",
            DiemKPI = null,
            KetQuaCongViec = null,
            TyLeHoanThanhDeadline = null,
            SoGioLamViec = null,
            NgayDanhGia = DateTime.Today,
            HieuSuat = 0,
            TrangThaiHoanThanh = "Chưa đánh giá",
            HeSoLuongHieuSuat = 0,
            LuongDuKien = employee.MucLuong
        };
    }

    private async Task<KyDanhGia> EnsureReferencesExistAsync(int maNhanVien, int maKyDanhGia)
    {
        var nhanVien = await _nhanVienRepo.GetByIdAsync(maNhanVien);
        if (nhanVien == null)
            throw new Exception("Không tìm thấy nhân viên.");

        var kyDanhGia = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);
        if (kyDanhGia == null)
            throw new Exception("Không tìm thấy kỳ đánh giá.");

        return kyDanhGia;
    }

    private static void ValidateKyDanhGia(KyDanhGiaDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.TenKyDanhGia))
            throw new Exception("Tên kỳ đánh giá không được để trống.");

        if (dto.NgayBatDau.Date > dto.NgayKetThuc.Date)
            throw new Exception("Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");

        if (string.IsNullOrWhiteSpace(dto.TrangThai))
            throw new Exception("Trạng thái kỳ đánh giá không được để trống.");
    }

    private static void ValidateScoreRanges(HieuSuatDTO dto)
    {
        ValidateSingleScore(dto.DiemKPI, nameof(dto.DiemKPI));
        ValidateSingleScore(dto.TyLeHoanThanhDeadline, nameof(dto.TyLeHoanThanhDeadline));

        if (dto.SoGioLamViec.HasValue && dto.SoGioLamViec.Value < 0)
            throw new Exception("SoGioLamViec không được nhỏ hơn 0.");
    }

    private static void ValidateSingleScore(decimal? value, string fieldName)
    {
        if (!value.HasValue)
            return;

        if (value.Value < 0m || value.Value > 100m)
            throw new Exception($"{fieldName} phải nằm trong khoảng từ 0 đến 100.");
    }

    private static decimal? CalculateFinalScore(HieuSuatDTO dto)
    {
        return CalculateFinalScore(dto.DiemKPI, dto.TyLeHoanThanhDeadline);
    }

    private static decimal? CalculateFinalScore(decimal? diemKpi, decimal? tyLeDeadline)
    {
        if (!diemKpi.HasValue && !tyLeDeadline.HasValue)
            return null;

        if (diemKpi.HasValue && !tyLeDeadline.HasValue)
            return Math.Round(diemKpi.Value, 2);

        if (!diemKpi.HasValue && tyLeDeadline.HasValue)
            return Math.Round(tyLeDeadline.Value, 2);

        var score = (diemKpi!.Value * 0.7m) + (tyLeDeadline!.Value * 0.3m);
        return Math.Round(score, 2);
    }

    private static string EvaluateCompletionStatus(decimal? tyLeDeadline, decimal? score)
    {
        var progress = tyLeDeadline ?? score ?? 0m;
        if (progress >= 100m)
            return "Hoàn thành vượt mức";
        if (progress >= 85m)
            return "Hoàn thành";
        if (progress >= 70m)
            return "Hoàn thành một phần";
        return "Chưa hoàn thành";
    }

    private static decimal CalculateSalaryCoefficient(decimal? score, decimal? tyLeDeadline)
    {
        var s = score ?? 0m;
        decimal coefficient = s switch
        {
            >= 95m => 0.20m,
            >= 90m => 0.15m,
            >= 80m => 0.10m,
            >= 70m => 0.05m,
            < 60m => -0.10m,
            _ => 0m
        };

        if (tyLeDeadline.HasValue && tyLeDeadline.Value < 70m)
            coefficient -= 0.05m;

        return Math.Clamp(coefficient, -0.20m, 0.30m);
    }

    private static decimal CalculateProjectedSalary(decimal baseSalary, decimal coefficient, decimal? soGioLamViec)
    {
        var heSoGio = 1m;
        if (soGioLamViec.HasValue && GioChuanThang > 0)
        {
            var ratio = soGioLamViec.Value / GioChuanThang;
            heSoGio = Math.Clamp(ratio, 0.8m, 1.2m);
        }

        var gross = baseSalary * (1m + coefficient) * heSoGio;
        return Math.Round(gross, 0, MidpointRounding.AwayFromZero);
    }

    private static void ValidateNgayDanhGiaTrongKy(DateTime ngayDanhGia, KyDanhGia kyDanhGia)
    {
        var ngay = ngayDanhGia.Date;
        if (ngay < kyDanhGia.NgayBatDau.Date || ngay > kyDanhGia.NgayKetThuc.Date)
            throw new Exception("Ngày đánh giá phải nằm trong khoảng thời gian của kỳ đánh giá.");
    }

    private static void EnsurePeriodOpenForWrite(KyDanhGia kyDanhGia)
    {
        if (string.Equals(kyDanhGia.TrangThai, TrangThaiKyDaKhoa, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Kỳ đánh giá đã khóa, không thể cập nhật dữ liệu hiệu suất.");
    }

}