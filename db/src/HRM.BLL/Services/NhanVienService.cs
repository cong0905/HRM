using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;
using HRM.Common.Helpers;

namespace HRM.BLL.Services;

public class NhanVienService : INhanVienService
{
    private readonly INhanVienRepository _repo;
    private readonly ITaiKhoanService _taiKhoanService;

    public NhanVienService(INhanVienRepository repo, ITaiKhoanService taiKhoanService)
    {
        _repo = repo;
        _taiKhoanService = taiKhoanService;
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

        // === Tự động tạo tài khoản cho nhân viên mới ===
        try
        {
            // Tên đăng nhập: dùng email nếu có, không thì tạo từ tên
            string tenDangNhap = !string.IsNullOrWhiteSpace(dto.Email)
                ? dto.Email.Trim().ToLower()
                : GenerateUsername(dto.HoTen);

            // Mật khẩu mặc định: ngày tháng năm sinh (dd/MM/yyyy)
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
            // Không để lỗi tạo tài khoản ảnh hưởng đến việc tạo nhân viên
            // Tài khoản có thể được tạo thủ công sau
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

    /// <summary>Tạo tên đăng nhập từ họ tên (bỏ dấu, viết thường). VD: "Nguyễn Văn An" → "an.nguyenvan"</summary>
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
