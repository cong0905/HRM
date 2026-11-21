using HRM.Next.Application.Common;
using HRM.Next.Application.Departments;
using HRM.Next.Domain.Departments;
using HRM.Next.Domain;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Next.Tests.Integration;

public sealed record TransactionRequest(bool FailSecondStep);
public sealed record Conventions(DateOnly Date, decimal Amount, DateTimeOffset Timestamp);
public sealed record ScopeResult(Guid ContextId, bool SameWithinRequest, int TrackedEntries);

// Test-only routes exercise real scoped services/SQL in the product pipeline.
// ApiFactory is the only place that adds this assembly as an MVC ApplicationPart.
[ApiController]
[Route("_tests/foundation")]
[AllowAnonymous]
public sealed class ProbeController : ControllerBase
{
    [HttpGet("scope")]
    public async Task<ScopeResult> Scope([FromServices] HrmDbContext db,
        [FromServices] IServiceProvider services, CancellationToken cancellationToken)
    {
        await db.Departments.CountAsync(cancellationToken);
        await Task.Delay(25, cancellationToken);
        await db.Departments.CountAsync(cancellationToken);
        return new ScopeResult(db.ContextId.InstanceId,
            ReferenceEquals(db, services.GetRequiredService<HrmDbContext>()), db.ChangeTracker.Entries().Count());
    }

    [HttpPost("transaction")]
    public async Task<ActionResult> Transaction(TransactionRequest request,
        [FromServices] IDepartmentRepository departments, [FromServices] IUnitOfWork unit,
        [FromServices] TimeProvider clock, CancellationToken cancellationToken)
    {
        await unit.ExecuteAsync(async token =>
        {
            departments.Add(Department.Create("Transaction step 1", clock.GetUtcNow()));
            await unit.SaveChangesAsync(token); // Real INSERT inside the uncommitted transaction.
            departments.Add(Department.Create("Transaction step 2", clock.GetUtcNow()));
            await unit.SaveChangesAsync(token); // Both steps reached SQL before the injected failure.
            if (request.FailSecondStep)
                throw new InvalidOperationException("SQL secret marker Password=DO_NOT_EXPOSE; SELECT * FROM private_table");
            return true;
        }, cancellationToken);
        return NoContent();
    }

    [HttpGet("read-tracking")]
    public async Task<ScopeResult> ReadTracking([FromServices] IDepartmentQueries queries,
        [FromServices] HrmDbContext db, CancellationToken cancellationToken)
    {
        await queries.ListAsync(PageRequest.Create(), cancellationToken);
        return new ScopeResult(db.ContextId.InstanceId, true, db.ChangeTracker.Entries().Count());
    }

    [HttpPost("conventions")]
    public Conventions Echo(Conventions value) => value;

    [HttpGet("errors/{kind}")]
    public ActionResult Error(string kind) => throw kind switch
    {
        "conflict" => new ResourceConflictException("resource.conflict", "Tài nguyên đã thay đổi."),
        _ => new DomainRuleException("domain.rule", "Thao tác vi phạm quy tắc nghiệp vụ.")
    };
}
