using HRM.DAL.Context;
using HRM.Domain.Entities;


namespace HRM.DAL.Repositories
{
    public class TinTuyenDungRepository : Repository<TinTuyenDung>, ITinTuyenDungRepository
    {
        public TinTuyenDungRepository(HrmDbContext context) : base(context)
        {
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

