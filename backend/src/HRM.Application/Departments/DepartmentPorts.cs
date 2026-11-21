using HRM.Next.Application.Common;
using HRM.Next.Domain.Departments;

namespace HRM.Next.Application.Departments;

public sealed record DepartmentResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

public interface IDepartmentQueries
{
    Task<PagedResult<DepartmentResponse>> ListAsync(PageRequest page, CancellationToken cancellationToken);
}

// Write methods only change tracked state. The use-case boundary owns commit.
public interface IDepartmentRepository
{
    void Add(Department department);
    Task<Department?> FindTrackedAsync(Guid id, CancellationToken cancellationToken);
}
