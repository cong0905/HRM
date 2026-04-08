using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

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
