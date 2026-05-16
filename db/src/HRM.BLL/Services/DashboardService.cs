using HRM.BLL.Interfaces;
using HRM.Common.Constants;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using HRM.DAL.Context;

namespace HRM.BLL.Services;

public class DashboardService : IDashboardService
{
    private readonly HrmDbContext _context;

    public DashboardService(HrmDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDTO> GetSummaryAsync()
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var firstDayThisMonth = new DateTime(today.Year, today.Month, 1);
        var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

        // Tổng nhân viên đang làm việc
        var tongNV = await _context.Set<NhanVien>()
            .CountAsync(nv => nv.TrangThai == "Đang làm việc");

        // NV tháng trước (tính tăng trưởng)
        var nvThangTruoc = await _context.Set<NhanVien>()
            .CountAsync(nv => nv.NgayVaoLam < firstDayThisMonth && nv.TrangThai == "Đang làm việc");

        // Đi làm hôm nay (có bản ghi chấm công hôm nay)
        var diLamHomNay = await _context.Set<ChamCong>()
            .Where(cc => cc.NgayChamCong == today && cc.GioVao != null)
            .Select(cc => cc.MaNhanVien)
            .Distinct()
            .CountAsync();

        // Đi muộn hôm nay (GioVao > 08:30)
        var gioTre = new TimeSpan(8, 30, 0);
        var diMuonHomNay = await _context.Set<ChamCong>()
            .CountAsync(cc => cc.NgayChamCong == today
                && cc.GioVao != null
                && cc.GioVao > gioTre);

        // Đi muộn hôm qua
        var diMuonHomQua = await _context.Set<ChamCong>()
            .CountAsync(cc => cc.NgayChamCong == yesterday
                && cc.GioVao != null
                && cc.GioVao > gioTre);

        // Nghỉ phép hôm nay (đơn đã duyệt, ngày hiện tại nằm trong khoảng nghỉ)
        var nghiPhepHomNay = await _context.Set<DonNghiPhep>()
            .CountAsync(d => d.TrangThai == DonNghiPhepTrangThai.DaDuyet
                && d.NgayBatDau <= today
                && d.NgayKetThuc >= today);

        // Nghỉ phép hôm qua
        var nghiPhepHomQua = await _context.Set<DonNghiPhep>()
            .CountAsync(d => d.TrangThai == DonNghiPhepTrangThai.DaDuyet
                && d.NgayBatDau <= yesterday
                && d.NgayKetThuc >= yesterday);

        // Tính % tăng trưởng
        decimal phanTramTang = nvThangTruoc > 0
            ? Math.Round((tongNV - nvThangTruoc) * 100m / nvThangTruoc, 1)
            : 0;

        decimal phanTramDiLam = tongNV > 0
            ? Math.Round(diLamHomNay * 100m / tongNV, 1)
            : 0;

        return new DashboardSummaryDTO
        {
            TongNhanVien = tongNV,
            DiLamHomNay = diLamHomNay,
            NghiPhepHomNay = nghiPhepHomNay,
            DiMuonHomNay = diMuonHomNay,
            PhanTramTangNV = phanTramTang,
            PhanTramDiLam = phanTramDiLam,
            ChenhLechNghiPhep = nghiPhepHomNay - nghiPhepHomQua,
            ChenhLechDiMuon = diMuonHomNay - diMuonHomQua
        };
    }

    public async Task<List<PhongBanThongKeDTO>> GetNhanVienTheoPhongBanAsync()
    {
        var data = await _context.Set<NhanVien>()
            .Where(nv => nv.TrangThai == "Đang làm việc" && nv.PhongBan != null)
            .GroupBy(nv => nv.PhongBan!.TenPhongBan)
            .Select(g => new { TenPhongBan = g.Key, SoNhanVien = g.Count() })
            .OrderByDescending(x => x.SoNhanVien)
            .ToListAsync();

        var tong = data.Sum(x => x.SoNhanVien);

        return data.Select(x => new PhongBanThongKeDTO
        {
            TenPhongBan = x.TenPhongBan,
            SoNhanVien = x.SoNhanVien,
            PhanTram = tong > 0 ? Math.Round(x.SoNhanVien * 100m / tong, 1) : 0
        }).ToList();
    }

    public async Task<List<TangTruongNhanSuDTO>> GetTangTruongNhanSuAsync(int soThang = 6)
    {
        var result = new List<TangTruongNhanSuDTO>();
        var now = DateTime.Today;

        for (int i = soThang - 1; i >= 0; i--)
        {
            var month = now.AddMonths(-i);
            var endOfMonth = new DateTime(month.Year, month.Month,
                DateTime.DaysInMonth(month.Year, month.Month));

            // Nếu tháng hiện tại thì tính đến hôm nay
            if (endOfMonth > now) endOfMonth = now;

            // Đếm NV đang làm việc tính đến cuối tháng đó
            var count = await _context.Set<NhanVien>()
                .CountAsync(nv => nv.NgayVaoLam <= endOfMonth
                    && (nv.TrangThai == "Đang làm việc"
                        || (nv.NgayNghiViec != null && nv.NgayNghiViec > endOfMonth)));

            result.Add(new TangTruongNhanSuDTO
            {
                TenThang = $"Tháng {month.Month}",
                SoNhanVien = count
            });
        }

        return result;
    }

    public async Task<List<HoatDongGanDayDTO>> GetHoatDongGanDayAsync(int top = 5)
    {
        var result = new List<HoatDongGanDayDTO>();
        var now = DateTime.Now;
        var today = DateTime.Today;

        // Lấy chấm công gần đây nhất hôm nay
        var chamCongs = await _context.Set<ChamCong>()
            .Include(cc => cc.NhanVien)
            .Where(cc => cc.NgayChamCong == today && cc.GioVao != null)
            .OrderByDescending(cc => cc.GioVao)
            .Take(top)
            .ToListAsync();

        foreach (var cc in chamCongs)
        {
            var gioVao = today.Add(cc.GioVao!.Value);
            result.Add(new HoatDongGanDayDTO
            {
                TenNhanVien = cc.NhanVien.HoTen,
                MoTa = $"Check-in lúc {cc.GioVao:hh\\:mm}",
                ThoiGian = FormatTimeAgo(now - gioVao),
                LoaiHoatDong = "Check-in"
            });
        }

        // Đơn nghỉ phép gần đây
        var donPheps = await _context.Set<DonNghiPhep>()
            .Include(d => d.NhanVien)
            .OrderByDescending(d => d.NgayTao)
            .Take(top)
            .ToListAsync();

        foreach (var don in donPheps)
        {
            result.Add(new HoatDongGanDayDTO
            {
                TenNhanVien = don.NhanVien.HoTen,
                MoTa = "Đã gửi đơn nghỉ phép năm",
                ThoiGian = FormatTimeAgo(now - don.NgayTao),
                LoaiHoatDong = don.TrangThai == DonNghiPhepTrangThai.ChoDuyet ? "Chờ duyệt" : don.TrangThai
            });
        }

        return result.OrderBy(x => ParseTimeAgoToMinutes(x.ThoiGian)).Take(top).ToList();
    }

    public async Task<List<ThongBaoDashboardDTO>> GetThongBaoAsync()
    {
        var today = DateTime.Today;
        var result = new List<ThongBaoDashboardDTO>();

        // Đơn nghỉ phép chờ duyệt
        var choDuyet = await _context.Set<DonNghiPhep>()
            .CountAsync(d => d.TrangThai == DonNghiPhepTrangThai.ChoDuyet);
        if (choDuyet > 0)
            result.Add(new ThongBaoDashboardDTO { Icon = "📋", NoiDung = $"Có {choDuyet} đơn nghỉ phép chờ duyệt", SoLuong = choDuyet });

        // NV chưa chấm công hôm nay
        var tongNV = await _context.Set<NhanVien>().CountAsync(nv => nv.TrangThai == "Đang làm việc");
        var daChamCong = await _context.Set<ChamCong>()
            .Where(cc => cc.NgayChamCong == today)
            .Select(cc => cc.MaNhanVien)
            .Distinct()
            .CountAsync();
        var chuaChamCong = tongNV - daChamCong;
        if (chuaChamCong > 0)
            result.Add(new ThongBaoDashboardDTO { Icon = "⏰", NoiDung = $"Nhân viên chưa chấm công hôm nay", SoLuong = chuaChamCong });

        // NV mới trong tháng
        var firstDayMonth = new DateTime(today.Year, today.Month, 1);
        var nvMoi = await _context.Set<NhanVien>()
            .CountAsync(nv => nv.NgayVaoLam >= firstDayMonth && nv.TrangThai == "Đang làm việc");
        if (nvMoi > 0)
            result.Add(new ThongBaoDashboardDTO { Icon = "👤", NoiDung = $"Nhân viên mới tháng này", SoLuong = nvMoi });

        return result;
    }

    private static string FormatTimeAgo(TimeSpan elapsed)
    {
        if (elapsed.TotalMinutes < 1) return "Vừa xong";
        if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} phút trước";
        if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} giờ trước";
        return $"{(int)elapsed.TotalDays} ngày trước";
    }

    private static int ParseTimeAgoToMinutes(string timeAgo)
    {
        if (timeAgo.Contains("phút")) return int.TryParse(timeAgo.Split(' ')[0], out var m) ? m : 999;
        if (timeAgo.Contains("giờ")) return int.TryParse(timeAgo.Split(' ')[0], out var h) ? h * 60 : 9999;
        if (timeAgo.Contains("ngày")) return int.TryParse(timeAgo.Split(' ')[0], out var d) ? d * 1440 : 99999;
        return 0; // "Vừa xong"
    }
}
