using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.Common.Helpers;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class AuthService : IAuthService
{
    private readonly ITaiKhoanRepository _taiKhoanRepo;

    public AuthService(ITaiKhoanRepository taiKhoanRepo)
    {
        _taiKhoanRepo = taiKhoanRepo;
    }

    public async Task<UserSessionDTO?> LoginAsync(LoginDTO loginDto)
    {
        var taiKhoan = await _taiKhoanRepo.GetByUsernameAsync(loginDto.TenDangNhap);
        if (taiKhoan == null) return null;

        if (!PasswordHelper.VerifyPassword(loginDto.MatKhau, taiKhoan.MatKhauHash))
            return null;

        taiKhoan.LanDangNhapCuoi = DateTime.Now;
        await _taiKhoanRepo.UpdateAsync(taiKhoan);

        return new UserSessionDTO
        {
            MaTaiKhoan = taiKhoan.MaTaiKhoan,
            MaNhanVien = taiKhoan.MaNhanVien,
            HoTen = taiKhoan.NhanVien.HoTen,
            VaiTro = taiKhoan.VaiTro,
            TenPhongBan = taiKhoan.NhanVien.PhongBan?.TenPhongBan
        };
    }

    public async Task<bool> ChangePasswordAsync(int maNhanVien, string oldPassword, string newPassword)
    {
        var accounts = await _taiKhoanRepo.FindAsync(tk => tk.MaNhanVien == maNhanVien);
        var taiKhoan = accounts.FirstOrDefault();
        if (taiKhoan == null) return false;

        if (!PasswordHelper.VerifyPassword(oldPassword, taiKhoan.MatKhauHash))
            return false;

        taiKhoan.MatKhauHash = PasswordHelper.HashPassword(newPassword);
        await _taiKhoanRepo.UpdateAsync(taiKhoan);
        return true;
    }

    public async Task<bool> RegisterAsync(Common.DTOs.RegisterDTO dto)
    {
        var existingAccount = await _taiKhoanRepo.GetByUsernameAsync(dto.TenDangNhap);
        if (existingAccount != null)
        {
            throw new Exception("Tên đăng nhập đã tồn tại");
        }

        var taiKhoanMoi = new TaiKhoan
        {
            TenDangNhap = dto.TenDangNhap,
            MatKhauHash = PasswordHelper.HashPassword(dto.MatKhau),
            VaiTro = dto.VaiTro,
            MaNhanVien = dto.MaNhanVien,
            TrangThai = "Hoạt động",
            NgayTao = DateTime.Now,
            LanDangNhapCuoi = null
        };

        await _taiKhoanRepo.AddAsync(taiKhoanMoi);
        return true;
    }
}