using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class PhongBanService : IPhongBanService
{
    private readonly IPhongBanRepository _repo;

    public PhongBanService(IPhongBanRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<PhongBanDTO>> GetAllAsync()
    {
        var list = await _repo.GetAllWithTruongPhongAsync();
        return list.Select(pb => new PhongBanDTO
        {
            MaPhongBan = pb.MaPhongBan,
            TenPhongBan = pb.TenPhongBan,
            MoTaChucNang = pb.MoTaChucNang,
            DiaDiemLamViec = pb.DiaDiemLamViec,
            TrangThai = pb.TrangThai,
            TenTruongPhong = pb.TruongPhong?.HoTen,
            SoNhanVien = pb.DanhSachNhanVien.Count
        }).ToList();
    }

    public async Task<PhongBanDTO?> GetByIdAsync(int id)
    {
        var pb = await _repo.GetByIdAsync(id);
        if (pb == null) return null;
        return new PhongBanDTO
        {
            MaPhongBan = pb.MaPhongBan,
            TenPhongBan = pb.TenPhongBan,
            MoTaChucNang = pb.MoTaChucNang,
            DiaDiemLamViec = pb.DiaDiemLamViec,
            TrangThai = pb.TrangThai
        };
    }

    public async Task CreateAsync(PhongBanDTO dto)
    {
        await _repo.AddAsync(new PhongBan
        {
            TenPhongBan = dto.TenPhongBan,
            MoTaChucNang = dto.MoTaChucNang,
            DiaDiemLamViec = dto.DiaDiemLamViec,
            TrangThai = dto.TrangThai
        });
    }

    public async Task UpdateAsync(PhongBanDTO dto)
    {
        var entity = await _repo.GetByIdAsync(dto.MaPhongBan);
        if (entity == null) throw new Exception("Không tìm thấy phòng ban");

        entity.TenPhongBan = dto.TenPhongBan;
        entity.MoTaChucNang = dto.MoTaChucNang;
        entity.DiaDiemLamViec = dto.DiaDiemLamViec;
        entity.TrangThai = dto.TrangThai;
        entity.NgayCapNhat = DateTime.Now;

        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) throw new Exception("Không tìm thấy phòng ban");
        entity.TrangThai = "Đã giải thể";
        await _repo.UpdateAsync(entity);
    }
}

public class ChamCongService : IChamCongService
{
    private readonly IChamCongRepository _repo;

    public ChamCongService(IChamCongRepository repo)
    {
        _repo = repo;
    }

    public async Task<ChamCongDTO?> CheckInAsync(int maNhanVien)
    {
        var existing = await _repo.GetTodayAsync(maNhanVien);
        if (existing != null) return null; // Đã check-in rồi

        var chamCong = new ChamCong
        {
            MaNhanVien = maNhanVien,
            NgayChamCong = DateTime.Today,
            GioVao = DateTime.Now.TimeOfDay,
            HinhThuc = "Phần mềm",
            TrangThai = DateTime.Now.TimeOfDay > new TimeSpan(8, 30, 0) ? "Đi muộn" : "Bình thường"
        };

        var created = await _repo.AddAsync(chamCong);
        return new ChamCongDTO
        {
            MaChamCong = created.MaChamCong,
            MaNhanVien = created.MaNhanVien,
            NgayChamCong = created.NgayChamCong,
            GioVao = created.GioVao,
            TrangThai = created.TrangThai
        };
    }

    public async Task<ChamCongDTO?> CheckOutAsync(int maNhanVien)
    {
        var existing = await _repo.GetTodayAsync(maNhanVien);
        if (existing == null || existing.GioRa != null) return null;

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
            TrangThai = existing.TrangThai
        };
    }

    public async Task<ChamCongDTO?> GetTodayAsync(int maNhanVien)
    {
        var cc = await _repo.GetTodayAsync(maNhanVien);
        if (cc == null) return null;
        return MapToDto(cc);
    }

    public async Task<List<ChamCongDTO>> GetHistoryAsync(int maNhanVien, DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetByNhanVienAsync(maNhanVien, tuNgay, denNgay);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<ChamCongDTO>> GetAllInPeriodAsync(DateTime tuNgay, DateTime denNgay)
    {
        var list = await _repo.GetAllInPeriodAsync(tuNgay, denNgay);
        return list.Select(MapToDto).ToList();
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

    private static ChamCongDTO MapToDto(ChamCong cc) => new()
    {
        MaChamCong = cc.MaChamCong,
        MaNhanVien = cc.MaNhanVien,
        TenNhanVien = cc.NhanVien?.HoTen,
        NgayChamCong = cc.NgayChamCong,
        GioVao = cc.GioVao,
        GioRa = cc.GioRa,
        TongGioLam = cc.TongGioLam,
        HinhThuc = cc.HinhThuc,
        TrangThai = cc.TrangThai,
        GhiChu = cc.GhiChu
    };
}

public class DonNghiPhepService : IDonNghiPhepService
{
    private readonly IDonNghiPhepRepository _repo;

    public DonNghiPhepService(IDonNghiPhepRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<DonNghiPhepDTO>> GetByNhanVienAsync(int maNhanVien)
    {
        var list = await _repo.GetByNhanVienAsync(maNhanVien);
        return list.Select(MapToDTO).ToList();
    }

    public async Task<List<DonNghiPhepDTO>> GetPendingAsync()
    {
        var list = await _repo.GetPendingAsync();
        return list.Select(MapToDTO).ToList();
    }

    public async Task CreateAsync(int maNhanVien, int maLoaiPhep, DateTime ngayBatDau, DateTime ngayKetThuc, string lyDo)
    {
        var soNgay = (decimal)(ngayKetThuc - ngayBatDau).TotalDays + 1;
        await _repo.AddAsync(new DonNghiPhep
        {
            MaNhanVien = maNhanVien,
            MaLoaiPhep = maLoaiPhep,
            NgayBatDau = ngayBatDau,
            NgayKetThuc = ngayKetThuc,
            SoNgayNghi = soNgay,
            LyDo = lyDo,
            TrangThai = "Chờ duyệt"
        });
    }

    public async Task ApproveAsync(int maDonPhep, int nguoiDuyet)
    {
        var don = await _repo.GetByIdAsync(maDonPhep);
        if (don == null) throw new Exception("Không tìm thấy đơn");
        don.TrangThai = "Đã duyệt";
        don.NguoiPheDuyet = nguoiDuyet;
        don.NgayPheDuyet = DateTime.Now;
        await _repo.UpdateAsync(don);
    }

    public async Task RejectAsync(int maDonPhep, int nguoiDuyet, string lyDoTuChoi)
    {
        var don = await _repo.GetByIdAsync(maDonPhep);
        if (don == null) throw new Exception("Không tìm thấy đơn");
        don.TrangThai = "Từ chối";
        don.NguoiPheDuyet = nguoiDuyet;
        don.NgayPheDuyet = DateTime.Now;
        don.LyDoTuChoi = lyDoTuChoi;
        await _repo.UpdateAsync(don);
    }

    public async Task CancelAsync(int maDonPhep, string lyDoHuy)
    {
        var don = await _repo.GetByIdAsync(maDonPhep);
        if (don == null) throw new Exception("Không tìm thấy đơn");
        don.TrangThai = "Đã hủy";
        don.NgayHuy = DateTime.Now;
        don.LyDoHuy = lyDoHuy;
        await _repo.UpdateAsync(don);
    }

    private static DonNghiPhepDTO MapToDTO(DonNghiPhep d) => new()
    {
        MaDonPhep = d.MaDonPhep,
        MaNhanVien = d.MaNhanVien,
        TenNhanVien = d.NhanVien?.HoTen,
        TenLoaiPhep = d.LoaiNghiPhep?.TenLoaiPhep,
        NgayBatDau = d.NgayBatDau,
        NgayKetThuc = d.NgayKetThuc,
        SoNgayNghi = d.SoNgayNghi,
        LyDo = d.LyDo,
        TrangThai = d.TrangThai,
        TenNguoiDuyet = d.NguoiPheDuyetNav?.HoTen,
        NgayTao = d.NgayTao
    };
}
