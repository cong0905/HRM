using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Common.Constants;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface IDonNghiPhepRepository : IRepository<DonNghiPhep>
{
    Task<List<DonNghiPhep>> GetByNhanVienAsync(int maNhanVien);
    Task<List<DonNghiPhep>> GetPendingAsync();
    Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay);
    Task<List<DonNghiPhep>> SearchAllAsync(string? keywordTenHoacMa, DateTime? tuNgay, DateTime? denNgay);
}

public class DonNghiPhepRepository : Repository<DonNghiPhep>, IDonNghiPhepRepository
{
    public DonNghiPhepRepository(HrmDbContext context) : base(context) { }

    public async Task<List<DonNghiPhep>> GetByNhanVienAsync(int maNhanVien)
    {
        return await _dbSet
            .Include(d => d.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(d => d.NhanVien).ThenInclude(n => n.ChucVu)
            .Include(d => d.LoaiNghiPhep)
            .Include(d => d.NguoiPheDuyetNav)
            .Where(d => d.MaNhanVien == maNhanVien)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync();
    }

    public async Task<List<DonNghiPhep>> GetPendingAsync()
    {
        return await _dbSet
            .Include(d => d.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(d => d.NhanVien).ThenInclude(n => n.ChucVu)
            .Include(d => d.LoaiNghiPhep)
            .Where(d => d.TrangThai == DonNghiPhepTrangThai.ChoDuyet)
            .OrderBy(d => d.NgayTao)
            .ToListAsync();
    }

    public async Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay)
    {
        var d = ngay.Date;
        return await _dbSet.AnyAsync(x =>
            x.MaNhanVien == maNhanVien
            && x.TrangThai == DonNghiPhepTrangThai.DaDuyet
            && x.NgayBatDau.Date <= d
            && x.NgayKetThuc.Date >= d);
    }

    public async Task<List<DonNghiPhep>> SearchAllAsync(string? keywordTenHoacMa, DateTime? tuNgay, DateTime? denNgay)
    {
        IQueryable<DonNghiPhep> q = _dbSet
            .AsNoTracking()
            .Include(d => d.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(d => d.NhanVien).ThenInclude(n => n.ChucVu)
            .Include(d => d.LoaiNghiPhep)
            .Include(d => d.NguoiPheDuyetNav);

        if (!string.IsNullOrWhiteSpace(keywordTenHoacMa))
        {
            var k = keywordTenHoacMa.Trim();
            if (int.TryParse(k, out var maHeThong))
            {
                q = q.Where(d =>
                    d.MaNhanVien == maHeThong
                    || d.NhanVien.HoTen.Contains(k)
                    || (d.NhanVien.MaNV != null && d.NhanVien.MaNV.Contains(k)));
            }
            else
            {
                q = q.Where(d =>
                    d.NhanVien.HoTen.Contains(k)
                    || (d.NhanVien.MaNV != null && d.NhanVien.MaNV.Contains(k)));
            }
        }

        if (tuNgay.HasValue && denNgay.HasValue)
        {
            var t = tuNgay.Value.Date;
            var e = denNgay.Value.Date;
            if (t > e)
                (t, e) = (e, t);
            q = q.Where(d => d.NgayBatDau <= e && d.NgayKetThuc >= t);
        }
        else if (tuNgay.HasValue)
        {
            var t = tuNgay.Value.Date;
            q = q.Where(d => d.NgayKetThuc >= t);
        }
        else if (denNgay.HasValue)
        {
            var e = denNgay.Value.Date;
            q = q.Where(d => d.NgayBatDau <= e);
        }

        return await q
            .OrderByDescending(d => d.NgayTao)
            .ThenByDescending(d => d.MaDonPhep)
            .ToListAsync();
    }
}
