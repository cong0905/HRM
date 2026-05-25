using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class NhanVienService : INhanVienService
{
    private readonly INhanVienRepository _repo;
    private readonly ITaiKhoanService _taiKhoanService;
    private readonly IBangLuongService _bangLuongService;

    public NhanVienService(
        INhanVienRepository repo,
        ITaiKhoanService taiKhoanService,
        IBangLuongService bangLuongService)
    {
        _repo = repo;
        _taiKhoanService = taiKhoanService;
        _bangLuongService = bangLuongService;
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

    public async Task<List<Common.DTOs.NhanVienDTO>> FilterAsync(string? keyword, int? maPhongBan, string? trangThai, string? gioiTinh)
    {
        var list = await _repo.FilterAsync(keyword, maPhongBan, trangThai, gioiTinh);
        return list.Select(MapToDTO).ToList();
    }

    public async Task<Common.DTOs.NhanVienDTO> CreateAsync(NhanVienCreateDTO dto)
    {
        var entity = new NhanVien
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


        try
        {
            string tenDangNhap = !string.IsNullOrWhiteSpace(dto.Email)
                ? dto.Email.Trim().ToLower()
                : GenerateUsername(dto.HoTen);

            string matKhauMacDinh = dto.NgaySinh.ToString("dd/MM/yyyy");

            await _taiKhoanService.CreateAsync(new RegisterDTO
            {
                MaNhanVien = created.MaNhanVien,
                TenDangNhap = tenDangNhap,
                MatKhau = matKhauMacDinh,
                VaiTro = "Nhân viên"
            });
        }
        catch
        {

        }

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
        var mucLuongCu = entity.MucLuong;
        entity.MucLuong = dto.MucLuong;
        entity.TrangThai = dto.TrangThai;
        entity.NgayCapNhat = DateTime.Now;

        await _repo.UpdateAsync(entity);

        if (entity.MucLuong != mucLuongCu)
            await _bangLuongService.DongBoBangLuongTheoNhanVienAsync(id);
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

    private static Common.DTOs.NhanVienDTO MapToDTO(NhanVien nv) => new()
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

    private static string GenerateUsername(string hoTen)
    {
        var normalized = RemoveDiacritics(hoTen.Trim().ToLower());
        var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "user" + DateTime.Now.Ticks;
        // Tên + họ đệm gộp lại
        var ten = parts[^1]; // tên (phần cuối)
        var hoDem = string.Join("", parts[..^1]); // họ đệm gộp
        return string.IsNullOrEmpty(hoDem) ? ten : $"{ten}.{hoDem}";
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // Xử lý thêm ký tự đặc biệt tiếng Việt
                sb.Append(c switch
                {
                    'đ' => 'd',
                    'Đ' => 'D',
                    _ => c
                });
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }
}
