using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Departments;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Api.Controllers;

public sealed record OrganizationWriteRequest([Required, StringLength(100)] string Name,
    [Required, StringLength(500)] string Reason, string? ExpectedName = null);
public sealed record OrganizationDeleteRequest([Required] string ExpectedName, [Required, StringLength(500)] string Reason);
public sealed record PositionResponse(Guid Id, string Name, DateTimeOffset CreatedAtUtc);

[ApiController, Route("api/v1")]
public sealed class OrganizationController(HrmDbContext db, IAuditRepository audit, ISecurityContext security, TimeProvider clock) : ControllerBase
{
    private ObjectResult Failure(int status, string code) => new(ApiProblems.Create(HttpContext, status, code))
        { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpGet("positions", Name = "ListPositions"), Authorize(Policy = HrmPolicies.DepartmentsRead)]
    [ProducesResponseType<PagedResult<PositionResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<PositionResponse>>> Positions(CancellationToken token,
        [Range(1, int.MaxValue)] int page = 1, [Range(1, 100)] int pageSize = 20)
    {
        var query = db.Positions.AsNoTracking();
        var total = await query.LongCountAsync(token);
        var items = await query.OrderBy(p => p.Name).ThenBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PositionResponse(p.Id, p.Name, p.CreatedAtUtc)).ToListAsync(token);
        return Ok(new PagedResult<PositionResponse>(items, page, pageSize, total));
    }

    [HttpPost("departments", Name = "CreateDepartment"), Authorize(Policy = HrmPolicies.DepartmentsManage)]
    [ProducesResponseType<PositionResponse>(201)]
    public Task<IActionResult> CreateDepartment(OrganizationWriteRequest request, CancellationToken token) => Write(false, null, request, token);
    [HttpPut("departments/{id:guid}", Name = "UpdateDepartment"), Authorize(Policy = HrmPolicies.DepartmentsManage)]
    [ProducesResponseType<PositionResponse>(200)]
    public Task<IActionResult> UpdateDepartment(Guid id, OrganizationWriteRequest request, CancellationToken token) => Write(false, id, request, token);
    [HttpDelete("departments/{id:guid}", Name = "DeleteDepartment"), Authorize(Policy = HrmPolicies.DepartmentsManage)]
    [ProducesResponseType(204)]
    public Task<IActionResult> DeleteDepartment(Guid id, OrganizationDeleteRequest request, CancellationToken token) => Delete(false, id, request, token);
    [HttpPost("positions", Name = "CreatePosition"), Authorize(Policy = HrmPolicies.DepartmentsManage)]
    [ProducesResponseType<PositionResponse>(201)]
    public Task<IActionResult> CreatePosition(OrganizationWriteRequest request, CancellationToken token) => Write(true, null, request, token);
    [HttpPut("positions/{id:guid}", Name = "UpdatePosition"), Authorize(Policy = HrmPolicies.DepartmentsManage)]
    [ProducesResponseType<PositionResponse>(200)]
    public Task<IActionResult> UpdatePosition(Guid id, OrganizationWriteRequest request, CancellationToken token) => Write(true, id, request, token);
    [HttpDelete("positions/{id:guid}", Name = "DeletePosition"), Authorize(Policy = HrmPolicies.DepartmentsManage)]
    [ProducesResponseType(204)]
    public Task<IActionResult> DeletePosition(Guid id, OrganizationDeleteRequest request, CancellationToken token) => Delete(true, id, request, token);

    private Task Audit(bool position, Guid id, string action, string reason, CancellationToken token) => audit.RecordAsync(new AuditEvent
    {
        Id = Guid.NewGuid(), OccurredAtUtc = clock.GetUtcNow(), ActorId = security.CurrentUserId,
        ActorUsername = security.CurrentUsername, TraceId = HttpContext.TraceIdentifier,
        Action = (position ? "positions." : "departments.") + action, EntityName = position ? "Position" : "Department",
        EntityId = id.ToString(), Reason = AuditMasker.SanitizeFreeText(reason), Payload = "{}"
    }, token);

    private async Task<IActionResult> Write(bool position, Guid? id, OrganizationWriteRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Reason)) return Failure(400, "organization.invalid_input");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(token);
        var name = request.Name.Trim();
        PositionResponse result;
        if (position)
        {
            var item = id.HasValue ? await db.Positions.AsTracking().SingleOrDefaultAsync(p => p.Id == id, token) : new Position { Id = Guid.NewGuid(), CreatedAtUtc = clock.GetUtcNow() };
            if (item is null) return Failure(404, "organization.not_found");
            if (id.HasValue && item.Name != request.ExpectedName) return Failure(409, "resource.conflict");
            if (await db.Positions.AnyAsync(p => p.Name == name && p.Id != item.Id, token)) return Failure(409, "organization.duplicate_name");
            item.Name = name;
            if (!id.HasValue) db.Positions.Add(item);
            result = new(item.Id, item.Name, item.CreatedAtUtc);
        }
        else
        {
            var item = id.HasValue ? await db.Departments.AsTracking().SingleOrDefaultAsync(p => p.Id == id, token) : Department.Create(name, clock.GetUtcNow());
            if (item is null) return Failure(404, "organization.not_found");
            if (id.HasValue && item.Name != request.ExpectedName) return Failure(409, "resource.conflict");
            if (await db.Departments.AnyAsync(p => p.Name == name && p.Id != item.Id, token)) return Failure(409, "organization.duplicate_name");
            item.Rename(name);
            if (!id.HasValue) db.Departments.Add(item);
            result = new(item.Id, item.Name, item.CreatedAtUtc);
        }
        await db.SaveChangesAsync(token);
        await Audit(position, result.Id, id.HasValue ? "updated" : "created", request.Reason, token);
        await tx.CommitAsync(token);
        return id.HasValue ? Ok(result) : Created($"/api/v1/{(position ? "positions" : "departments")}", result);
    }

    private async Task<IActionResult> Delete(bool position, Guid id, OrganizationDeleteRequest request, CancellationToken token)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token);
        await LockAsync(token);
        if (await db.EmployeeProfiles.AnyAsync(e => position ? e.PositionId == id : e.DepartmentId == id, token)) return Failure(409, "organization.in_use");
        if (position)
        {
            var item = await db.Positions.SingleOrDefaultAsync(p => p.Id == id, token);
            if (item is null) return Failure(404, "organization.not_found");
            if (item.Name != request.ExpectedName) return Failure(409, "resource.conflict");
            db.Positions.Remove(item);
        }
        else
        {
            var item = await db.Departments.SingleOrDefaultAsync(p => p.Id == id, token);
            if (item is null) return Failure(404, "organization.not_found");
            if (item.Name != request.ExpectedName) return Failure(409, "resource.conflict");
            db.Departments.Remove(item);
        }
        await db.SaveChangesAsync(token);
        await Audit(position, id, "deleted", request.Reason, token);
        await tx.CommitAsync(token);
        return NoContent();
    }

    private Task LockAsync(CancellationToken token) => db.Database.ExecuteSqlRawAsync(
        "DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'HRM.Organization.Configuration', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000; IF @r < 0 THROW 51017, 'Organization configuration busy.', 1;", token);
}
