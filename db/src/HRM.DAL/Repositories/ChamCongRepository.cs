using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface IChamCongRepository : IRepository<ChamCong>
{
    Task<ChamCong?> GetByIdWithNhanVienAsync(int maChamCong);
    Task<bool> ExistsOtherOnSameDayAsync(int maNhanVien, DateTime ngay, int excludeMaChamCong);
    Task<bool> ExistsAnyAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCong>> GetByNhanVienAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCong>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay);
    Task<ChamCong?> GetTodayAsync(int maNhanVien);
    Task<List<DateTime>> GetDistinctNgayChamCongInMonthAsync(int maNhanVien, int year, int month);
    Task<List<DateTime>> GetDistinctNgayChamCongInMonthAllAsync(int year, int month);
}

public class ChamCongRepository : Repository<ChamCong>, IChamCongRepository
{
    public ChamCongRepository(HrmDbContext context) : base(context) { }

    public async Task<ChamCong?> GetByIdWithNhanVienAsync(int maChamCong)
    {
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
            .FirstOrDefaultAsync(cc => cc.MaChamCong == maChamCong);
    }

    public async Task<bool> ExistsOtherOnSameDayAsync(int maNhanVien, DateTime ngay, int excludeMaChamCong)
    {
        var d = ngay.Date;
        var next = d.AddDays(1);
        return await _dbSet.AnyAsync(cc =>
            cc.MaNhanVien == maNhanVien
            && cc.NgayChamCong >= d && cc.NgayChamCong < next
            && cc.MaChamCong != excludeMaChamCong);
    }

    public async Task<bool> ExistsAnyAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet.AnyAsync(cc =>
            cc.MaNhanVien == maNhanVien
            && cc.NgayChamCong >= from
            && cc.NgayChamCong < toExclusive);
    }

    public async Task<List<ChamCong>> GetByNhanVienAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
            .Where(cc => cc.MaNhanVien == maNhanVien
                && cc.NgayChamCong >= from
                && cc.NgayChamCong < toExclusive)
            .OrderByDescending(cc => cc.NgayChamCong)
            .ToListAsync();
    }

    public async Task<List<ChamCong>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
            .Where(cc => cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .OrderByDescending(cc => cc.NgayChamCong)
            .ThenBy(cc => cc.MaNhanVien)
            .ToListAsync();
    }

    public async Task<ChamCong?> GetTodayAsync(int maNhanVien)
    {
        var today = DateTime.Today;
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
            .FirstOrDefaultAsync(cc => cc.MaNhanVien == maNhanVien && cc.NgayChamCong == today);
    }

    public async Task<List<DateTime>> GetDistinctNgayChamCongInMonthAsync(int maNhanVien, int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var toExclusive = from.AddMonths(1);
        return await _dbSet
            .AsNoTracking()
            .Where(cc => cc.MaNhanVien == maNhanVien
                && cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .Select(cc => cc.NgayChamCong.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<List<DateTime>> GetDistinctNgayChamCongInMonthAllAsync(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var toExclusive = from.AddMonths(1);
        return await _dbSet
            .AsNoTracking()
            .Where(cc => cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .Select(cc => cc.NgayChamCong.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }
}
