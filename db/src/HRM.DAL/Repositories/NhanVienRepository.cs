using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface INhanVienRepository : IRepository<NhanVien>
{
    Task<List<NhanVien>> GetAllWithDetailsAsync();
    Task<NhanVien?> GetByIdWithDetailsAsync(int id);
    Task<List<NhanVien>> SearchAsync(string keyword);
    Task<List<NhanVien>> FilterAsync(string? keyword, int? maPhongBan, string? trangThai, string? gioiTinh);
}

public class NhanVienRepository : Repository<NhanVien>, INhanVienRepository
{
    public NhanVienRepository(HrmDbContext context) : base(context) { }

    public async Task<List<NhanVien>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(nv => nv.PhongBan)
            .Include(nv => nv.ChucVu)
            .OrderBy(nv => nv.HoTen)
            .ToListAsync();
    }

    public async Task<NhanVien?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(nv => nv.PhongBan)
            .Include(nv => nv.ChucVu)
            .Include(nv => nv.TaiLieus)
            .FirstOrDefaultAsync(nv => nv.MaNhanVien == id);
    }

    public async Task<List<NhanVien>> SearchAsync(string keyword)
    {
        var kw = keyword.Trim().ToLower();
        return await _dbSet
            .Include(nv => nv.PhongBan)
            .Include(nv => nv.ChucVu)
            .Where(nv =>
                nv.HoTen.ToLower().Contains(kw)
                || (nv.MaNV != null && nv.MaNV.ToLower().Contains(kw))
                || (nv.Email != null && nv.Email.ToLower().Contains(kw))
                || (nv.SoDienThoai != null && nv.SoDienThoai.Contains(kw))
                || (nv.CCCD != null && nv.CCCD.Contains(kw))
                || (nv.PhongBan != null && nv.PhongBan.TenPhongBan.ToLower().Contains(kw))
                || (nv.ChucVu != null && nv.ChucVu.TenChucVu.ToLower().Contains(kw))
                || (nv.DiaChi != null && nv.DiaChi.ToLower().Contains(kw)))
            .OrderBy(nv => nv.HoTen)
            .ToListAsync();
    }

    public async Task<List<NhanVien>> FilterAsync(string? keyword, int? maPhongBan, string? trangThai, string? gioiTinh)
    {
        var query = _dbSet
            .Include(nv => nv.PhongBan)
            .Include(nv => nv.ChucVu)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(nv =>
                nv.HoTen.ToLower().Contains(kw)
                || (nv.MaNV != null && nv.MaNV.ToLower().Contains(kw))
                || (nv.Email != null && nv.Email.ToLower().Contains(kw))
                || (nv.SoDienThoai != null && nv.SoDienThoai.Contains(kw))
                || (nv.CCCD != null && nv.CCCD.Contains(kw))
                || (nv.PhongBan != null && nv.PhongBan.TenPhongBan.ToLower().Contains(kw))
                || (nv.ChucVu != null && nv.ChucVu.TenChucVu.ToLower().Contains(kw)));
        }

        if (maPhongBan.HasValue)
            query = query.Where(nv => nv.MaPhongBan == maPhongBan.Value);

        if (!string.IsNullOrWhiteSpace(trangThai) && trangThai != "Tất cả")
            query = query.Where(nv => nv.TrangThai == trangThai);

        if (!string.IsNullOrWhiteSpace(gioiTinh) && gioiTinh != "Tất cả")
            query = query.Where(nv => nv.GioiTinh == gioiTinh);

        return await query.OrderBy(nv => nv.HoTen).ToListAsync();
    }
}
