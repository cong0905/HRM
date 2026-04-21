using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class NhanVienService : INhanVienService
{
    private readonly INhanVienRepository _repo;

    public NhanVienService(INhanVienRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<Common.DTOs.NhanVienDTO>> GetAllAsync()
    {
        var list = await _repo.GetAllWithDetailsAsync();
        return list.Select(MapToDTO).ToList();
    }

    public async Task<Common.DTOs.NhanVienDTO?> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdWithDetailsAsync(id);
        return entity == null ? null : MapToDTO(entity);
    }

    public async Task<List<Common.DTOs.NhanVienDTO>> SearchAsync(string keyword)
    {
        var list = await _repo.SearchAsync(keyword);
        return list.Select(MapToDTO).ToList();
    }

    public async Task<Common.DTOs.NhanVienDTO> CreateAsync(NhanVienCreateDTO dto)
    {
        var entity = new Domain.Entities.NhanVienDTO
        {
            HoTen = dto.HoTen,
            NgaySinh = dto.NgaySinh,
            GioiTinh = dto.GioiTinh,
            CCCD = dto.CCCD,
            DiaChi = dto.DiaChi,
            SoDienThoai = dto.SoDienThoai,
            Email = dto.Email,
            TinhTrangHonNhan = dto.TinhTrangHonNhan,
            MaPhongBan = dto.MaPhongBan,
            MaChucVu = dto.MaChucVu,
            NgayVaoLam = dto.NgayVaoLam,
            MucLuong = dto.MucLuong,
            TrangThai = dto.TrangThai
        };

        var created = await _repo.AddAsync(entity);
        return MapToDTO(created);
    }

    public async Task UpdateAsync(int id, NhanVienCreateDTO dto)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) throw new Exception("Không tìm thấy nhân viên");

        entity.HoTen = dto.HoTen;
        entity.NgaySinh = dto.NgaySinh;
        entity.GioiTinh = dto.GioiTinh;
        entity.CCCD = dto.CCCD;
        entity.DiaChi = dto.DiaChi;
        entity.SoDienThoai = dto.SoDienThoai;
        entity.Email = dto.Email;
        entity.TinhTrangHonNhan = dto.TinhTrangHonNhan;
        entity.MaPhongBan = dto.MaPhongBan;
        entity.MaChucVu = dto.MaChucVu;
        entity.NgayVaoLam = dto.NgayVaoLam;
        entity.MucLuong = dto.MucLuong;
        entity.TrangThai = dto.TrangThai;
        entity.NgayCapNhat = DateTime.Now;

        await _repo.UpdateAsync(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity == null) throw new Exception("Không tìm thấy nhân viên");

        // Soft delete
        entity.TrangThai = "Nghỉ việc";
        entity.NgayNghiViec = DateTime.Today;
        entity.NgayCapNhat = DateTime.Now;
        await _repo.UpdateAsync(entity);
    }

    private static Common.DTOs.NhanVienDTO MapToDTO(Domain.Entities.NhanVienDTO nv) => new()
    {
        MaNhanVien = nv.MaNhanVien,
        MaNV = nv.MaNV,
        HoTen = nv.HoTen,
        NgaySinh = nv.NgaySinh,
        GioiTinh = nv.GioiTinh,
        CCCD = nv.CCCD,
        DiaChi = nv.DiaChi,
        SoDienThoai = nv.SoDienThoai,
        Email = nv.Email,
        TinhTrangHonNhan = nv.TinhTrangHonNhan,
        MaPhongBan = nv.MaPhongBan,
        TenPhongBan = nv.PhongBan?.TenPhongBan,
        MaChucVu = nv.MaChucVu,
        TenChucVu = nv.ChucVu?.TenChucVu,
        NgayVaoLam = nv.NgayVaoLam,
        MucLuong = nv.MucLuong,
        TrangThai = nv.TrangThai,
        AnhDaiDien = nv.AnhDaiDien
    };
}
