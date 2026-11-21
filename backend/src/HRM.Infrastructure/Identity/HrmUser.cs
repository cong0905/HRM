using Microsoft.AspNetCore.Identity;

namespace HRM.Next.Infrastructure.Identity;

public sealed class HrmUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; }
}

public sealed class AccountSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SecurityStamp { get; set; } = string.Empty;
    public Guid RefreshVersion { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }
    public bool Revoked { get; set; }
}
