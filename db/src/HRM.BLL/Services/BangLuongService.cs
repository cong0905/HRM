using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.BLL.Services;

public class BangLuongService : IBangLuongService
{
    private const decimal NgayCongChuan = 26m;
    private const decimal GioLamMotNgay = 8m;
    private const decimal HeSoGioLamThem = 1.5m;
    private const decimal NguongDiemChuan = 100m;
    private const decimal TyLeThuongCoBan = 0.05m;
    private const decimal TyLePhatCoBan = 0.05m;

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
        await DongBoBangLuongTheoHieuSuatThangAsync(thang, nam, isAdmin, maNhanVienDangNhap);

        List<BangLuong> rows;
        if (isAdmin)
            rows = await _bangLuongRepo.GetByThangNamWithNhanVienAsync(thang, nam);
        else
            rows = await _bangLuongRepo.GetByThangNamForNhanVienAsync(thang, nam, maNhanVienDangNhap);

        var maQuanTri = await LayMaNhanVienQuanTriAsync();
        var hieuSuatTheoNv = await LayHieuSuatTheoThangAsync(thang, nam);

        return rows
            .Where(b => !maQuanTri.Contains(b.MaNhanVien))
            .Select(b =>
            {
                hieuSuatTheoNv.TryGetValue(b.MaNhanVien, out var hs);
                return MapToDto(b, hs);
            })
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
            .AsTracking()
            .Where(b => b.MaNhanVien == maNhanVien)
            .ToListAsync();

        var changed = false;
        foreach (var b in rows)
        {
            if (b.LuongCoBan == nv.MucLuong) continue;
            var hs = await LayHieuSuatMotNhanVienThangAsync(b.MaNhanVien, b.Thang, b.Nam);
            ApplyLuongCoBanMoi(b, nv.MucLuong, hs);
            b.NgayTinhLuong = DateTime.Now;
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync();
    }

    private async Task DongBoLuongCoBanThangAsync(int thang, int nam, bool isAdmin, int maNhanVienDangNhap)
    {
        var query = _db.BangLuong
            .AsTracking()
            .Include(b => b.NhanVien)
            .Where(b => b.Thang == thang && b.Nam == nam);

        if (!isAdmin)
            query = query.Where(b => b.MaNhanVien == maNhanVienDangNhap);

        var rows = await query.ToListAsync();
        if (rows.Count == 0) return;

        var hieuSuatTheoNv = await LayHieuSuatTheoThangAsync(thang, nam);
        var changed = false;

        foreach (var b in rows)
        {
            var mucLuong = b.NhanVien?.MucLuong;
            if (mucLuong == null || b.LuongCoBan == mucLuong.Value) continue;

            hieuSuatTheoNv.TryGetValue(b.MaNhanVien, out var hs);
            ApplyLuongCoBanMoi(b, mucLuong.Value, hs);
            b.NgayTinhLuong = DateTime.Now;
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync();
    }

    private async Task DongBoBangLuongTheoHieuSuatThangAsync(int thang, int nam, bool isAdmin, int maNhanVienDangNhap)
    {
        var query = _db.BangLuong.Where(b => b.Thang == thang && b.Nam == nam);
        if (!isAdmin)
            query = query.Where(b => b.MaNhanVien == maNhanVienDangNhap);

        var rows = await query.ToListAsync();
        if (rows.Count == 0) return;

        var hieuSuatTheoNv = await LayHieuSuatTheoThangAsync(thang, nam);
        var changed = false;

        foreach (var b in rows)
        {
            hieuSuatTheoNv.TryGetValue(b.MaNhanVien, out var hs);
            var heSo = TinhHeSoLuong(hs);
            var (thuong, phat) = TinhThuongPhatTheoHieuSuat(b.LuongCoBan, hs);

            if (b.TongThuong == thuong && b.TongPhat == phat)
            {
                var thuNhapMoi = Math.Round(TinhTongThuNhap(b, heSo), 0, MidpointRounding.AwayFromZero);
                if (b.TongThuNhap == thuNhapMoi) continue;
            }

            b.TongThuong = thuong;
            b.TongPhat = phat;
            TinhLaiThuNhapVaThucNhan(b, heSo);
            b.NgayTinhLuong = DateTime.Now;
            changed = true;
        }

        if (changed)
            await _db.SaveChangesAsync();
    }

    private static void ApplyLuongCoBanMoi(BangLuong b, decimal luongCoBan, HieuSuatNhanVien? hieuSuat)
    {
        b.LuongCoBan = luongCoBan;
        var luongGio = luongCoBan / NgayCongChuan / GioLamMotNgay;
        b.TienLamThem = Math.Round(b.SoGioLamThem * luongGio * HeSoGioLamThem, 0, MidpointRounding.AwayFromZero);
        ApDungThuongPhatTuHieuSuat(b, hieuSuat);
        TinhLaiThuNhapVaThucNhan(b, TinhHeSoLuong(hieuSuat));
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

        var banGhiCu = await _db.BangLuong
            .AsNoTracking()
            .Where(b => b.Thang == thang && b.Nam == nam)
            .ToDictionaryAsync(b => b.MaNhanVien);

        var hieuSuatTheoNv = await LayHieuSuatTheoThangAsync(thang, nam);

        await _bangLuongRepo.XoaTheoThangNamAsync(thang, nam);

        var list = new List<BangLuong>();
        foreach (var nv in active)
        {
            hieuSuatTheoNv.TryGetValue(nv.MaNhanVien, out var hs);
            var heSo = TinhHeSoLuong(hs);

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
                BHXH = cu?.BHXH ?? 0,
                BHYT = cu?.BHYT ?? 0,
                BHTN = cu?.BHTN ?? 0,
                ThueTNCN = cu?.ThueTNCN ?? 0,
                NgayTinhLuong = DateTime.Now,
                TrangThai = string.IsNullOrWhiteSpace(cu?.TrangThai) ? "Chờ duyệt" : cu!.TrangThai
            };
            ApDungThuongPhatTuHieuSuat(bl, hs);
            TinhLaiThuNhapVaThucNhan(bl, heSo);
            list.Add(bl);
        }

        if (list.Count > 0)
            await _bangLuongRepo.ThemNhieuAsync(list);

        return list.Count;
    }

    public Task CapNhatThuongPhatVaTinhLaiAsync(int maBangLuong, decimal tongThuong, decimal tongPhat) =>
        throw new InvalidOperationException(
            "Thưởng và phạt được tính tự động theo hiệu suất. Vui lòng dùng 「Tính lương tháng」 hoặc cập nhật hiệu suất rồi tải lại.");

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
        var hs = await LayHieuSuatMotNhanVienThangAsync(b.MaNhanVien, b.Thang, b.Nam);
        TinhLaiThuNhapVaThucNhan(b, TinhHeSoLuong(hs));
        b.NgayTinhLuong = DateTime.Now;
        await _bangLuongRepo.UpdateAsync(b);
    }

    private static void ApDungThuongPhatTuHieuSuat(BangLuong b, HieuSuatNhanVien? hieuSuat)
    {
        var (thuong, phat) = TinhThuongPhatTheoHieuSuat(b.LuongCoBan, hieuSuat);
        b.TongThuong = thuong;
        b.TongPhat = phat;
    }

    /// <summary>Điểm HS ≥ 100 → thưởng; &lt; 100 → phạt. Chưa có hiệu suất → 0.</summary>
    private static (decimal Thuong, decimal Phat) TinhThuongPhatTheoHieuSuat(decimal luongCoBan, HieuSuatNhanVien? hieuSuat)
    {
        if (luongCoBan <= 0) return (0, 0);

        var diem = LayDiemHieuSuat(hieuSuat);
        if (!diem.HasValue) return (0, 0);

        if (diem.Value >= NguongDiemChuan)
        {
            var heSoVuot = 1m + Math.Min((diem.Value - NguongDiemChuan) / 100m, 0.5m);
            var thuong = Math.Round(luongCoBan * TyLeThuongCoBan * heSoVuot, 0, MidpointRounding.AwayFromZero);
            return (thuong, 0);
        }

        var heSoThieu = Math.Min((NguongDiemChuan - diem.Value) / 100m, 1m);
        var phat = Math.Round(luongCoBan * TyLePhatCoBan * heSoThieu, 0, MidpointRounding.AwayFromZero);
        return (0, phat);
    }

    private static decimal TinhHeSoLuong(HieuSuatNhanVien? hieuSuat) =>
        HieuSuatService.TinhBonusHieuSuat(LayDiemHieuSuat(hieuSuat));

    private static decimal? LayDiemHieuSuat(HieuSuatNhanVien? hieuSuat)
    {
        if (hieuSuat == null) return null;
        return HieuSuatService.TinhDiemHieuSuatCuoiCung(
            hieuSuat.DiemChuyenCan, hieuSuat.DiemKPI, hieuSuat.TyLeHoanThanhDeadline);
    }

    private static decimal TinhLuongCoBanSauHieuSuat(decimal luongCoBan, decimal heSoLuong) =>
        Math.Round(luongCoBan * (1m + heSoLuong), 0, MidpointRounding.AwayFromZero);

    private static decimal TinhLuongTheoNgayCong(BangLuong b, decimal heSoLuong)
    {
        if (b.SoNgayLamViec <= 0) return 0;

        var luongHs = TinhLuongCoBanSauHieuSuat(b.LuongCoBan, heSoLuong);
        if (b.SoNgayLamViec >= NgayCongChuan) return luongHs;

        var luongMotNgay = Math.Round(luongHs / NgayCongChuan, 0, MidpointRounding.AwayFromZero);
        return luongMotNgay * b.SoNgayLamViec;
    }

    private static decimal TinhTongThuNhap(BangLuong b, decimal heSoLuong) =>
        TinhLuongTheoNgayCong(b, heSoLuong) + b.TongPhuCap + b.TienLamThem + b.TongThuong - b.TongPhat;

    private static void TinhLaiThuNhapVaThucNhan(BangLuong b, decimal heSoLuong)
    {
        var tongThuNhap = Math.Round(TinhTongThuNhap(b, heSoLuong), 0, MidpointRounding.AwayFromZero);
        var tongKhauTru = b.BHXH + b.BHYT + b.BHTN + b.ThueTNCN;
        b.TongThuNhap = tongThuNhap;
        b.TongKhauTru = tongKhauTru;
        b.LuongThucNhan = Math.Round(tongThuNhap - tongKhauTru, 0, MidpointRounding.AwayFromZero);
    }

    private async Task<Dictionary<int, HieuSuatNhanVien>> LayHieuSuatTheoThangAsync(int thang, int nam)
    {
        var tuNgay = new DateTime(nam, thang, 1);
        var denNgay = tuNgay.AddMonths(1).AddDays(-1);

        var rows = await _db.HieuSuatNhanVien
            .AsNoTracking()
            .Include(h => h.KyDanhGia)
            .Where(h => h.KyDanhGia != null
                && h.KyDanhGia.NgayBatDau <= denNgay
                && h.KyDanhGia.NgayKetThuc >= tuNgay)
            .ToListAsync();

        return rows
            .GroupBy(h => h.MaNhanVien)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(h => h.NgayDanhGia).First());
    }

    private async Task<HieuSuatNhanVien?> LayHieuSuatMotNhanVienThangAsync(int maNhanVien, int thang, int nam)
    {
        var map = await LayHieuSuatTheoThangAsync(thang, nam);
        return map.GetValueOrDefault(maNhanVien);
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

    private static BangLuongDTO MapToDto(BangLuong b, HieuSuatNhanVien? hieuSuat)
    {
        var heSo = TinhHeSoLuong(hieuSuat);
        var diem = LayDiemHieuSuat(hieuSuat);

        return new BangLuongDTO
        {
            MaBangLuong = b.MaBangLuong,
            MaNhanVien = b.MaNhanVien,
            TenNhanVien = b.NhanVien?.HoTen,
            Thang = b.Thang,
            Nam = b.Nam,
            LuongCoBan = b.LuongCoBan,
            DiemHieuSuat = diem,
            HeSoLuongHieuSuat = heSo,
            LuongCoBanSauHieuSuat = TinhLuongCoBanSauHieuSuat(b.LuongCoBan, heSo),
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
