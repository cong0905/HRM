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
}
