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
        return await _dbSet
            .Include(nv => nv.PhongBan)
            .Include(nv => nv.ChucVu)
            .Where(nv => nv.HoTen.Contains(keyword)
                || (nv.MaNV != null && nv.MaNV.Contains(keyword))
                || (nv.Email != null && nv.Email.Contains(keyword))
                || (nv.SoDienThoai != null && nv.SoDienThoai.Contains(keyword)))
            .ToListAsync();
    }
}
