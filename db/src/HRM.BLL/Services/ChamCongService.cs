using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class ChamCongService : IChamCongService
{
    private readonly IChamCongRepository _repo;
    private readonly IDonNghiPhepRepository _donNghiPhepRepo;

    public ChamCongService(IChamCongRepository repo, IDonNghiPhepRepository donNghiPhepRepo)
    {
        _repo = repo;
        _donNghiPhepRepo = donNghiPhepRepo;
    }

    public async Task<ChamCongDTO?> CheckInAsync(int maNhanVien)
    {
        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, DateTime.Today))
            throw new InvalidOperationException("Hôm nay bạn có đơn nghỉ phép đã duyệt, không thể chấm công vào ca.");

        var existing = await _repo.GetTodayAsync(maNhanVien);
        if (existing != null) return null; // Đã check-in rồi

        var chamCong = new ChamCong
        {
            MaNhanVien = maNhanVien,
            NgayChamCong = DateTime.Today,
            GioVao = DateTime.Now.TimeOfDay,
            HinhThuc = "Phần mềm",
            TrangThai = DateTime.Now.TimeOfDay > new TimeSpan(8, 30, 0) ? "Đi muộn" : "Bình thường"
        };

        var created = await _repo.AddAsync(chamCong);
        return new ChamCongDTO
        {
            MaChamCong = created.MaChamCong,
            MaNhanVien = created.MaNhanVien,
            NgayChamCong = created.NgayChamCong,
            GioVao = created.GioVao,
            TrangThai = created.TrangThai
        };
    }

    public async Task<ChamCongDTO?> CheckOutAsync(int maNhanVien)
    {
        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, DateTime.Today))
            throw new InvalidOperationException("Hôm nay bạn có đơn nghỉ phép đã duyệt, không thể chấm tan ca.");

        var existing = await _repo.GetTodayAsync(maNhanVien);
        if (existing == null || existing.GioRa != null) return null;

        existing.GioRa = DateTime.Now.TimeOfDay;
        if (existing.GioVao.HasValue)
        {
            var totalHours = (decimal)(existing.GioRa.Value - existing.GioVao.Value).TotalHours;
            existing.TongGioLam = Math.Round(totalHours, 2);
            existing.GioLamThem = totalHours > 8 ? Math.Round(totalHours - 8, 2) : 0;
        }

        if (existing.GioRa < new TimeSpan(17, 0, 0))
        {
            existing.TrangThai = existing.TrangThai == "Đi muộn" ? "Đi muộn và về sớm" : "Về sớm";
        }

        await _repo.UpdateAsync(existing);

        return new ChamCongDTO
        {
            MaChamCong = existing.MaChamCong,
            MaNhanVien = existing.MaNhanVien,
            NgayChamCong = existing.NgayChamCong,
            GioVao = existing.GioVao,
            GioRa = existing.GioRa,
            TongGioLam = existing.TongGioLam,
            TrangThai = existing.TrangThai
        };
    }

    public async Task<ChamCongDTO?> GetTodayAsync(int maNhanVien)
    {
        var cc = await _repo.GetTodayAsync(maNhanVien);
        if (cc == null) return null;
        return MapToDTO(cc);
    }

    public Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay) =>
        _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, ngay.Date);

    public async Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetByNhanVienAsync(maNhanVien, tuNgay, denNgay);
        return list.Select(MapToDTO).ToList();
    }

    public async Task<List<ChamCongDTO>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetAllInPeriodAsync(tuNgay, denNgay);
        return list.Select(MapToDTO).ToList();
    }

    public Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAsync(int maNhanVien, int year, int month) =>
        _repo.GetDistinctNgayChamCongInMonthAsync(maNhanVien, year, month);

    public Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAllAsync(int year, int month) =>
        _repo.GetDistinctNgayChamCongInMonthAllAsync(year, month);

    public async Task UpdateByAdminAsync(int maChamCong, ChamCongAdminUpdateDTO dto)
    {
        var entity = await _repo.GetByIdWithNhanVienAsync(maChamCong);
        if (entity == null)
            throw new InvalidOperationException("Không tìm thấy bản ghi chấm công.");

        var newDay = dto.NgayChamCong.Date;
        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(entity.MaNhanVien, newDay))
            throw new InvalidOperationException("Ngày này nhân viên có đơn nghỉ phép đã duyệt, không thể ghi nhận hoặc sửa chấm công.");

        if (newDay != entity.NgayChamCong.Date)
        {
            var dup = await _repo.ExistsOtherOnSameDayAsync(entity.MaNhanVien, newDay, maChamCong);
            if (dup)
                throw new InvalidOperationException("Nhân viên đã có bản ghi chấm công trong ngày này.");
        }

        entity.NgayChamCong = newDay;
        entity.GioVao = dto.GioVao;
        entity.GioRa = dto.GioRa;
        entity.HinhThuc = string.IsNullOrWhiteSpace(dto.HinhThuc) ? null : dto.HinhThuc.Trim();
        entity.TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "Bình thường" : dto.TrangThai.Trim();
        entity.GhiChu = string.IsNullOrWhiteSpace(dto.GhiChu) ? null : dto.GhiChu.Trim();

        if (entity.GioVao.HasValue && entity.GioRa.HasValue)
        {
            var totalHours = (decimal)(entity.GioRa.Value - entity.GioVao.Value).TotalHours;
            if (totalHours < 0)
                throw new InvalidOperationException("Giờ ra phải sau giờ vào.");
            entity.TongGioLam = Math.Round(totalHours, 2);
            entity.GioLamThem = totalHours > 8 ? Math.Round(totalHours - 8, 2) : 0;
        }
        else
        {
            entity.TongGioLam = null;
            entity.GioLamThem = 0;
        }

        await _repo.UpdateAsync(entity);
    }

    private static ChamCongDTO MapToDTO(ChamCong cc) => new()
    {
        MaChamCong = cc.MaChamCong,
        MaNhanVien = cc.MaNhanVien,
        MaNV = cc.NhanVien?.MaNV,
        TenNhanVien = cc.NhanVien?.HoTen,
        TenPhongBan = cc.NhanVien?.PhongBan?.TenPhongBan,
        TenChucVu = cc.NhanVien?.ChucVu?.TenChucVu,
        NgayChamCong = cc.NgayChamCong,
        GioVao = cc.GioVao,
        GioRa = cc.GioRa,
        TongGioLam = cc.TongGioLam,
        HinhThuc = cc.HinhThuc,
        TrangThai = cc.TrangThai,
        GhiChu = cc.GhiChu
    };
}
