using System.Security.Claims;
using HRM.Next.Application.Authorization;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Identity;

public sealed class SecurityContext(IHttpContextAccessor httpContextAccessor, HrmDbContext db) : ISecurityContext
{
    private Guid? _cachedEmployeeId;
    private bool _employeeIdResolved;

    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? CurrentUserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? CurrentUsername => Principal?.Identity?.Name;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToList()
        ?? (IReadOnlyList<string>)[];

    public IReadOnlyList<string> Permissions =>
        Principal?.FindAll("permission").Select(c => c.Value).Distinct().ToList()
        ?? (IReadOnlyList<string>)[];

    public bool HasPermission(string permission) =>
        Principal?.HasClaim("permission", permission) ?? false;

    public bool IsInRole(string role) =>
        Principal?.IsInRole(role) ?? false;

    public async Task<Guid?> GetEmployeeIdAsync(CancellationToken token = default)
    {
        if (_employeeIdResolved) return _cachedEmployeeId;
        var userId = CurrentUserId;
        if (!userId.HasValue)
        {
            _employeeIdResolved = true;
            return null;
        }

        _cachedEmployeeId = await db.EmployeeProfiles.AsNoTracking()
            .Where(e => e.UserId == userId.Value)
            .Select(e => (Guid?)e.Id)
            .SingleOrDefaultAsync(token);

        _employeeIdResolved = true;
        return _cachedEmployeeId;
    }
}

