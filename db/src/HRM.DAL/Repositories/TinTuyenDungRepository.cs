using HRM.DAL.Context;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace HRM.DAL.Repositories
{
    public class TinTuyenDungRepository : Repository<TinTuyenDung>, ITinTuyenDungRepository
    {
        public TinTuyenDungRepository(HrmDbContext context) : base(context)
        {
        }

        public async Task<List<TinTuyenDung>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(x => x.PhongBan)
                .OrderByDescending(x => x.NgayDang)
                .ToListAsync();
        }

        public async Task<List<TinTuyenDung>> SearchWithDetailsAsync(string keyword)
        {
            keyword = keyword?.Trim() ?? string.Empty;
            if (keyword.Length == 0)
                return await GetAllWithDetailsAsync();

            return await _dbSet
                .Include(x => x.PhongBan)
                .Where(x =>
                    x.MaTinTuyenDung.ToString().Contains(keyword)
                    || x.ViTriTuyenDung.Contains(keyword)
                    || (x.TrangThai != null && x.TrangThai.Contains(keyword))
                    || (x.DiadiemLamViec != null && x.DiadiemLamViec.Contains(keyword))
                    || (x.PhongBan != null && x.PhongBan.TenPhongBan.Contains(keyword)))
                .OrderByDescending(x => x.NgayDang)
                .ToListAsync();
        }

        public async Task<bool> DeleteTinTuyenDungAsync(int id)
        {
            try
            {
                var tinCanXoa = await _context.TinTuyenDung.FindAsync(id);
                if (tinCanXoa != null)
                {
                    _context.TinTuyenDung.Remove(tinCanXoa);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                // MOI RUỘT LỖI TỪ SQL SERVER RA
                string loiThucSu = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

                // Ném cái lỗi thật này lên cho Form (frmMain) bắt được và hiện lên màn hình
                throw new Exception("Chi tiết lỗi từ Database:\n" + loiThucSu);
            }
        }
    }
}

