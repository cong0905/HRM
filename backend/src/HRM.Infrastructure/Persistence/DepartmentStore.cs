using HRM.Next.Application.Common;
using HRM.Next.Application.Departments;
using HRM.Next.Domain.Departments;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Persistence;

public sealed class DepartmentStore(HrmDbContext db) : IDepartmentQueries, IDepartmentRepository
{
    public void Add(Department department) => db.Departments.Add(department);

    public Task<Department?> FindTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        db.Departments.AsTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<PagedResult<DepartmentResponse>> ListAsync(PageRequest page, CancellationToken cancellationToken)
    {
        var query = db.Departments.AsNoTracking();
        var count = await query.LongCountAsync(cancellationToken);
        var ordered = page.Sort switch
        {
            "-name" => query.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            "createdAt" => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            "-createdAt" => query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Name).ThenBy(x => x.Id)
        };
        var items = await ordered.Skip(page.Offset).Take(page.PageSize)
            .Select(x => new DepartmentResponse(x.Id, x.Name, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        return new PagedResult<DepartmentResponse>(items, page.Page, page.PageSize, count);
    }
}
