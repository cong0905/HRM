using System.Threading.Tasks;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface ITaiKhoanRepository : IRepository<TaiKhoan>
{
    Task<TaiKhoan?> GetByUsernameAsync(string username);
    Task<TaiKhoan?> GetByNhanVienEmailAsync(string email);
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

    public async Task<TaiKhoan?> GetByNhanVienEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var lower = email.Trim().ToLower();
        return await _dbSet
            .Include(tk => tk.NhanVien)
                .ThenInclude(nv => nv.PhongBan)
            .FirstOrDefaultAsync(tk => tk.NhanVien != null && tk.NhanVien.Email != null && tk.NhanVien.Email.ToLower() == lower && tk.TrangThai == "Hoạt động");
    }
}
