using HRM.DAL.Context;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.DAL.Repositories
{
    public class PhongVanRepository : Repository<PhongVan>, IPhongVanRepository
    {
        public PhongVanRepository(HrmDbContext context) : base(context)
        {
        }

        public async Task<List<PhongVan>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(pv => pv.UngVien)
                .Include(pv => pv.NguoiPhongVanNav)
                .OrderByDescending(pv => pv.NgayPhongVan)
                .ToListAsync();
        }

        public async Task<List<PhongVan>> SearchWithDetailsAsync(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;
            if (keyword.Length == 0)
                return await GetAllWithDetailsAsync();

            return await _dbSet
                .Include(pv => pv.UngVien)
                .Include(pv => pv.NguoiPhongVanNav)
                .Where(pv =>
                   (pv.DiaDiem != null && pv.DiaDiem.Contains(keyword))
                    || pv.TrangThai.Contains(keyword)
                    || (pv.KetQua != null && pv.KetQua.Contains(keyword))
                    || pv.MaPhongVan.ToString().Contains(keyword)
                    || pv.MaUngVien.ToString().Contains(keyword)
                    || (pv.UngVien != null && pv.UngVien.HoTen.Contains(keyword))
                    || (pv.NguoiPhongVanNav != null && pv.NguoiPhongVanNav.HoTen.Contains(keyword))
                )
                .OrderByDescending(pv => pv.NgayPhongVan)
                .ToListAsync();
        }
    }
}
