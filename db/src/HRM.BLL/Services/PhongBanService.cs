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
