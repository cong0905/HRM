using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace HRM.BLL.Services;

public class ChamCongService : IChamCongService
{
    private const string WhitelistPolicyType = "ChamCongWhitelist";
    private readonly IChamCongRepository _repo;
    private readonly IDonNghiPhepRepository _donNghiPhepRepo;
    private readonly IRepository<ChinhSach> _chinhSachRepo;

    public ChamCongService(IChamCongRepository repo, IDonNghiPhepRepository donNghiPhepRepo, IRepository<ChinhSach> chinhSachRepo)
    {
        _repo = repo;
        _donNghiPhepRepo = donNghiPhepRepo;
        _chinhSachRepo = chinhSachRepo;
    }

    public async Task<ChamCongDTO?> CheckInAsync(int maNhanVien, string? hwid = null)
    {
        await EnsureCurrentNetworkAllowedAsync();

        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, DateTime.Today))
            throw new InvalidOperationException("Hôm nay bạn có đơn nghỉ phép đã duyệt, không thể chấm công vào ca.");

        var existing = await _repo.GetTodayAsync(maNhanVien);
        if (existing != null) return null; // Đã check-in rồi

        var normalizedHwid = NormalizeHwid(hwid);
        var chamCong = new ChamCong
        {
            MaNhanVien = maNhanVien,
            NgayChamCong = DateTime.Today,
            GioVao = DateTime.Now.TimeOfDay,
            HinhThuc = "Phần mềm",
            TrangThai = DateTime.Now.TimeOfDay > new TimeSpan(8, 30, 0) ? "Đi muộn" : "Bình thường",
            Hwid = normalizedHwid
        };

        var created = await _repo.AddAsync(chamCong);
        return new ChamCongDTO
        {
            MaChamCong = created.MaChamCong,
            MaNhanVien = created.MaNhanVien,
            NgayChamCong = created.NgayChamCong,
            GioVao = created.GioVao,
            TrangThai = created.TrangThai,
            Hwid = created.Hwid
        };
    }

    public async Task<ChamCongDTO?> CheckOutAsync(int maNhanVien, string? hwid = null)
    {
        await EnsureCurrentNetworkAllowedAsync();

        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, DateTime.Today))
            throw new InvalidOperationException("Hôm nay bạn có đơn nghỉ phép đã duyệt, không thể chấm tan ca.");

        var existing = await _repo.GetTodayAsync(maNhanVien);
        if (existing == null || existing.GioRa != null) return null;

        existing.Hwid = NormalizeHwid(hwid);
        existing.GioRa = DateTime.Now.TimeOfDay;
        if (existing.GioVao.HasValue)
        {
            var totalHours = (decimal)(existing.GioRa.Value - existing.GioVao.Value).TotalHours;
            existing.TongGioLam = Math.Round(totalHours, 2);
            existing.GioLamThem = totalHours > 8 ? Math.Round(totalHours - 8, 2) : 0;
        }

        if (existing.GioRa < new TimeSpan(17, 0, 0))
        {
            existing.TrangThai = existing.TrangThai == "Đi muộn" ? "Đi muộn và về sớm" : "Về sớm";
        }

        await _repo.UpdateAsync(existing);

        return new ChamCongDTO
        {
            MaChamCong = existing.MaChamCong,
            MaNhanVien = existing.MaNhanVien,
            NgayChamCong = existing.NgayChamCong,
            GioVao = existing.GioVao,
            GioRa = existing.GioRa,
            TongGioLam = existing.TongGioLam,
            TrangThai = existing.TrangThai,
            Hwid = existing.Hwid
        };
    }

    public async Task<bool> IsCurrentNetworkAllowedAsync()
    {
        var rules = await GetActiveWhitelistRulesAsync();
        if (rules.Count == 0)
            return false; // strict mode: chưa cấu hình whitelist thì không cho chấm công

        var localIps = GetLocalIPv4Addresses();
        if (localIps.Count == 0)
            return false;

        foreach (var ip in localIps)
        {
            if (rules.Any(rule => IsIpMatchRule(ip, rule)))
                return true;
        }
        return false;
    }

    private async Task EnsureCurrentNetworkAllowedAsync()
    {
        var rules = await GetActiveWhitelistRulesAsync();
        if (rules.Count == 0)
            throw new InvalidOperationException("Chưa cấu hình whitelist mạng chấm công. Vui lòng liên hệ Admin.");

        var localIps = GetLocalIPv4Addresses();
        if (localIps.Count == 0)
            throw new InvalidOperationException("Không tìm thấy địa chỉ IPv4 của máy để đối chiếu whitelist chấm công.");

        var matched = localIps.Any(ip => rules.Any(rule => IsIpMatchRule(ip, rule)));
        if (matched)
            return;

        var currentIps = string.Join(", ", localIps.Select(x => x.ToString()));
        throw new InvalidOperationException(
            $"Mạng hiện tại không nằm trong whitelist chấm công. IP hiện tại: {currentIps}");
    }

    public async Task<List<ChamCongWhitelistDTO>> GetWhitelistAsync()
    {
        var items = await _chinhSachRepo.FindAsync(x =>
            x.LoaiChinhSach == WhitelistPolicyType &&
            x.TrangThai == "Hoạt động");

        return items
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new ChamCongWhitelistDTO
            {
                MaWhitelist = x.MaChinhSach,
                Rule = x.NoiDung,
                GhiChu = x.PhamViApDung,
                NgayTao = x.NgayTao
            })
            .ToList();
    }

    public async Task AddWhitelistAsync(ChamCongWhitelistCreateDTO dto)
    {
        var rule = dto.Rule.Trim();
        if (!IsValidRule(rule))
            throw new InvalidOperationException("Rule không hợp lệ. Dùng IP (vd: 192.168.1.10) hoặc CIDR (vd: 192.168.1.0/24).");

        var existing = await GetActiveWhitelistRulesAsync();
        if (existing.Any(x => x.Equals(rule, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Rule này đã tồn tại trong whitelist.");

        await _chinhSachRepo.AddAsync(new ChinhSach
        {
            TenChinhSach = $"Whitelist mạng chấm công: {rule}",
            LoaiChinhSach = WhitelistPolicyType,
            NoiDung = rule,
            PhamViApDung = string.IsNullOrWhiteSpace(dto.GhiChu) ? null : dto.GhiChu.Trim(),
            NgayHieuLuc = DateTime.Now,
            PhienBan = 1,
            TrangThai = "Hoạt động",
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        });
    }

    public async Task RemoveWhitelistAsync(int maWhitelist)
    {
        var entity = await _chinhSachRepo.GetByIdAsync(maWhitelist);
        if (entity == null || entity.LoaiChinhSach != WhitelistPolicyType)
            throw new InvalidOperationException("Không tìm thấy rule whitelist.");

        entity.TrangThai = "Ngừng áp dụng";
        entity.NgayCapNhat = DateTime.Now;
        await _chinhSachRepo.UpdateAsync(entity);
    }

    public async Task<ChamCongDTO> AddByAdminAsync(int maNhanVien, ChamCongAdminUpdateDTO dto)
    {
        var day = dto.NgayChamCong.Date;
        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, day))
            throw new InvalidOperationException("Ngày này nhân viên có đơn nghỉ phép đã duyệt, không thể thêm chấm công.");

        if (await _repo.ExistsAnyAsync(maNhanVien, day, day))
            throw new InvalidOperationException("Nhân viên đã có bản ghi chấm công trong ngày này.");

        var entity = new ChamCong
        {
            MaNhanVien = maNhanVien,
            NgayChamCong = day,
            GioVao = dto.GioVao,
            GioRa = dto.GioRa,
            HinhThuc = string.IsNullOrWhiteSpace(dto.HinhThuc) ? "Admin nhập bù" : dto.HinhThuc.Trim(),
            TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "Bình thường" : dto.TrangThai.Trim(),
            GhiChu = string.IsNullOrWhiteSpace(dto.GhiChu) ? null : dto.GhiChu.Trim()
        };

        if (entity.GioVao.HasValue && entity.GioRa.HasValue)
        {
            var totalHours = (decimal)(entity.GioRa.Value - entity.GioVao.Value).TotalHours;
            if (totalHours < 0)
                throw new InvalidOperationException("Giờ ra phải sau giờ vào.");
            entity.TongGioLam = Math.Round(totalHours, 2);
            entity.GioLamThem = totalHours > 8 ? Math.Round(totalHours - 8, 2) : 0;
        }
        else
        {
            entity.TongGioLam = null;
            entity.GioLamThem = 0;
        }

        var created = await _repo.AddAsync(entity);
        var withNhanVien = await _repo.GetByIdWithNhanVienAsync(created.MaChamCong) ?? created;
        return MapToDTO(withNhanVien);
    }

    public async Task<ChamCongDTO?> GetTodayAsync(int maNhanVien)
    {
        var cc = await _repo.GetTodayAsync(maNhanVien);
        if (cc == null) return null;
        return MapToDTO(cc);
    }

    public Task<bool> HasApprovedLeaveOnDateAsync(int maNhanVien, DateTime ngay) =>
        _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(maNhanVien, ngay.Date);

    public async Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetByNhanVienAsync(maNhanVien, tuNgay, denNgay);
        return list.Select(MapToDTO).ToList();
    }

    public async Task<List<ChamCongDTO>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetAllInPeriodAsync(tuNgay, denNgay);
        return list.Select(MapToDTO).ToList();
    }

    public Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAsync(int maNhanVien, int year, int month) =>
        _repo.GetDistinctNgayChamCongInMonthAsync(maNhanVien, year, month);

    public Task<List<DateTime>> GetDistinctAttendanceDatesInMonthAllAsync(int year, int month) =>
        _repo.GetDistinctNgayChamCongInMonthAllAsync(year, month);

    public async Task UpdateByAdminAsync(int maChamCong, ChamCongAdminUpdateDTO dto)
    {
        var entity = await _repo.GetByIdWithNhanVienAsync(maChamCong);
        if (entity == null)
            throw new InvalidOperationException("Không tìm thấy bản ghi chấm công.");

        var newDay = dto.NgayChamCong.Date;
        if (await _donNghiPhepRepo.HasApprovedLeaveOnDateAsync(entity.MaNhanVien, newDay))
            throw new InvalidOperationException("Ngày này nhân viên có đơn nghỉ phép đã duyệt, không thể ghi nhận hoặc sửa chấm công.");

        if (newDay != entity.NgayChamCong.Date)
        {
            var dup = await _repo.ExistsOtherOnSameDayAsync(entity.MaNhanVien, newDay, maChamCong);
            if (dup)
                throw new InvalidOperationException("Nhân viên đã có bản ghi chấm công trong ngày này.");
        }

        entity.NgayChamCong = newDay;
        entity.GioVao = dto.GioVao;
        entity.GioRa = dto.GioRa;
        entity.HinhThuc = string.IsNullOrWhiteSpace(dto.HinhThuc) ? null : dto.HinhThuc.Trim();
        entity.TrangThai = string.IsNullOrWhiteSpace(dto.TrangThai) ? "Bình thường" : dto.TrangThai.Trim();
        entity.GhiChu = string.IsNullOrWhiteSpace(dto.GhiChu) ? null : dto.GhiChu.Trim();

        if (entity.GioVao.HasValue && entity.GioRa.HasValue)
        {
            var totalHours = (decimal)(entity.GioRa.Value - entity.GioVao.Value).TotalHours;
            if (totalHours < 0)
                throw new InvalidOperationException("Giờ ra phải sau giờ vào.");
            entity.TongGioLam = Math.Round(totalHours, 2);
            entity.GioLamThem = totalHours > 8 ? Math.Round(totalHours - 8, 2) : 0;
        }
        else
        {
            entity.TongGioLam = null;
            entity.GioLamThem = 0;
        }

        await _repo.UpdateAsync(entity);
    }

    private static ChamCongDTO MapToDTO(ChamCong cc) => new()
    {
        MaChamCong = cc.MaChamCong,
        MaNhanVien = cc.MaNhanVien,
        MaNV = cc.NhanVien?.MaNV,
        TenNhanVien = cc.NhanVien?.HoTen,
        TenPhongBan = cc.NhanVien?.PhongBan?.TenPhongBan,
        TenChucVu = cc.NhanVien?.ChucVu?.TenChucVu,
        NgayChamCong = cc.NgayChamCong,
        GioVao = cc.GioVao,
        GioRa = cc.GioRa,
        TongGioLam = cc.TongGioLam,
        HinhThuc = cc.HinhThuc,
        TrangThai = cc.TrangThai,
        GhiChu = cc.GhiChu,
        Hwid = cc.Hwid
    };

    private static string? NormalizeHwid(string? hwid)
    {
        if (string.IsNullOrWhiteSpace(hwid))
            return null;
        var t = hwid.Trim();
        return t.Length <= 128 ? t : t[..128];
    }

    private async Task<List<string>> GetActiveWhitelistRulesAsync()
    {
        var items = await _chinhSachRepo.FindAsync(x =>
            x.LoaiChinhSach == WhitelistPolicyType &&
            x.TrangThai == "Hoạt động");

        return items
            .Select(x => x.NoiDung?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<IPAddress> GetLocalIPv4Addresses()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.OperationalStatus == OperationalStatus.Up &&
                n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(u => u.Address)
            .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
            .Distinct()
            .ToList();
    }

    private static bool IsValidRule(string rule)
    {
        if (string.IsNullOrWhiteSpace(rule))
            return false;
        return IsValidIpv4(rule) || IsValidCidr(rule);
    }

    private static bool IsIpMatchRule(IPAddress ip, string rule)
    {
        if (IsValidIpv4(rule))
            return IPAddress.Parse(rule).Equals(ip);
        if (IsValidCidr(rule))
            return IsIpInCidr(ip, rule);
        return false;
    }

    private static bool IsValidIpv4(string input) =>
        IPAddress.TryParse(input, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork;

    private static bool IsValidCidr(string input)
    {
        var parts = input.Split('/');
        if (parts.Length != 2 || !IsValidIpv4(parts[0]))
            return false;
        return int.TryParse(parts[1], out var prefix) && prefix is >= 0 and <= 32;
    }

    private static bool IsIpInCidr(IPAddress ip, string cidr)
    {
        var parts = cidr.Split('/');
        var network = IPAddress.Parse(parts[0]);
        var prefix = int.Parse(parts[1]);

        var ipUint = ToUint(ip);
        var netUint = ToUint(network);
        var mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);

        return (ipUint & mask) == (netUint & mask);
    }

    private static uint ToUint(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
}
