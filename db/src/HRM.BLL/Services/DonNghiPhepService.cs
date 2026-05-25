using HRM.BLL.Interfaces;
using HRM.Common.Constants;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class DonNghiPhepService : IDonNghiPhepService
{
    private const int MaLoaiPhepNam = 1;

    private readonly IDonNghiPhepRepository _repo;
    private readonly ISoNgayPhepRepository _soNgayRepo;
    private readonly IRepository<LoaiNghiPhep> _loaiRepo;
    private readonly IChamCongRepository _chamCongRepo;

    public DonNghiPhepService(
        IDonNghiPhepRepository repo,
        ISoNgayPhepRepository soNgayRepo,
        IRepository<LoaiNghiPhep> loaiRepo,
        IChamCongRepository chamCongRepo)
    {
        _repo = repo;
        _soNgayRepo = soNgayRepo;
        _loaiRepo = loaiRepo;
        _chamCongRepo = chamCongRepo;
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

    public async Task<List<DonNghiPhepDTO>> GetAllTraCuuAsync(string? keyword, DateTime? tuNgay, DateTime? denNgay)
    {
        var list = await _repo.SearchAllAsync(keyword, tuNgay, denNgay);
        return list.Select(MapToDTO).ToList();
    }

    public async Task<List<LoaiNghiPhepDTO>> GetLoaiNghiPhepAsync()
    {
        var list = await _loaiRepo.GetAllAsync();
        return list
            .OrderBy(l => l.MaLoaiPhep)
            .Select(l => new LoaiNghiPhepDTO
            {
                MaLoaiPhep = l.MaLoaiPhep,
                TenLoaiPhep = l.TenLoaiPhep,
                CoLuong = l.CoLuong
            })
            .ToList();
    }

    public async Task<SoNgayPhepDTO> GetSoNgayPhepAsync(int maNhanVien, int nam)
    {
        var s = await _soNgayRepo.EnsureAsync(maNhanVien, nam);
        return new SoNgayPhepDTO
        {
            Nam = s.Nam,
            TongSoNgayPhep = s.TongSoNgayPhep,
            SoNgayDaSuDung = s.SoNgayDaSuDung,
            SoNgayConLai = s.SoNgayConLai
        };
    }

    public async Task CreateAsync(int maNhanVien, int maLoaiPhep, DateTime ngayBatDau, DateTime ngayKetThuc, string lyDo)
    {
        var tu = ngayBatDau.Date;
        var den = ngayKetThuc.Date;
        if (tu > den)
            throw new InvalidOperationException("Ngày bắt đầu không được sau ngày kết thúc.");

        if (string.IsNullOrWhiteSpace(lyDo))
            throw new InvalidOperationException("Vui lòng nhập lý do nghỉ.");

        var soNgay = (decimal)(den - tu).TotalDays + 1;
        var soNgayInt = (int)Math.Ceiling((double)soNgay);

        var existing = await _repo.GetByNhanVienAsync(maNhanVien);
        foreach (var d in existing)
        {
            if (d.TrangThai != DonNghiPhepTrangThai.ChoDuyet && d.TrangThai != DonNghiPhepTrangThai.DaDuyet)
                continue;
            var a = d.NgayBatDau.Date;
            var b = d.NgayKetThuc.Date;
            if (tu <= b && den >= a)
                throw new InvalidOperationException("Khoảng thời gian này trùng với một đơn nghỉ đang chờ duyệt hoặc đã duyệt.");
        }

        if (await _chamCongRepo.ExistsAnyAsync(maNhanVien, tu, den))
            throw new InvalidOperationException("Trong khoảng ngày đã chọn có ít nhất một ngày đã có chấm công. Vui lòng điều chỉnh đơn nghỉ.");

        if (maLoaiPhep == MaLoaiPhepNam)
        {
            var soPhep = await _soNgayRepo.EnsureAsync(maNhanVien, tu.Year);
            if (soPhep.SoNgayConLai < soNgayInt)
                throw new InvalidOperationException($"Số ngày phép năm {tu.Year} còn lại ({soPhep.SoNgayConLai}) không đủ cho đơn {soNgayInt} ngày.");
        }

        await _repo.AddAsync(new DonNghiPhep
        {
            MaNhanVien = maNhanVien,
            MaLoaiPhep = maLoaiPhep,
            NgayBatDau = tu,
            NgayKetThuc = den,
            SoNgayNghi = soNgay,
            LyDo = lyDo.Trim(),
            TrangThai = DonNghiPhepTrangThai.ChoDuyet,
            NgayTao = DateTime.Now
        });
    }

    public async Task ApproveAsync(int maDonPhep, int nguoiDuyet)
    {
        var don = await _repo.GetByIdAsync(maDonPhep);
        if (don == null) throw new InvalidOperationException("Không tìm thấy đơn.");
        if (don.TrangThai != DonNghiPhepTrangThai.ChoDuyet)
            throw new InvalidOperationException("Chỉ có thể duyệt đơn đang ở trạng thái chờ duyệt.");

        var tuDon = don.NgayBatDau.Date;
        var denDon = don.NgayKetThuc.Date;
        if (await _chamCongRepo.ExistsAnyAsync(don.MaNhanVien, tuDon, denDon))
            throw new InvalidOperationException("Không thể duyệt: trong khoảng nghỉ nhân viên đã có bản ghi chấm công.");

        don.TrangThai = DonNghiPhepTrangThai.DaDuyet;
        don.NguoiPheDuyet = nguoiDuyet;
        don.NgayPheDuyet = DateTime.Now;
        await _repo.UpdateAsync(don);

        if (don.MaLoaiPhep == MaLoaiPhepNam)
        {
            var nam = don.NgayBatDau.Year;
            var so = await _soNgayRepo.EnsureAsync(don.MaNhanVien, nam);
            var them = (int)Math.Ceiling((double)don.SoNgayNghi);
            so.SoNgayDaSuDung += them;
            await _soNgayRepo.UpdateAsync(so);
        }
    }

    public async Task RejectAsync(int maDonPhep, int nguoiDuyet, string lyDoTuChoi)
    {
        var don = await _repo.GetByIdAsync(maDonPhep);
        if (don == null) throw new InvalidOperationException("Không tìm thấy đơn.");
        if (don.TrangThai != DonNghiPhepTrangThai.ChoDuyet)
            throw new InvalidOperationException("Chỉ có thể từ chối đơn đang ở trạng thái chờ duyệt.");

        if (string.IsNullOrWhiteSpace(lyDoTuChoi))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối.");

        don.TrangThai = DonNghiPhepTrangThai.TuChoi;
        don.NguoiPheDuyet = nguoiDuyet;
        don.NgayPheDuyet = DateTime.Now;
        don.LyDoTuChoi = lyDoTuChoi.Trim();
        await _repo.UpdateAsync(don);
    }

    public async Task CancelAsync(int maDonPhep, int maNhanVienChuDon, string lyDoHuy)
    {
        var don = await _repo.GetByIdAsync(maDonPhep);
        if (don == null) throw new InvalidOperationException("Không tìm thấy đơn.");
        if (don.MaNhanVien != maNhanVienChuDon)
            throw new InvalidOperationException("Bạn không thể hủy đơn của người khác.");
        if (don.TrangThai != DonNghiPhepTrangThai.ChoDuyet)
            throw new InvalidOperationException("Chỉ có thể hủy đơn đang chờ duyệt.");

        if (string.IsNullOrWhiteSpace(lyDoHuy))
            lyDoHuy = "Người lao động hủy đơn";

        don.TrangThai = DonNghiPhepTrangThai.DaHuy;
        don.NgayHuy = DateTime.Now;
        don.LyDoHuy = lyDoHuy.Trim();
        await _repo.UpdateAsync(don);
    }

    private static DonNghiPhepDTO MapToDTO(DonNghiPhep d) => new()
    {
        MaDonPhep = d.MaDonPhep,
        MaNhanVien = d.MaNhanVien,
        MaNV = d.NhanVien?.MaNV,
        TenNhanVien = d.NhanVien?.HoTen,
        TenPhongBan = d.NhanVien?.PhongBan?.TenPhongBan,
        TenChucVu = d.NhanVien?.ChucVu?.TenChucVu,
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
