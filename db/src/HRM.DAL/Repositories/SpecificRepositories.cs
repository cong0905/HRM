using Microsoft.EntityFrameworkCore;
using HRM.Domain.Entities;
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

public interface IPhongBanRepository : IRepository<PhongBan>
{
    Task<List<PhongBan>> GetAllWithTruongPhongAsync();
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
}

public interface IChamCongRepository : IRepository<ChamCong>
{
    Task<List<ChamCong>> GetByNhanVienAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCong>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay);
    Task<ChamCong?> GetTodayAsync(int maNhanVien);
    Task<List<DateTime>> GetDistinctNgayChamCongInMonthAsync(int maNhanVien, int year, int month);
    Task<List<DateTime>> GetDistinctNgayChamCongInMonthAllAsync(int year, int month);
}

public class ChamCongRepository : Repository<ChamCong>, IChamCongRepository
{
    public ChamCongRepository(HrmDbContext context) : base(context) { }

    public async Task<List<ChamCong>> GetByNhanVienAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet
            .Include(cc => cc.NhanVien)
            .Where(cc => cc.MaNhanVien == maNhanVien
                && cc.NgayChamCong >= from
                && cc.NgayChamCong < toExclusive)
            .OrderByDescending(cc => cc.NgayChamCong)
            .ToListAsync();
    }

    public async Task<List<ChamCong>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet
            .Include(cc => cc.NhanVien)
            .Where(cc => cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .OrderByDescending(cc => cc.NgayChamCong)
            .ThenBy(cc => cc.MaNhanVien)
            .ToListAsync();
    }

    public async Task<ChamCong?> GetTodayAsync(int maNhanVien)
    {
        var today = DateTime.Today;
        return await _dbSet
            .FirstOrDefaultAsync(cc => cc.MaNhanVien == maNhanVien && cc.NgayChamCong == today);
    }

    public async Task<List<DateTime>> GetDistinctNgayChamCongInMonthAsync(int maNhanVien, int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var toExclusive = from.AddMonths(1);
        return await _dbSet
            .AsNoTracking()
            .Where(cc => cc.MaNhanVien == maNhanVien
                && cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .Select(cc => cc.NgayChamCong.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }

    public async Task<List<DateTime>> GetDistinctNgayChamCongInMonthAllAsync(int year, int month)
    {
        var from = new DateTime(year, month, 1);
        var toExclusive = from.AddMonths(1);
        return await _dbSet
            .AsNoTracking()
            .Where(cc => cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .Select(cc => cc.NgayChamCong.Date)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();
    }
}

public interface IDonNghiPhepRepository : IRepository<DonNghiPhep>
{
    Task<List<DonNghiPhep>> GetByNhanVienAsync(int maNhanVien);
    Task<List<DonNghiPhep>> GetPendingAsync();
}

public class DonNghiPhepRepository : Repository<DonNghiPhep>, IDonNghiPhepRepository
{
    public DonNghiPhepRepository(HrmDbContext context) : base(context) { }

    public async Task<List<DonNghiPhep>> GetByNhanVienAsync(int maNhanVien)
    {
        return await _dbSet
            .Include(d => d.LoaiNghiPhep)
            .Include(d => d.NguoiPheDuyetNav)
            .Where(d => d.MaNhanVien == maNhanVien)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync();
    }

    public async Task<List<DonNghiPhep>> GetPendingAsync()
    {
        return await _dbSet
            .Include(d => d.NhanVien)
            .Include(d => d.LoaiNghiPhep)
            .Where(d => d.TrangThai == "Chờ duyệt")
            .OrderBy(d => d.NgayTao)
            .ToListAsync();
    }
}

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
