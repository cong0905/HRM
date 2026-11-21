namespace HRM.Next.Application.Authorization;

public interface ISecurityContext
{
    Guid? CurrentUserId { get; }
    string? CurrentUsername { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
    bool IsInRole(string role);
    Task<Guid?> GetEmployeeIdAsync(CancellationToken token = default);
}

