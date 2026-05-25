using HRM.BLL.Interfaces;
using HRM.Common.DTOs;
using HRM.Common.Helpers;
using HRM.DAL.Repositories;
using HRM.Domain.Entities;

namespace HRM.BLL.Services;

public class AuthService : IAuthService
{
    private readonly ITaiKhoanRepository _taiKhoanRepo;
    private readonly IPasswordResetTokenRepository _tokenRepo;
    private readonly IEmailSender _emailSender;

    public AuthService(ITaiKhoanRepository taiKhoanRepo, IPasswordResetTokenRepository tokenRepo, IEmailSender emailSender)
    {
        _taiKhoanRepo = taiKhoanRepo;
        _tokenRepo = tokenRepo;
        _emailSender = emailSender;
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

    public async Task<string?> SendPasswordResetAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var taiKhoan = await _taiKhoanRepo.GetByNhanVienEmailAsync(email.Trim().ToLower());

        // Always return true to avoid email enumeration
        if (taiKhoan == null) return null;

        var otpCode = Random.Shared.Next(100000, 999999).ToString();
        var token = new PasswordResetToken
        {
            MaTaiKhoan = taiKhoan.MaTaiKhoan,
            Email = taiKhoan.NhanVien?.Email,
            Token = otpCode,
            Purpose = "ResetPassword",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // OTP expires faster
            IsUsed = false
        };

        await _tokenRepo.AddAsync(token);

        var resetLink = $"https://your-hrm.local/reset-password?token={token.Token}";
        var body = $"<p>Xin chào {taiKhoan.NhanVien?.HoTen},</p><p>Nhấn vào liên kết để đặt lại mật khẩu: <a href=\"{resetLink}\">Reset mật khẩu</a></p><p>Liên kết có hiệu lực 1 giờ.</p>";

        var emailSent = false;
        if (!string.IsNullOrEmpty(token.Email))
        {
            try
            {
                await _emailSender.SendEmailAsync(token.Email, "Yêu cầu đặt lại mật khẩu", body);
                emailSent = true;
            }
            catch
            {
                // ignore email send errors for now
                emailSent = false;
            }
        }

        // If email isn't sent (e.g., dev), return token so dev can paste into reset form
        return emailSent ? null : token.Token;
    }

    public async Task<bool> ResetPasswordWithTokenAsync(string tokenStr, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(tokenStr)) return false;

        var token = await _tokenRepo.GetByTokenAsync(tokenStr);
        if (token == null) return false;

        if (!PasswordPolicy.IsValid(newPassword, out var reason))
        {
            return false;
        }

        TaiKhoan? taiKhoan = null;
        if (token.MaTaiKhoan.HasValue)
        {
            taiKhoan = await _taiKhoanRepo.GetByIdAsync(token.MaTaiKhoan.Value);
        }

        if (taiKhoan == null && !string.IsNullOrEmpty(token.Email))
        {
            taiKhoan = await _taiKhoanRepo.GetByNhanVienEmailAsync(token.Email);
        }

        if (taiKhoan == null) return false;

        taiKhoan.MatKhauHash = PasswordHelper.HashPassword(newPassword);
        await _taiKhoanRepo.UpdateAsync(taiKhoan);

        token.IsUsed = true;
        await _tokenRepo.UpdateAsync(token);

        return true;
    }

    public async Task<bool> SendEmailVerificationAsync(int maNhanVien)
    {
        var accounts = await _taiKhoanRepo.FindAsync(tk => tk.MaNhanVien == maNhanVien);
        var taiKhoan = accounts.FirstOrDefault();
        if (taiKhoan == null || string.IsNullOrEmpty(taiKhoan.NhanVien?.Email)) return false;

        var token = new PasswordResetToken
        {
            MaTaiKhoan = taiKhoan.MaTaiKhoan,
            Email = taiKhoan.NhanVien.Email,
            Token = Guid.NewGuid().ToString("N"),
            Purpose = "VerifyEmail",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsUsed = false
        };

        await _tokenRepo.AddAsync(token);

        var link = $"https://your-hrm.local/verify-email?token={token.Token}";
        var body = $"<p>Xin chào {taiKhoan.NhanVien.HoTen},</p><p>Nhấn vào liên kết để xác thực email: <a href=\"{link}\">Xác thực email</a></p>";
        await _emailSender.SendEmailAsync(token.Email!, "Xác thực email", body);
        return true;
    }

    public async Task<bool> VerifyEmailTokenAsync(string tokenStr)
    {
        var token = await _tokenRepo.GetByTokenAsync(tokenStr);
        if (token == null || token.Purpose != "VerifyEmail") return false;

        token.IsUsed = true;
        await _tokenRepo.UpdateAsync(token);

        // Optionally mark employee's email as confirmed - requires schema change.
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