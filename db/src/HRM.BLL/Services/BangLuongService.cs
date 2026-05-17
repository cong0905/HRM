using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.BLL.Services;

/// <summary>
/// Tính lương từ mức lương, chấm công, phụ cấp. BHXH/BHYT/BHTN/thuế TNCN nhập thủ công trên bảng lương.
/// </summary>
public class BangLuongService : IBangLuongService
{
    private const decimal NgayCongChuan = 26m;
    private const decimal GioLamMotNgay = 8m;
    private const decimal HeSoGioLamThem = 1.5m;

    private readonly IBangLuongRepository _bangLuongRepo;
    private readonly INhanVienRepository _nhanVienRepo;
    private readonly IChamCongRepository _chamCongRepo;
    private readonly HrmDbContext _db;

    public BangLuongService(
        IBangLuongRepository bangLuongRepo,
        INhanVienRepository nhanVienRepo,
        IChamCongRepository chamCongRepo,
        HrmDbContext db)
    {
        _bangLuongRepo = bangLuongRepo;
        _nhanVienRepo = nhanVienRepo;
        _chamCongRepo = chamCongRepo;
        _db = db;
    }

    public async Task<List<BangLuongDTO>> GetBangLuongAsync(int thang, int nam, bool isAdmin, int maNhanVienDangNhap)
    {
        await DongBoLuongCoBanThangAsync(thang, nam, isAdmin, maNhanVienDangNhap);

        List<BangLuong> rows;
        if (isAdmin)
            rows = await _bangLuongRepo.GetByThangNamWithNhanVienAsync(thang, nam);
        else
            rows = await _bangLuongRepo.GetByThangNamForNhanVienAsync(thang, nam, maNhanVienDangNhap);

        var maQuanTri = await LayMaNhanVienQuanTriAsync();
        return rows
            .Where(b => !maQuanTri.Contains(b.MaNhanVien))
            .Select(MapToDto)
            .ToList();
    }

    private async Task<HashSet<int>> LayMaNhanVienQuanTriAsync()
    {
        var ids = await _db.TaiKhoan
            .AsNoTracking()
            .Where(t => t.VaiTro == "Admin" || t.VaiTro == "Quản trị viên")
            .Select(t => t.MaNhanVien)
            .ToListAsync();
        return ids.ToHashSet();
    }

    public async Task DongBoBangLuongTheoNhanVienAsync(int maNhanVien)
    {
        var nv = await _nhanVienRepo.GetByIdAsync(maNhanVien);
        if (nv == null) return;

        var rows = await _db.BangLuong
            .Where(b => b.MaNhanVien == maNhanVien)
            .ToListAsync();

        var changed = false;
        foreach (var b in rows)
        {
            if (b.LuongCoBan == nv.MucLuong) continue;
            ApplyLuongCoBanMoi(b, nv.MucLuong);
            b.NgayTinhLuong = DateTime.Now;
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync();
    }

    /// <summary>Cập nhật LuongCoBan theo MucLuong mới nhất trước khi hiển thị bảng lương.</summary>
    private async Task DongBoLuongCoBanThangAsync(int thang, int nam, bool isAdmin, int maNhanVienDangNhap)
    {
        var query = _db.BangLuong
            .Include(b => b.NhanVien)
            .Where(b => b.Thang == thang && b.Nam == nam);

        if (!isAdmin)
            query = query.Where(b => b.MaNhanVien == maNhanVienDangNhap);

        var rows = await query.ToListAsync();
        var changed = false;

        foreach (var b in rows)
        {
            var mucLuong = b.NhanVien?.MucLuong;
            if (mucLuong == null || b.LuongCoBan == mucLuong.Value) continue;

            ApplyLuongCoBanMoi(b, mucLuong.Value);
            b.NgayTinhLuong = DateTime.Now;
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync();
    }

    private static void ApplyLuongCoBanMoi(BangLuong b, decimal luongCoBan)
    {
        b.LuongCoBan = luongCoBan;
        var luongGio = luongCoBan / NgayCongChuan / GioLamMotNgay;
        b.TienLamThem = Math.Round(b.SoGioLamThem * luongGio * HeSoGioLamThem, 0, MidpointRounding.AwayFromZero);
        TinhLaiThuNhapVaThucNhan(b);
    }

    public async Task<int> TinhVaLuuBangLuongThangAsync(int thang, int nam)
    {
        if (thang is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(thang), "Tháng phải từ 1 đến 12.");

        var tuNgay = new DateTime(nam, thang, 1);
        var denNgay = tuNgay.AddMonths(1).AddDays(-1);

        var maQuanTri = await LayMaNhanVienQuanTriAsync();
        var nhanViens = await _nhanVienRepo.GetAllWithDetailsAsync();
        var active = nhanViens
            .Where(n => n.TrangThai.Equals("Đang làm việc", StringComparison.OrdinalIgnoreCase))
            .Where(n => !maQuanTri.Contains(n.MaNhanVien))
            .ToList();

        // Giữ thưởng/phạt (và trạng thái) đã nhập trước khi ghi đè bảng lương tháng
        var banGhiCu = await _db.BangLuong
            .AsNoTracking()
            .Where(b => b.Thang == thang && b.Nam == nam)
            .ToDictionaryAsync(b => b.MaNhanVien);

        await _bangLuongRepo.XoaTheoThangNamAsync(thang, nam);

        var list = new List<BangLuong>();
        foreach (var nv in active)
        {
            var luongCoBan = nv.MucLuong;
            var ngayLam = await DemNgayChamCongAsync(nv.MaNhanVien, tuNgay, denNgay);
            var gioLamThem = await TongGioLamThemAsync(nv.MaNhanVien, tuNgay, denNgay);
            var tongPhuCap = await TongPhuCapTrongThangAsync(nv.MaNhanVien, tuNgay, denNgay);

            var luongGio = luongCoBan / NgayCongChuan / GioLamMotNgay;
            var tienLamThem = Math.Round(gioLamThem * luongGio * HeSoGioLamThem, 0, MidpointRounding.AwayFromZero);

            banGhiCu.TryGetValue(nv.MaNhanVien, out var cu);
            var bl = new BangLuong
            {
                MaNhanVien = nv.MaNhanVien,
                Thang = thang,
                Nam = nam,
                LuongCoBan = luongCoBan,
                TongPhuCap = tongPhuCap,
                SoNgayLamViec = ngayLam,
                SoGioLamThem = gioLamThem,
                TienLamThem = tienLamThem,
                TongThuong = cu?.TongThuong ?? 0,
                TongPhat = cu?.TongPhat ?? 0,
                BHXH = cu?.BHXH ?? 0,
                BHYT = cu?.BHYT ?? 0,
                BHTN = cu?.BHTN ?? 0,
                ThueTNCN = cu?.ThueTNCN ?? 0,
                NgayTinhLuong = DateTime.Now,
                TrangThai = string.IsNullOrWhiteSpace(cu?.TrangThai) ? "Chờ duyệt" : cu!.TrangThai
            };
            TinhLaiThuNhapVaThucNhan(bl);
            list.Add(bl);
        }

        if (list.Count > 0)
            await _bangLuongRepo.ThemNhieuAsync(list);

        return list.Count;
    }

    public async Task CapNhatThuongPhatVaTinhLaiAsync(int maBangLuong, decimal tongThuong, decimal tongPhat)
    {
        if (tongThuong < 0 || tongPhat < 0)
            throw new ArgumentException("Thưởng và phạt không được âm.");

        var b = await _bangLuongRepo.GetByIdAsync(maBangLuong)
            ?? throw new InvalidOperationException("Không tìm thấy bản ghi bảng lương.");

        b.TongThuong = tongThuong;
        b.TongPhat = tongPhat;
        TinhLaiThuNhapVaThucNhan(b);
        b.NgayTinhLuong = DateTime.Now;
        await _bangLuongRepo.UpdateAsync(b);
    }

    public async Task CapNhatKhoanKhauTruVaTinhLaiAsync(int maBangLuong, decimal bhxh, decimal bhyt, decimal bhtn, decimal thueTncn)
    {
        if (bhxh < 0 || bhyt < 0 || bhtn < 0 || thueTncn < 0)
            throw new ArgumentException("BHXH, BHYT, BHTN và thuế TNCN không được âm.");

        var b = await _bangLuongRepo.GetByIdAsync(maBangLuong)
            ?? throw new InvalidOperationException("Không tìm thấy bản ghi bảng lương.");

        b.BHXH = bhxh;
        b.BHYT = bhyt;
        b.BHTN = bhtn;
        b.ThueTNCN = thueTncn;
        TinhLaiThuNhapVaThucNhan(b);
        b.NgayTinhLuong = DateTime.Now;
        await _bangLuongRepo.UpdateAsync(b);
    }

    private static decimal TinhLuongTheoNgayCong(BangLuong b)
    {
        if (b.SoNgayLamViec <= 0) return 0;
        if (b.SoNgayLamViec >= NgayCongChuan) return b.LuongCoBan;
        var luongMotNgay = Math.Round(b.LuongCoBan / NgayCongChuan, 0, MidpointRounding.AwayFromZero);
        return luongMotNgay * b.SoNgayLamViec;
    }

    private static decimal TinhTongThuNhap(BangLuong b) =>
        TinhLuongTheoNgayCong(b) + b.TongPhuCap + b.TienLamThem + b.TongThuong - b.TongPhat;

    private static void TinhLaiThuNhapVaThucNhan(BangLuong b)
    {
        var tongThuNhap = Math.Round(TinhTongThuNhap(b), 0, MidpointRounding.AwayFromZero);
        var tongKhauTru = b.BHXH + b.BHYT + b.BHTN + b.ThueTNCN;
        b.TongThuNhap = tongThuNhap;
        b.TongKhauTru = tongKhauTru;
        b.LuongThucNhan = Math.Round(tongThuNhap - tongKhauTru, 0, MidpointRounding.AwayFromZero);
    }

    private async Task<int> DemNgayChamCongAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var dates = await _chamCongRepo.GetDistinctNgayChamCongInMonthAsync(maNhanVien, tuNgay.Year, tuNgay.Month);
        return dates.Count;
    }

    private async Task<decimal> TongGioLamThemAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var rows = await _chamCongRepo.GetByNhanVienAsync(maNhanVien, tuNgay, denNgay);
        return rows.Sum(r => r.GioLamThem);
    }

    private async Task<decimal> TongPhuCapTrongThangAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var start = tuNgay.Date;
        var end = denNgay.Date.AddDays(1);

        var rows = await _db.PhuCapNhanVien
            .AsNoTracking()
            .Include(p => p.LoaiPhuCap)
            .Where(p => p.MaNhanVien == maNhanVien
                && p.NgayApDung < end
                && (p.NgayKetThuc == null || p.NgayKetThuc.Value.Date >= start))
            .ToListAsync();

        decimal sum = 0;
        foreach (var g in rows.GroupBy(p => p.MaPhuCap))
        {
            var pick = g.OrderByDescending(x => x.NgayApDung).First();
            sum += pick.LoaiPhuCap.SoTien;
        }

        return sum;
    }

    private static BangLuongDTO MapToDto(BangLuong b)
    {
        return new BangLuongDTO
        {
            MaBangLuong = b.MaBangLuong,
            MaNhanVien = b.MaNhanVien,
            TenNhanVien = b.NhanVien?.HoTen,
            Thang = b.Thang,
            Nam = b.Nam,
            LuongCoBan = b.LuongCoBan,
            TongPhuCap = b.TongPhuCap,
            SoNgayLamViec = b.SoNgayLamViec,
            SoGioLamThem = b.SoGioLamThem,
            TienLamThem = b.TienLamThem,
            TongThuong = b.TongThuong,
            TongPhat = b.TongPhat,
            BHXH = b.BHXH,
            BHYT = b.BHYT,
            BHTN = b.BHTN,
            ThueTNCN = b.ThueTNCN,
            TongThuNhap = b.TongThuNhap,
            TongKhauTru = b.TongKhauTru,
            LuongThucNhan = b.LuongThucNhan,
            NgayTinhLuong = b.NgayTinhLuong,
            TrangThai = b.TrangThai
        };
    }
}
