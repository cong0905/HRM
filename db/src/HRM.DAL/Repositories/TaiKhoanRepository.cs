using System.Threading.Tasks;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface ITaiKhoanRepository : IRepository<TaiKhoan>
{
    Task<TaiKhoan?> GetByUsernameAsync(string username);
}

public class TaiKhoanRepository : Repository<TaiKhoan>, ITaiKhoanRepository
{
    public TaiKhoanRepository(HrmDbContext context) : base(context) { }

    public async Task<TaiKhoan?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Include(tk => tk.NhanVien)
                .ThenInclude(nv => nv.PhongBan)
            .FirstOrDefaultAsync(tk => tk.TenDangNhap == username && tk.TrangThai == "Hoạt động");
    }
}
