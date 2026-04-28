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
    private readonly IChamCongRepository _chamCongRepo;

    public HieuSuatService(
        IRepository<HieuSuatNhanVien> hieuSuatRepo,
        INhanVienRepository nhanVienRepo,
        IRepository<KyDanhGia> kyDanhGiaRepo,
        IChamCongRepository chamCongRepo)
    {
        _hieuSuatRepo = hieuSuatRepo;
        _nhanVienRepo = nhanVienRepo;
        _kyDanhGiaRepo = kyDanhGiaRepo;
        _chamCongRepo = chamCongRepo;
    }

    public async Task<List<HieuSuatDTO>> GetAllAsync()
    {
        var danhSachHieuSuat = await _hieuSuatRepo.GetAllAsync();
        if (danhSachHieuSuat.Count > 0)
            return await MapListAsync(danhSachHieuSuat);

        var danhSachNhanVien = await _nhanVienRepo.GetAllWithDetailsAsync();
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

        var danhSachNhanVien = await _nhanVienRepo.GetAllWithDetailsAsync();
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
        dto.DiemKPI = chiSoTuDong.DiemKpi;
        dto.TyLeHoanThanhDeadline = chiSoTuDong.TyLeDeadline;
        dto.SoGioLamViec = chiSoTuDong.SoGioLamViec;

        KiemTraKhoangDiem(dto);

        var diemTongKet = TinhDiemHieuSuatCuoiCung(dto);
        var trangThaiCongViec = DanhGiaTrangThaiHoanThanh(dto.TyLeHoanThanhDeadline, diemTongKet);

        var banGhiMoi = new HieuSuatNhanVien
        {
            MaNhanVien = dto.MaNhanVien,
            MaKyDanhGia = dto.MaKyDanhGia,
            DiemKPI = dto.DiemKPI,
            KetQuaCongViec = trangThaiCongViec,
            TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline,
            SoGioLamViec = dto.SoGioLamViec,
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
        dto.DiemKPI = chiSoTuDong.DiemKpi;
        dto.TyLeHoanThanhDeadline = chiSoTuDong.TyLeDeadline;
        dto.SoGioLamViec = chiSoTuDong.SoGioLamViec;

        KiemTraKhoangDiem(dto);

        var diemTongKet = TinhDiemHieuSuatCuoiCung(dto);
        var trangThaiCongViec = DanhGiaTrangThaiHoanThanh(dto.TyLeHoanThanhDeadline, diemTongKet);

        banGhiCanCapNhat.MaNhanVien = dto.MaNhanVien;
        banGhiCanCapNhat.MaKyDanhGia = dto.MaKyDanhGia;
        banGhiCanCapNhat.DiemKPI = dto.DiemKPI;
        banGhiCanCapNhat.KetQuaCongViec = trangThaiCongViec;
        banGhiCanCapNhat.TyLeHoanThanhDeadline = dto.TyLeHoanThanhDeadline;
        banGhiCanCapNhat.SoGioLamViec = dto.SoGioLamViec;
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
        var danhSachNhanVien = await _nhanVienRepo.GetAllWithDetailsAsync();
        var banDoNhanVien = danhSachNhanVien.ToDictionary(x => x.MaNhanVien);

        var danhSachKyDanhGia = await _kyDanhGiaRepo.GetAllAsync();
        var banDoKyDanhGia = danhSachKyDanhGia.ToDictionary(x => x.MaKyDanhGia);

        return list
            .OrderByDescending(x => x.NgayDanhGia)
            .Select(banGhi => MapToDto(banGhi, banDoNhanVien, banDoKyDanhGia))
            .ToList();
    }

    private async Task<HieuSuatDTO> MapAsync(HieuSuatNhanVien entity)
    {
        var danhSachNhanVien = await _nhanVienRepo.GetAllWithDetailsAsync();
        var banDoNhanVien = danhSachNhanVien.ToDictionary(x => x.MaNhanVien);

        var danhSachKyDanhGia = await _kyDanhGiaRepo.GetAllAsync();
        var banDoKyDanhGia = danhSachKyDanhGia.ToDictionary(x => x.MaKyDanhGia);

        return MapToDto(entity, banDoNhanVien, banDoKyDanhGia);
    }

    private static HieuSuatDTO MapToDto(
        HieuSuatNhanVien entity,
        IReadOnlyDictionary<int, NhanVien> employeeMap,
        IReadOnlyDictionary<int, KyDanhGia> periodMap)
    {
        employeeMap.TryGetValue(entity.MaNhanVien, out var nhanVien);
        periodMap.TryGetValue(entity.MaKyDanhGia, out var kyDanhGiaThongTin);

        var diemTong = TinhDiemHieuSuatCuoiCung(entity.DiemKPI, entity.TyLeHoanThanhDeadline);
        var trangThaiCongViec = DanhGiaTrangThaiHoanThanh(entity.TyLeHoanThanhDeadline, diemTong);
        var heSoLuongHieuSuat = TinhHeSoLuongHieuSuat(diemTong, entity.TyLeHoanThanhDeadline);
        var luongDuKien = TinhLuongDuKien(nhanVien?.MucLuong ?? 0m, heSoLuongHieuSuat, entity.SoGioLamViec);

        return new HieuSuatDTO
        {
            MaHieuSuat = entity.MaHieuSuat,
            MaNhanVien = entity.MaNhanVien,
            TenNhanVien = nhanVien?.HoTen,
            MaKyDanhGia = entity.MaKyDanhGia,
            TenKyDanhGia = kyDanhGiaThongTin?.TenKyDanhGia,
            DiemKPI = entity.DiemKPI,
            KetQuaCongViec = trangThaiCongViec,
            TyLeHoanThanhDeadline = entity.TyLeHoanThanhDeadline,
            SoGioLamViec = entity.SoGioLamViec,
            NgayDanhGia = entity.NgayDanhGia,
            HieuSuat = diemTong ?? 0m,
            TrangThaiHoanThanh = trangThaiCongViec,
            HeSoLuongHieuSuat = heSoLuongHieuSuat,
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

    private static decimal? TinhDiemHieuSuatCuoiCung(HieuSuatDTO duLieu)
    {
        return TinhDiemHieuSuatCuoiCung(duLieu.DiemKPI, duLieu.TyLeHoanThanhDeadline);
    }

    private static decimal? TinhDiemHieuSuatCuoiCung(decimal? diemKpi, decimal? tyLeDeadline)
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

    private static string DanhGiaTrangThaiHoanThanh(decimal? tyLeDeadline, decimal? diemTong)
    {
        var mucTienTrien = tyLeDeadline ?? diemTong ?? 0m;
        if (mucTienTrien >= 100m)
            return "Hoàn thành vượt mức";
        if (mucTienTrien >= 85m)
            return "Hoàn thành";
        if (mucTienTrien >= 70m)
            return "Hoàn thành một phần";
        return "Chưa hoàn thành";
    }

    private static decimal TinhHeSoLuongHieuSuat(decimal? diemTong, decimal? tyLeDeadline)
    {
        var diemXepLoai = diemTong ?? 0m;
        decimal heSoLuong = diemXepLoai switch
        {
            >= 95m => 0.20m,
            >= 90m => 0.15m,
            >= 80m => 0.10m,
            >= 70m => 0.05m,
            < 60m => -0.10m,
            _ => 0m
        };

        if (tyLeDeadline.HasValue && tyLeDeadline.Value < 70m)
            heSoLuong -= 0.05m;

        return Math.Clamp(heSoLuong, -0.20m, 0.30m);
    }
// LuongDuKien=LuongCoBan×(1+HeSoHieuSuat)×HeSoGio
    private static decimal TinhLuongDuKien(decimal luongCoBan, decimal heSoLuong, decimal? soGioLamViec)
    {
        var heSoGio = 1m;
        if (soGioLamViec.HasValue && GioChuanThang > 0)
        {
            var tyLeGio = soGioLamViec.Value / GioChuanThang;
            heSoGio = Math.Clamp(tyLeGio, 0.8m, 1.2m);
        }

        var luongTong = luongCoBan * (1m + heSoLuong) * heSoGio;
        return Math.Round(luongTong, 0, MidpointRounding.AwayFromZero);
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

    private async Task<(decimal DiemKpi, decimal TyLeDeadline, decimal SoGioLamViec)> TinhChiSoTuDongAsync(int maNhanVien, KyDanhGia kyDanhGia)
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

        var danhSachCoGioVao = danhSachChamCong.Where(x => x.GioVao.HasValue).ToList();
        var soLanDungGio = danhSachCoGioVao.Count(x => x.GioVao!.Value <= new TimeSpan(8, 30, 0));
        var tyLeDungGio = danhSachCoGioVao.Count == 0
            ? 100m
            : (soLanDungGio * 100m) / danhSachCoGioVao.Count;

        var tyLeDeadline = Math.Round(
            Math.Clamp((tyLeDiLam * 0.5m) + (tyLeGioLam * 0.3m) + (tyLeDungGio * 0.2m), 0m, 100m),
            2);

        var diemKpi = Math.Round(Math.Clamp((tyLeGioLam * 0.7m) + (tyLeDungGio * 0.3m), 0m, 100m), 2);

        return (diemKpi, tyLeDeadline, soGioLamViec);
    }

    private static int DemSoNgayLamViec(DateTime tuNgay, DateTime denNgay)
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