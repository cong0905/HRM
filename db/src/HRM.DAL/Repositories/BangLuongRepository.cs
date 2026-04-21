using HRM.Domain.Entities;
using HRM.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace HRM.DAL.Repositories;

public interface IBangLuongRepository : IRepository<BangLuong>
{
    Task<List<BangLuong>> GetByThangNamWithNhanVienAsync(int thang, int nam);
    Task<List<BangLuong>> GetByThangNamForNhanVienAsync(int thang, int nam, int maNhanVien);
    Task XoaTheoThangNamAsync(int thang, int nam);
    Task ThemNhieuAsync(IEnumerable<BangLuong> items);
}

public class BangLuongRepository : Repository<BangLuong>, IBangLuongRepository
{
    public BangLuongRepository(HrmDbContext context) : base(context) { }

    public async Task<List<BangLuong>> GetByThangNamWithNhanVienAsync(int thang, int nam)
    {
        return await _context.BangLuong
            .AsNoTracking()
            .Include(b => b.NhanVien)
            .Where(b => b.Thang == thang && b.Nam == nam)
            .OrderBy(b => b.NhanVien.HoTen)
            .ToListAsync();
    }

    public async Task<List<BangLuong>> GetByThangNamForNhanVienAsync(int thang, int nam, int maNhanVien)
    {
        return await _context.BangLuong
            .AsNoTracking()
            .Include(b => b.NhanVien)
            .Where(b => b.Thang == thang && b.Nam == nam && b.MaNhanVien == maNhanVien)
            .OrderBy(b => b.NhanVien.HoTen)
            .ToListAsync();
    }

    public async Task XoaTheoThangNamAsync(int thang, int nam)
    {
        var rows = await _context.BangLuong.Where(b => b.Thang == thang && b.Nam == nam).ToListAsync();
        _context.BangLuong.RemoveRange(rows);
        await _context.SaveChangesAsync();
    }

    public async Task ThemNhieuAsync(IEnumerable<BangLuong> items)
    {
        await _context.BangLuong.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }
}
