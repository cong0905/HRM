using System;

namespace HRM.Domain.Entities;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int? MaTaiKhoan { get; set; }
    public string? Email { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Purpose { get; set; } = "ResetPassword"; // or VerifyEmail
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    // Navigation
    public TaiKhoan? TaiKhoan { get; set; }
}
