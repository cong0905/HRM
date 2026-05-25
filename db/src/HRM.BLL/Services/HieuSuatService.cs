using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

using Microsoft.Extensions.Configuration;

namespace HRM.BLL.Services;

public class HieuSuatService : IHieuSuatService
{
    private const string TrangThaiKyDaKhoa = "Đã khóa";

    private readonly IRepository<HieuSuatNhanVien> _hieuSuatRepo;
    private readonly INhanVienRepository _nhanVienRepo;
    private readonly IRepository<KyDanhGia> _kyDanhGiaRepo;
    private readonly IChamCongRepository _chamCongRepo;
    private readonly IConfiguration _configuration;

    public HieuSuatService(
        IRepository<HieuSuatNhanVien> hieuSuatRepo,
        INhanVienRepository nhanVienRepo,
        IRepository<KyDanhGia> kyDanhGiaRepo,
        IChamCongRepository chamCongRepo,
        IConfiguration configuration)
    {
        _hieuSuatRepo = hieuSuatRepo;
        _nhanVienRepo = nhanVienRepo;
        _kyDanhGiaRepo = kyDanhGiaRepo;
        _chamCongRepo = chamCongRepo;
        _configuration = configuration;
    }

    public async Task<List<HieuSuatDTO>> GetAllAsync()
    {
        var danhSachHieuSuat = await _hieuSuatRepo.GetAllAsync();
        if (danhSachHieuSuat.Count > 0)
            return await MapListAsync(danhSachHieuSuat);

        var danhSachNhanVien = await _nhanVienRepo.GetAllAsync();
        var danhSachKyDanhGia = await _kyDanhGiaRepo.GetAllAsync();
        var kyGanNhat = danhSachKyDanhGia
            .OrderByDescending(x => x.NgayBatDau)
            .FirstOrDefault();

        return danhSachNhanVien
            .OrderBy(x => x.HoTen)
            .Select(nhanVien => CreateDefaultDto(nhanVien, kyGanNhat))
            .ToList();
    }

    public async Task<List<HieuSuatDTO>> GetByNhanVienAsync(int maNhanVien)
    {
        var danhSach = await _hieuSuatRepo.FindAsync(x => x.MaNhanVien == maNhanVien);
        return await MapListAsync(danhSach);
    }

    public async Task<List<HieuSuatDTO>> GetByKyDanhGiaAsync(int maKyDanhGia)
    {
        var danhSach = await _hieuSuatRepo.FindAsync(x => x.MaKyDanhGia == maKyDanhGia);
        var danhSachDaChuyenDoi = await MapListAsync(danhSach);

        var danhSachNhanVien = await _nhanVienRepo.GetAllAsync();
        var kyDanhGiaHienTai = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);

        var maNhanVienDaCo = danhSachDaChuyenDoi
            .Select(x => x.MaNhanVien)
            .ToHashSet();

        var danhSachThieu = danhSachNhanVien
            .Where(nhanVien => !maNhanVienDaCo.Contains(nhanVien.MaNhanVien))
            .OrderBy(nhanVien => nhanVien.HoTen)
            .Select(nhanVien => CreateDefaultDto(nhanVien, kyDanhGiaHienTai))
            .ToList();

        danhSachDaChuyenDoi.AddRange(danhSachThieu);
        return danhSachDaChuyenDoi
            .OrderBy(x => x.TenNhanVien)
            .ThenByDescending(x => x.NgayDanhGia)
            .ToList();
    }

    public async Task<List<KyDanhGiaDTO>> GetKyDanhGiaAsync()
    {
        var danhSachKyDanhGia = await _kyDanhGiaRepo.GetAllAsync();
        return danhSachKyDanhGia
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

        var banGhiDaTonTai = await _kyDanhGiaRepo.FindAsync(x => x.TenKyDanhGia == dto.TenKyDanhGia.Trim());
        if (banGhiDaTonTai.Count > 0)
            throw new Exception("Tên kỳ đánh giá đã tồn tại.");

        var kyDanhGiaMoi = await _kyDanhGiaRepo.AddAsync(new KyDanhGia
        {
            TenKyDanhGia = dto.TenKyDanhGia.Trim(),
            NgayBatDau = dto.NgayBatDau.Date,
            NgayKetThuc = dto.NgayKetThuc.Date,
            TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "Mở" : dto.TrangThai.Trim()
        });

        return new KyDanhGiaDTO
        {
            MaKyDanhGia = kyDanhGiaMoi.MaKyDanhGia,
            TenKyDanhGia = kyDanhGiaMoi.TenKyDanhGia,
            NgayBatDau = kyDanhGiaMoi.NgayBatDau,
            NgayKetThuc = kyDanhGiaMoi.NgayKetThuc,
            TrangThai = kyDanhGiaMoi.TrangThai
        };
    }

    public async Task UpdateKyDanhGiaAsync(int maKyDanhGia, KyDanhGiaDTO dto)
    {
        ValidateKyDanhGia(dto);

        var kyDanhGiaCanCapNhat = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);
        if (kyDanhGiaCanCapNhat == null)
            throw new Exception("Không tìm thấy kỳ đánh giá.");

        var banSaoTrung = await _kyDanhGiaRepo.FindAsync(x => x.TenKyDanhGia == dto.TenKyDanhGia.Trim() && x.MaKyDanhGia != maKyDanhGia);
        if (banSaoTrung.Count > 0)
            throw new Exception("Tên kỳ đánh giá đã tồn tại.");

        kyDanhGiaCanCapNhat.TenKyDanhGia = dto.TenKyDanhGia.Trim();
        kyDanhGiaCanCapNhat.NgayBatDau = dto.NgayBatDau.Date;
        kyDanhGiaCanCapNhat.NgayKetThuc = dto.NgayKetThuc.Date;
        kyDanhGiaCanCapNhat.TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? kyDanhGiaCanCapNhat.TrangThai : dto.TrangThai.Trim();

        await _kyDanhGiaRepo.UpdateAsync(kyDanhGiaCanCapNhat);
    }

    public async Task DeleteKyDanhGiaAsync(int maKyDanhGia)
    {
        var kyDanhGiaCanXoa = await _kyDanhGiaRepo.GetByIdAsync(maKyDanhGia);
        if (kyDanhGiaCanXoa == null)
            throw new Exception("Không tìm thấy kỳ đánh giá.");

        var soBanGhiLienKet = await _hieuSuatRepo.CountAsync(x => x.MaKyDanhGia == maKyDanhGia);
        if (soBanGhiLienKet > 0)
            throw new Exception("Kỳ đánh giá đã có dữ liệu hiệu suất, không thể xóa.");

        await _kyDanhGiaRepo.DeleteAsync(kyDanhGiaCanXoa);
    }

    public async Task<HieuSuatDTO> CreateAsync(HieuSuatDTO dto)
    {
        var kyDanhGia = await EnsureReferencesExistAsync(dto.MaNhanVien, dto.MaKyDanhGia);
        DamBaoKyMoCuaGhi(kyDanhGia);

        var soBanGhiDaTonTai = await _hieuSuatRepo.CountAsync(x =>
            x.MaNhanVien == dto.MaNhanVien
            && x.MaKyDanhGia == dto.MaKyDanhGia);
        if (soBanGhiDaTonTai > 0)
            throw new Exception("Nhân viên đã có bản ghi hiệu suất trong kỳ đánh giá này.");

        var ngayDanhGia = dto.NgayDanhGia == default ? DateTime.Now : dto.NgayDanhGia;
        KiemTraNgayDanhGiaTrongKy(ngayDanhGia, kyDanhGia);

        var chiSoTuDong = await TinhChiSoTuDongAsync(dto.MaNhanVien, kyDanhGia);
        dto.DiemChuyenCan = chiSoTuDong.DiemChuyenCan;
        dto.TyLeDiLam = chiSoTuDong.TyLeDiLam;
        dto.TyLeGioLam = chiSoTuDong.TyLeGioLam;
        dto.TyLeDungGio = chiSoTuDong.TyLeDungGio;
        dto.SoGioLamViec = chiSoTuDong.SoGioLamViec;

        KiemTraKhoangDiem(dto);

        var diemTongKet = TinhDiemHieuSuatCuoiCung(dto.DiemChuyenCan, dto.DiemKPI, dto.TyLeHoanThanhDeadline);
        var trangThaiCongViec = DanhGiaTrangThaiHoanThanh(dto.TyLeHoanThanhDeadline, diemTongKet);

        var banGhiMoi = new HieuSuatNhanVien
        {
            MaNhanVien = dto.MaNhanVien,
            MaKyDanhGia = dto.MaKyDanhGia,
            DiemKPI = dto.DiemKPI,
            NhanXetCuaQuanLy = dto.NhanXetCuaQuanLy,
            TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline,
            SoGioLamViec = dto.SoGioLamViec,
            DiemChuyenCan = dto.DiemChuyenCan,
            TyLeDiLam = dto.TyLeDiLam,
            TyLeGioLam = dto.TyLeGioLam,
            TyLeDungGio = dto.TyLeDungGio,
            NgayDanhGia = ngayDanhGia
        };

        var banGhiDaTao = await _hieuSuatRepo.AddAsync(banGhiMoi);
        return await MapAsync(banGhiDaTao);
    }

    public async Task UpdateAsync(int maHieuSuat, HieuSuatDTO dto)
    {
        var banGhiCanCapNhat = await _hieuSuatRepo.GetByIdAsync(maHieuSuat);
        if (banGhiCanCapNhat == null)
            throw new Exception("Không tìm thấy bản ghi hiệu suất.");

        var kyDanhGia = await EnsureReferencesExistAsync(dto.MaNhanVien, dto.MaKyDanhGia);
        DamBaoKyMoCuaGhi(kyDanhGia);

        var banSaoTrung = await _hieuSuatRepo.CountAsync(x =>
            x.MaHieuSuat != maHieuSuat
            && x.MaNhanVien == dto.MaNhanVien
            && x.MaKyDanhGia == dto.MaKyDanhGia);
        if (banSaoTrung > 0)
            throw new Exception("Nhân viên đã có bản ghi hiệu suất trong kỳ đánh giá này.");

        var ngayDanhGia = dto.NgayDanhGia == default ? banGhiCanCapNhat.NgayDanhGia : dto.NgayDanhGia;
        KiemTraNgayDanhGiaTrongKy(ngayDanhGia, kyDanhGia);

        var chiSoTuDong = await TinhChiSoTuDongAsync(dto.MaNhanVien, kyDanhGia);
        dto.DiemChuyenCan = chiSoTuDong.DiemChuyenCan;
        dto.TyLeDiLam = chiSoTuDong.TyLeDiLam;
        dto.TyLeGioLam = chiSoTuDong.TyLeGioLam;
        dto.TyLeDungGio = chiSoTuDong.TyLeDungGio;
        dto.SoGioLamViec = chiSoTuDong.SoGioLamViec;

        KiemTraKhoangDiem(dto);

        var diemTongKet = TinhDiemHieuSuatCuoiCung(dto.DiemChuyenCan, dto.DiemKPI, dto.TyLeHoanThanhDeadline);
        var trangThaiCongViec = DanhGiaTrangThaiHoanThanh(dto.TyLeHoanThanhDeadline, diemTongKet);

        banGhiCanCapNhat.MaNhanVien = dto.MaNhanVien;
        banGhiCanCapNhat.MaKyDanhGia = dto.MaKyDanhGia;
        banGhiCanCapNhat.DiemKPI = dto.DiemKPI;
        banGhiCanCapNhat.NhanXetCuaQuanLy = dto.NhanXetCuaQuanLy;
        banGhiCanCapNhat.TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline;
        banGhiCanCapNhat.SoGioLamViec = dto.SoGioLamViec;
        banGhiCanCapNhat.DiemChuyenCan = dto.DiemChuyenCan;
        banGhiCanCapNhat.TyLeDiLam = dto.TyLeDiLam;
        banGhiCanCapNhat.TyLeGioLam = dto.TyLeGioLam;
        banGhiCanCapNhat.TyLeDungGio = dto.TyLeDungGio;
        banGhiCanCapNhat.NgayDanhGia = ngayDanhGia;

        await _hieuSuatRepo.UpdateAsync(banGhiCanCapNhat);
    }

    public async Task DeleteAsync(int maHieuSuat)
    {
        var banGhiCanXoa = await _hieuSuatRepo.GetByIdAsync(maHieuSuat);
        if (banGhiCanXoa == null)
            throw new Exception("Không tìm thấy bản ghi hiệu suất.");

        await _hieuSuatRepo.DeleteAsync(banGhiCanXoa);
    }

    private async Task<List<HieuSuatDTO>> MapListAsync(IEnumerable<HieuSuatNhanVien> list)
    {
        var employeeIds = list.Select(x => x.MaNhanVien).Distinct().ToList();
        var periodIds = list.Select(x => x.MaKyDanhGia).Distinct().ToList();

        var danhSachNhanVien = await _nhanVienRepo.FindAsync(x => employeeIds.Contains(x.MaNhanVien));
        var banDoNhanVien = danhSachNhanVien.ToDictionary(x => x.MaNhanVien);

        var danhSachKyDanhGia = await _kyDanhGiaRepo.FindAsync(x => periodIds.Contains(x.MaKyDanhGia));
        var banDoKyDanhGia = danhSachKyDanhGia.ToDictionary(x => x.MaKyDanhGia);

        return list
            .OrderByDescending(x => x.NgayDanhGia)
            .Select(banGhi => MapToDto(banGhi, banDoNhanVien, banDoKyDanhGia))
            .ToList();
    }

    private async Task<HieuSuatDTO> MapAsync(HieuSuatNhanVien entity)
    {
        var nhanVien = await _nhanVienRepo.GetByIdAsync(entity.MaNhanVien);
        var kyDanhGia = await _kyDanhGiaRepo.GetByIdAsync(entity.MaKyDanhGia);

        var banDoNhanVien = nhanVien != null ? new Dictionary<int, NhanVien> { { nhanVien.MaNhanVien, nhanVien } } : new Dictionary<int, NhanVien>();
        var banDoKyDanhGia = kyDanhGia != null ? new Dictionary<int, KyDanhGia> { { kyDanhGia.MaKyDanhGia, kyDanhGia } } : new Dictionary<int, KyDanhGia>();

        return MapToDto(entity, banDoNhanVien, banDoKyDanhGia);
    }

    private HieuSuatDTO MapToDto(
        HieuSuatNhanVien entity,
        IReadOnlyDictionary<int, NhanVien> employeeMap,
        IReadOnlyDictionary<int, KyDanhGia> periodMap)
    {
        employeeMap.TryGetValue(entity.MaNhanVien, out var nhanVien);
        periodMap.TryGetValue(entity.MaKyDanhGia, out var kyDanhGiaThongTin);

        var diemTong = TinhDiemHieuSuatCuoiCung(entity.DiemChuyenCan, entity.DiemKPI, entity.TyLeHoanThanhDeadline);
        var trangThaiCongViec = DanhGiaTrangThaiHoanThanh(entity.TyLeHoanThanhDeadline, diemTong);
        var bonusHieuSuat = TinhBonusHieuSuat(diemTong);
        var heSoChuyenCan = Math.Min((entity.TyLeDiLam ?? 0m) / 100m, 1.0m);
        var luongDuKien = Math.Round((nhanVien?.MucLuong ?? 0m) * heSoChuyenCan * (1m + bonusHieuSuat), 0, MidpointRounding.AwayFromZero);

        return new HieuSuatDTO
        {
            MaHieuSuat = entity.MaHieuSuat,
            MaNhanVien = entity.MaNhanVien,
            TenNhanVien = nhanVien?.HoTen,
            MaKyDanhGia = entity.MaKyDanhGia,
            TenKyDanhGia = kyDanhGiaThongTin?.TenKyDanhGia,
            DiemKPI = entity.DiemKPI,
            NhanXetCuaQuanLy = entity.NhanXetCuaQuanLy,
            TyLeHoanThanhDeadline = entity.TyLeHoanThanhDeadline,
            SoGioLamViec = entity.SoGioLamViec,
            DiemChuyenCan = entity.DiemChuyenCan,
            TyLeDiLam = entity.TyLeDiLam,
            TyLeGioLam = entity.TyLeGioLam,
            TyLeDungGio = entity.TyLeDungGio,
            NgayDanhGia = entity.NgayDanhGia,
            DiemHieuSuatTong = diemTong ?? 0m,
            TrangThaiHoanThanh = trangThaiCongViec,
            HeSoLuongHieuSuat = bonusHieuSuat,
            LuongDuKien = luongDuKien
        };
    }

    private static HieuSuatDTO CreateDefaultDto(NhanVien employee, KyDanhGia? kyDanhGiaThongTin)
    {
        return new HieuSuatDTO
        {
            MaHieuSuat = 0,
            MaNhanVien = employee.MaNhanVien,
            TenNhanVien = employee.HoTen,
            MaKyDanhGia = kyDanhGiaThongTin?.MaKyDanhGia ?? 0,
            TenKyDanhGia = kyDanhGiaThongTin?.TenKyDanhGia ?? "Chưa có kỳ đánh giá",
            DiemKPI = null,
            NhanXetCuaQuanLy = null,
            TyLeHoanThanhDeadline = null,
            SoGioLamViec = null,
            DiemChuyenCan = null,
            TyLeDiLam = null,
            TyLeGioLam = null,
            TyLeDungGio = null,
            NgayDanhGia = DateTime.Today,
            DiemHieuSuatTong = 0,
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

    private static void KiemTraKhoangDiem(HieuSuatDTO dto)
    {
        KiemTraDiemDon(dto.DiemKPI, nameof(dto.DiemKPI));
        KiemTraDiemDon(dto.TyLeHoanThanhDeadline, nameof(dto.TyLeHoanThanhDeadline));

        if (dto.SoGioLamViec.HasValue && dto.SoGioLamViec.Value < 0)
            throw new Exception("SoGioLamViec không được nhỏ hơn 0.");
    }

    private static void KiemTraDiemDon(decimal? giaTri, string tenTruong)
    {
        if (!giaTri.HasValue)
            return;

        if (giaTri.Value < 0m || giaTri.Value > 100m)
            throw new Exception($"{tenTruong} phải nằm trong khoảng từ 0 đến 100.");
    }

    internal static decimal? TinhDiemHieuSuatCuoiCung(HieuSuatDTO duLieu)
    {
        return TinhDiemHieuSuatCuoiCung(duLieu.DiemChuyenCan, duLieu.DiemKPI, duLieu.TyLeHoanThanhDeadline);
    }

    internal static decimal? TinhDiemHieuSuatCuoiCung(decimal? diemChuyenCan, decimal? diemKpi, decimal? tyLeDeadline)
    {
        var cc = diemChuyenCan ?? 0m;
        var kpi = diemKpi ?? 0m;
        var dl = tyLeDeadline ?? 0m;

        if (diemChuyenCan == null && diemKpi == null && tyLeDeadline == null) return null;

        var score = (cc * 0.3m) + (kpi * 0.4m) + (dl * 0.3m);
        return Math.Round(score, 2);
    }

    internal static string DanhGiaTrangThaiHoanThanh(decimal? tyLeDeadline, decimal? diemTong)
    {
        var mucTienTrien = diemTong ?? 0m;
        if (mucTienTrien >= 90m) return "Hoàn thành vượt mức";
        if (mucTienTrien >= 70m) return "Hoàn thành";
        if (mucTienTrien >= 50m) return "Hoàn thành một phần";
        return "Chưa hoàn thành";
    }

    internal static decimal TinhBonusHieuSuat(decimal? diemTong)
    {
        var diemXepLoai = diemTong ?? 0m;
        return diemXepLoai switch
        {
            >= 95m => 0.25m,
            >= 85m => 0.15m,
            >= 75m => 0.08m,
            >= 65m => 0.03m,
            >= 50m => 0m,
            _ => -0.10m
        };
    }

    private static void KiemTraNgayDanhGiaTrongKy(DateTime ngayDanhGia, KyDanhGia kyDanhGia)
    {
        var ngay = ngayDanhGia.Date;
        if (ngay < kyDanhGia.NgayBatDau.Date || ngay > kyDanhGia.NgayKetThuc.Date)
            throw new Exception("Ngày đánh giá phải nằm trong khoảng thời gian của kỳ đánh giá.");
    }

    private static void DamBaoKyMoCuaGhi(KyDanhGia kyDanhGia)
    {
        if (string.Equals(kyDanhGia.TrangThai, TrangThaiKyDaKhoa, StringComparison.OrdinalIgnoreCase))
            throw new Exception("Kỳ đánh giá đã khóa, không thể cập nhật dữ liệu hiệu suất.");
    }

    private async Task<(decimal DiemChuyenCan, decimal TyLeDiLam, decimal TyLeGioLam, decimal TyLeDungGio, decimal SoGioLamViec)> TinhChiSoTuDongAsync(int maNhanVien, KyDanhGia kyDanhGia)
    {
        var danhSachChamCong = await _chamCongRepo.GetByNhanVienAsync(maNhanVien, kyDanhGia.NgayBatDau, kyDanhGia.NgayKetThuc);

        var tongGioLam = danhSachChamCong.Sum(x => x.TongGioLam ?? 0m);
        var soGioLamViec = Math.Round(Math.Max(0m, tongGioLam), 2);

        var soNgayLamViecDuKien = DemSoNgayLamViec(kyDanhGia.NgayBatDau, kyDanhGia.NgayKetThuc);
        var soNgayLamViecThucTe = danhSachChamCong
            .Where(x => (x.TongGioLam ?? 0m) > 0m || x.GioVao.HasValue)
            .Select(x => x.NgayChamCong.Date)
            .Distinct()
            .Count();

        var tyLeDiLam = soNgayLamViecDuKien <= 0
            ? 0m
            : Math.Clamp((soNgayLamViecThucTe * 100m) / soNgayLamViecDuKien, 0m, 100m);

        var gioTieuChuan = soNgayLamViecDuKien * 8m;
        var tyLeGioLam = gioTieuChuan <= 0m
            ? 0m
            : Math.Clamp((soGioLamViec * 100m) / gioTieuChuan, 0m, 120m);

        var standardStartTimeStr = _configuration.GetValue<string>("PerformanceSettings:StandardStartTime", "08:30:00");
        if (!TimeSpan.TryParse(standardStartTimeStr, out var standardStartTime))
        {
            standardStartTime = new TimeSpan(8, 30, 0);
        }

        var danhSachCoGioVao = danhSachChamCong.Where(x => x.GioVao.HasValue).ToList();
        var soLanDungGio = danhSachCoGioVao.Count(x => x.GioVao!.Value <= standardStartTime);
        var tyLeDungGio = danhSachCoGioVao.Count == 0
            ? (soNgayLamViecThucTe > 0 ? 100m : 0m) // Fix: Nếu không đi làm ngày nào thì tỷ lệ đúng giờ = 0
            : (soLanDungGio * 100m) / danhSachCoGioVao.Count;

        var diemChuyenCan = Math.Round((tyLeDiLam * 0.4m) + (tyLeGioLam * 0.4m) + (tyLeDungGio * 0.2m), 2);

        return (diemChuyenCan, Math.Round(tyLeDiLam, 2), Math.Round(tyLeGioLam, 2), Math.Round(tyLeDungGio, 2), soGioLamViec);
    }

    internal static int DemSoNgayLamViec(DateTime tuNgay, DateTime denNgay)
    {
        var ngayBatDau = tuNgay.Date;
        var ngayKetThuc = denNgay.Date;
        if (ngayKetThuc < ngayBatDau)
            return 0;

        var soNgay = 0;
        for (var ngay = ngayBatDau; ngay <= ngayKetThuc; ngay = ngay.AddDays(1))
        {
            if (ngay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            soNgay++;
        }

        return soNgay;
    }

}