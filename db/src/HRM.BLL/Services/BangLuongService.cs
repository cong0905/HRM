using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Context;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.BLL.Services;

/// <summary>
/// Tính lương từ mức lương, chấm công, phụ cấp. Thưởng/phạt nhập thủ công trên bảng lương (không lấy tự động từ module ThuongPhat).
/// Công thức mang tính tham khảo (BHXH/BHYT/BHTN/thuế đơn giản hóa).
/// </summary>
public class BangLuongService : IBangLuongService
{
    private const decimal NgayCongChuan = 26m;
    private const decimal GioLamMotNgay = 8m;
    private const decimal HeSoGioLamThem = 1.5m;
    /// <summary>Mức lương làm căn cứ đóng BHXH (tối đa theo quy định — giá trị mẫu).</summary>
    private const decimal TranBHXHToiDa = 36_000_000m;
    private const decimal GiamTruBanThanThang = 11_000_000m;

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
        List<BangLuong> rows;
        if (isAdmin)
            rows = await _bangLuongRepo.GetByThangNamWithNhanVienAsync(thang, nam);
        else
            rows = await _bangLuongRepo.GetByThangNamForNhanVienAsync(thang, nam, maNhanVienDangNhap);

        return rows.Select(MapToDto).ToList();
    }

    public async Task<int> TinhVaLuuBangLuongThangAsync(int thang, int nam)
    {
        if (thang is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(thang), "Tháng phải từ 1 đến 12.");

        var tuNgay = new DateTime(nam, thang, 1);
        var denNgay = tuNgay.AddMonths(1).AddDays(-1);

        var nhanViens = await _nhanVienRepo.GetAllWithDetailsAsync();
        var active = nhanViens
            .Where(n => n.TrangThai.Equals("Đang làm việc", StringComparison.OrdinalIgnoreCase))
            .ToList();

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
                TongThuong = 0,
                TongPhat = 0,
                NgayTinhLuong = DateTime.Now,
                TrangThai = "Chờ duyệt"
            };
            TinhLaiCacKhoanTuBangLuong(bl);
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
        TinhLaiCacKhoanTuBangLuong(b);
        b.NgayTinhLuong = DateTime.Now;
        await _bangLuongRepo.UpdateAsync(b);
    }

    /// <summary>Tính BHXH, thuế, tổng thu nhập, thực nhận từ các cột hiện có của bản ghi.</summary>
    private static void TinhLaiCacKhoanTuBangLuong(BangLuong b)
    {
        var luongTheoNgayCong = Math.Round(b.LuongCoBan / NgayCongChuan * b.SoNgayLamViec, 0, MidpointRounding.AwayFromZero);
        var tongThuNhapTruocThue = luongTheoNgayCong + b.TongPhuCap + b.TienLamThem + b.TongThuong - b.TongPhat;

        var mucDongBhxh = Math.Min(b.LuongCoBan, TranBHXHToiDa);
        var bhxh = Math.Round(mucDongBhxh * 0.08m, 0, MidpointRounding.AwayFromZero);
        var bhyt = Math.Round(mucDongBhxh * 0.015m, 0, MidpointRounding.AwayFromZero);
        var bhtn = Math.Round(mucDongBhxh * 0.01m, 0, MidpointRounding.AwayFromZero);

        var tongKhauTruBaoHiem = bhxh + bhyt + bhtn;
        var thuNhapChiuThue = tongThuNhapTruocThue - tongKhauTruBaoHiem - GiamTruBanThanThang;
        var thueTncn = thuNhapChiuThue > 0
            ? Math.Round(thuNhapChiuThue * 0.1m, 0, MidpointRounding.AwayFromZero)
            : 0m;

        var tongKhauTru = tongKhauTruBaoHiem + thueTncn;
        var luongThucNhan = tongThuNhapTruocThue - thueTncn;

        b.BHXH = bhxh;
        b.BHYT = bhyt;
        b.BHTN = bhtn;
        b.ThueTNCN = thueTncn;
        b.TongThuNhap = tongThuNhapTruocThue;
        b.TongKhauTru = tongKhauTru;
        b.LuongThucNhan = luongThucNhan;
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
