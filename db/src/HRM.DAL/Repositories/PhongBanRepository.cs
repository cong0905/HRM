using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface IPhongBanRepository : IRepository<PhongBan>
{
    Task<List<PhongBan>> GetAllWithTruongPhongAsync();
    Task<List<PhongBan>> SearchAsync(string keyword);
}

public class PhongBanRepository : Repository<PhongBan>, IPhongBanRepository
{
    public PhongBanRepository(HrmDbContext context) : base(context) { }

    public async Task<List<PhongBan>> GetAllWithTruongPhongAsync()
    {
        return await _dbSet
            .Include(pb => pb.TruongPhong)
            .Include(pb => pb.DanhSachNhanVien)
            .OrderBy(pb => pb.TenPhongBan)
            .ToListAsync();
    }

    public async Task<List<PhongBan>> SearchAsync(string keyword)
    {
        return await _dbSet
            .Include(pb => pb.TruongPhong)
            .Include(pb => pb.DanhSachNhanVien)
            .Where(pb => pb.TenPhongBan.Contains(keyword)
                || (pb.MoTaChucNang != null && pb.MoTaChucNang.Contains(keyword))
                || (pb.DiaDiemLamViec != null && pb.DiaDiemLamViec.Contains(keyword)))
            .OrderBy(pb => pb.TenPhongBan)
            .ToListAsync();
    }
}
