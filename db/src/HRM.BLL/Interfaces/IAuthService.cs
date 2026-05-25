using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IAuthService
{
    Task<UserSessionDTO?> LoginAsync(LoginDTO loginDto);
    Task<bool> ChangePasswordAsync(int maNhanVien, string oldPassword, string newPassword);
    Task<bool> RegisterAsync(Common.DTOs.RegisterDTO dto);
    // Returns token string for dev (when email not sent), otherwise returns null
    Task<string?> SendPasswordResetAsync(string email);
    Task<bool> ResetPasswordWithTokenAsync(string token, string newPassword);
    Task<bool> SendEmailVerificationAsync(int maNhanVien);
    Task<bool> VerifyEmailTokenAsync(string token);
}
