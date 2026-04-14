using System.Linq;
using HRM.Common.Constants;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
    Task<ChamCong?> GetByIdWithNhanVienAsync(int maChamCong);
    Task<bool> ExistsOtherOnSameDayAsync(int maNhanVien, DateTime ngay, int excludeMaChamCong);
    /// <summary>Có ít nhất một bản ghi chấm công của nhân viên trong khoảng [tuNgay, denNgay] (theo ngày).</summary>
    Task<bool> ExistsAnyAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCong>> GetByNhanVienAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay);
    Task<List<ChamCong>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay);
    Task<ChamCong?> GetTodayAsync(int maNhanVien);
    Task<List<DateTime>> GetDistinctNgayChamCongInMonthAsync(int maNhanVien, int year, int month);
    Task<List<DateTime>> GetDistinctNgayChamCongInMonthAllAsync(int year, int month);
}

public class ChamCongRepository : Repository<ChamCong>, IChamCongRepository
{
    public ChamCongRepository(HrmDbContext context) : base(context) { }

    public async Task<ChamCong?> GetByIdWithNhanVienAsync(int maChamCong)
    {
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
            .FirstOrDefaultAsync(cc => cc.MaChamCong == maChamCong);
    }

    public async Task<bool> ExistsOtherOnSameDayAsync(int maNhanVien, DateTime ngay, int excludeMaChamCong)
    {
        var d = ngay.Date;
        var next = d.AddDays(1);
        return await _dbSet.AnyAsync(cc =>
            cc.MaNhanVien == maNhanVien
            && cc.NgayChamCong >= d && cc.NgayChamCong < next
            && cc.MaChamCong != excludeMaChamCong);
    }

    public async Task<bool> ExistsAnyAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet.AnyAsync(cc =>
            cc.MaNhanVien == maNhanVien
            && cc.NgayChamCong >= from
            && cc.NgayChamCong < toExclusive);
    }

    public async Task<List<ChamCong>> GetByNhanVienAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var from = tuNgay.Date;
        var toExclusive = denNgay.Date.AddDays(1);
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
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
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
            .Where(cc => cc.NgayChamCong >= from && cc.NgayChamCong < toExclusive)
            .OrderByDescending(cc => cc.NgayChamCong)
            .ThenBy(cc => cc.MaNhanVien)
            .ToListAsync();
    }

    public async Task<ChamCong?> GetTodayAsync(int maNhanVien)
    {
        var today = DateTime.Today;
        return await _dbSet
            .Include(cc => cc.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(cc => cc.NhanVien).ThenInclude(n => n.ChucVu)
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
    /// <summary>Nhân viên có đơn nghỉ đã duyệt trùng ngày (cả ngày trong khoảng nghỉ).</summary>
    Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay);
    /// <summary>Tra cứu toàn bộ đơn: sắp xếp mới nhất trước. Lọc theo từ khóa nhân viên và/hoặc khoảng ngày (đơn giao với [tu, den]).</summary>
    Task<List<DonNghiPhep>> SearchAllAsync(string? keywordTenHoacMa, DateTime? tuNgay, DateTime? denNgay);
}

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

public class DonNghiPhepRepository : Repository<DonNghiPhep>, IDonNghiPhepRepository
{
    public DonNghiPhepRepository(HrmDbContext context) : base(context) { }

    public async Task<List<DonNghiPhep>> GetByNhanVienAsync(int maNhanVien)
    {
        return await _dbSet
            .Include(d => d.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(d => d.NhanVien).ThenInclude(n => n.ChucVu)
            .Include(d => d.LoaiNghiPhep)
            .Include(d => d.NguoiPheDuyetNav)
            .Where(d => d.MaNhanVien == maNhanVien)
            .OrderByDescending(d => d.NgayTao)
            .ToListAsync();
    }

    public async Task<List<DonNghiPhep>> GetPendingAsync()
    {
        return await _dbSet
            .Include(d => d.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(d => d.NhanVien).ThenInclude(n => n.ChucVu)
            .Include(d => d.LoaiNghiPhep)
            .Where(d => d.TrangThai == DonNghiPhepTrangThai.ChoDuyet)
            .OrderBy(d => d.NgayTao)
            .ToListAsync();
    }

    public async Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay)
    {
        var d = ngay.Date;
        return await _dbSet.AnyAsync(x =>
            x.MaNhanVien == maNhanVien
            && x.TrangThai == DonNghiPhepTrangThai.DaDuyet
            && x.NgayBatDau.Date <= d
            && x.NgayKetThuc.Date >= d);
    }

    public async Task<List<DonNghiPhep>> SearchAllAsync(string? keywordTenHoacMa, DateTime? tuNgay, DateTime? denNgay)
    {
        IQueryable<DonNghiPhep> q = _dbSet
            .AsNoTracking()
            .Include(d => d.NhanVien).ThenInclude(n => n.PhongBan)
            .Include(d => d.NhanVien).ThenInclude(n => n.ChucVu)
            .Include(d => d.LoaiNghiPhep)
            .Include(d => d.NguoiPheDuyetNav);

        if (!string.IsNullOrWhiteSpace(keywordTenHoacMa))
        {
            var k = keywordTenHoacMa.Trim();
            if (int.TryParse(k, out var maHeThong))
            {
                q = q.Where(d =>
                    d.MaNhanVien == maHeThong
                    || d.NhanVien.HoTen.Contains(k)
                    || (d.NhanVien.MaNV != null && d.NhanVien.MaNV.Contains(k)));
            }
            else
            {
                q = q.Where(d =>
                    d.NhanVien.HoTen.Contains(k)
                    || (d.NhanVien.MaNV != null && d.NhanVien.MaNV.Contains(k)));
            }
        }

        if (tuNgay.HasValue && denNgay.HasValue)
        {
            var t = tuNgay.Value.Date;
            var e = denNgay.Value.Date;
            if (t > e)
                (t, e) = (e, t);
            q = q.Where(d => d.NgayBatDau <= e && d.NgayKetThuc >= t);
        }
        else if (tuNgay.HasValue)
        {
            var t = tuNgay.Value.Date;
            q = q.Where(d => d.NgayKetThuc >= t);
        }
        else if (denNgay.HasValue)
        {
            var e = denNgay.Value.Date;
            q = q.Where(d => d.NgayBatDau <= e);
        }

        return await q
            .OrderByDescending(d => d.NgayTao)
            .ThenByDescending(d => d.MaDonPhep)
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
