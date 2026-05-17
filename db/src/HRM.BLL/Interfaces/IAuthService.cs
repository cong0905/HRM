using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IAuthService
{
    Task<UserSessionDTO?> LoginAsync(LoginDTO loginDto);
    Task<bool> ChangePasswordAsync(int maNhanVien, string oldPassword, string newPassword);
    Task<bool> RegisterAsync(Common.DTOs.RegisterDTO dto);
    Task<bool> SendPasswordResetAsync(string email);
    Task<bool> ResetPasswordWithTokenAsync(string token, string newPassword);
    Task<bool> SendEmailVerificationAsync(int maNhanVien);
    Task<bool> VerifyEmailTokenAsync(string token);
}
