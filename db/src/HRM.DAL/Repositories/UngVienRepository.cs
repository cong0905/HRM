using HRM.DAL.Context;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.DAL.Repositories
{
    public class UngVienRepository : Repository<UngVien>, IUngVienRepository
    {
        public UngVienRepository(HrmDbContext context) : base(context)
        {
        }

        public override async Task<List<UngVien>> GetAllAsync()
        {
            return await _dbSet.Include(x => x.TinTuyenDung).ToListAsync();
        }

        public override async Task<UngVien?> GetByIdAsync(int id)
        {
            return await _dbSet.Include(x => x.TinTuyenDung).FirstOrDefaultAsync(x => x.MaUngVien == id);
        }

        public async Task<List<UngVien>> SearchAsync(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;
            if (keyword.Length == 0)
                return await GetAllAsync();

            return await _dbSet.Include(x => x.TinTuyenDung)
                .Where(x =>
                    x.HoTen.Contains(keyword)
                    || (x.Email != null && x.Email.Contains(keyword))
                    || (x.SoDienThoai != null && x.SoDienThoai.Contains(keyword))
                    || (x.TrangThai != null && x.TrangThai.Contains(keyword))
                    || x.MaUngVien.ToString().Contains(keyword))
                .OrderByDescending(x => x.NgayNop)
                .ToListAsync();
        }
    }
}
