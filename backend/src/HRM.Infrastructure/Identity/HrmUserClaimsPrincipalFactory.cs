using System.Security.Claims;
using HRM.Next.Application.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HRM.Next.Infrastructure.Identity;

public sealed class HrmUserClaimsPrincipalFactory(
    UserManager<HrmUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<HrmUser, IdentityRole<Guid>>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(HrmUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var roles = await UserManager.GetRolesAsync(user);
        var permissions = HrmPermissions.GetPermissionsForRoles(roles);
        foreach (var permission in permissions)
        {
            if (!identity.HasClaim("permission", permission))
            {
                identity.AddClaim(new Claim("permission", permission));
            }
        }
        return identity;
    }
}

