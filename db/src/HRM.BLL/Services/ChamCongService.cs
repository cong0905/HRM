using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class ChamCongService : IChamCongService
{
    private readonly IChamCongRepository _repo;

    public ChamCongService(IChamCongRepository repo)
    {
        _repo = repo;
    }

    public async Task<ChamCongDTO?> CheckInAsync(int maNhanVien)
    {
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
        return MapToDto(cc);
    }

    public async Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetByNhanVienAsync(maNhanVien, tuNgay, denNgay);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<ChamCongDTO>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetAllInPeriodAsync(tuNgay, denNgay);
        return list.Select(MapToDto).ToList();
    }

    public Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAsync(int maNhanVien, int year, int month) =>
        _repo.GetDistinctNgayChamCongInMonthAsync(maNhanVien, year, month);

    public Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAllAsync(int year, int month) =>
        _repo.GetDistinctNgayChamCongInMonthAllAsync(year, month);

    private static ChamCongDTO MapToDto(ChamCong cc) => new()
    {
        MaChamCong = cc.MaChamCong,
        MaNhanVien = cc.MaNhanVien,
        TenNhanVien = cc.NhanVien?.HoTen,
        NgayChamCong = cc.NgayChamCong,
        GioVao = cc.GioVao,
        GioRa = cc.GioRa,
        TongGioLam = cc.TongGioLam,
        HinhThuc = cc.HinhThuc,
        TrangThai = cc.TrangThai
    };
}
