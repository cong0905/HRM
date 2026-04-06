using HRM.Common.DTOs;

namespace HRM.BLL.Interfaces;

public interface IAuthService
{
    Task<UserSessionDTO?> LoginAsync(LoginDTO loginDto);
    Task<bool> ChangePasswordAsync(int maNhanVien, string oldPassword, string newPassword);
    Task<bool> RegisterAsync(Common.DTOs.RegisterDTO dto);
}
