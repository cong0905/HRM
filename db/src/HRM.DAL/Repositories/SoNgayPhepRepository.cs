using System.Threading.Tasks;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.DAL.Repositories;

public interface ISoNgayPhepRepository
{
    Task<SoNgayPhep?> GetAsync(int maNhanVien, int nam);
    Task<SoNgayPhep> EnsureAsync(int maNhanVien, int nam);
    Task UpdateAsync(SoNgayPhep entity);
}

public class SoNgayPhepRepository : ISoNgayPhepRepository
{
    private readonly HrmDbContext _context;

    public SoNgayPhepRepository(HrmDbContext context)
    {
        _context = context;
    }

    public async Task<SoNgayPhep?> GetAsync(int maNhanVien, int nam)
    {
        return await _context.Set<SoNgayPhep>()
            .FirstOrDefaultAsync(s => s.MaNhanVien == maNhanVien && s.Nam == nam);
    }

    public async Task<SoNgayPhep> EnsureAsync(int maNhanVien, int nam)
    {
        var existing = await GetAsync(maNhanVien, nam);
        if (existing != null)
            return existing;

        var row = new SoNgayPhep
        {
            MaNhanVien = maNhanVien,
            Nam = nam,
            TongSoNgayPhep = 12,
            SoNgayDaSuDung = 0,
            PhepNamCuConLai = 0
        };
        _context.Set<SoNgayPhep>().Add(row);
        await _context.SaveChangesAsync();
        return row;
    }

    public async Task UpdateAsync(SoNgayPhep entity)
    {
        _context.Set<SoNgayPhep>().Update(entity);
        await _context.SaveChangesAsync();
    }
}
