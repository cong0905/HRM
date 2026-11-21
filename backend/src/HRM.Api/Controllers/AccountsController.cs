using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Api.Controllers;

public sealed record AssignRolesRequest(
    [Required] List<string> Roles,
    [Required, StringLength(500)] string Reason);

public sealed record DisableAccountRequest(
    [Required, StringLength(500)] string Reason);

public sealed record AccountResponse(Guid Id, string Username, bool IsActive, List<string> Roles);

public sealed record AssignScopesRequest(
    [Required] List<Guid> EmployeeIds,
    [Required, StringLength(500)] string Reason);

public sealed record RevokeScopeRequest(
    [StringLength(500)] string? Reason);

public sealed record HrScopeAssignmentResponse(
    Guid Id,
    Guid HrUserId,
    Guid EmployeeId,
    DateTimeOffset AssignedAtUtc,
    Guid? AssignedBy);

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(
    ISecurityContext security,
    UserManager<HrmUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    AccountService accountService,
    IAuditRepository auditRepo,
    HrmDbContext db,
    TimeProvider clock) : ControllerBase
{
    private ObjectResult ProblemResult(int status, string code) =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpGet(Name = "ListAccounts"), Authorize(Policy = HrmPolicies.AccountsManage)]
    [ProducesResponseType<PagedResult<AccountResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<AccountResponse>>> List(CancellationToken token,
        [Range(1, int.MaxValue)] int page = 1, [Range(1, 100)] int pageSize = 20, string? username = null)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(username)) query = query.Where(u => u.UserName!.Contains(username.Trim()));
        var total = await query.LongCountAsync(token);
        var users = await query.OrderBy(u => u.UserName).ThenBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(token);
        var ids = users.Select(u => u.Id).ToArray();
        var assignments = await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId) select new { ur.UserId, r.Name }).ToListAsync(token);
        return Ok(new PagedResult<AccountResponse>(users.Select(u => new AccountResponse(u.Id, u.UserName ?? "", u.IsActive,
            assignments.Where(r => r.UserId == u.Id).Select(r => r.Name!).Order().ToList())).ToList(), page, pageSize, total));
    }

    [HttpPut("{id:guid}/roles", Name = "AssignAccountRoles")]
    [Authorize(Policy = HrmPolicies.AccountsAssignRoles)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesRequest request, CancellationToken token)
    {
        // Anti-self-escalation: Caller cannot alter their own roles
        if (id == security.CurrentUserId)
        {
            return ProblemResult(403, "accounts.cannot_modify_own_roles");
        }

        foreach (var role in request.Roles)
        {
            if (!HrmRoles.IsValid(role))
                return ProblemResult(400, "accounts.invalid_role");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return ProblemResult(404, "accounts.user_not_found");

        // Ensure roles exist in AspNetRoles
        foreach (var role in request.Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(request.Roles, StringComparer.OrdinalIgnoreCase);
        var toAdd = request.Roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase);

        var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
        if (!removeResult.Succeeded) return ProblemResult(400, "accounts.role_update_failed");

        var addResult = await userManager.AddToRolesAsync(user, toAdd);
        if (!addResult.Succeeded) return ProblemResult(400, "accounts.role_update_failed");

        // SLA T00 Session Revocation: update security stamp and revoke all existing sessions
        await userManager.UpdateSecurityStampAsync(user);
        await accountService.RevokeAllAsync(user.Id, disable: false, token);

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "accounts.roles_assigned",
            EntityName = "HrmUser",
            EntityId = id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request.Reason),
            Payload = AuditMasker.MaskObject(new { targetUserId = id, oldRoles = currentRoles, newRoles = request.Roles })
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        return NoContent();
    }

    [HttpPost("{id:guid}/disable", Name = "DisableAccount")]
    [Authorize(Policy = HrmPolicies.AccountsManage)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Disable(Guid id, [FromBody] DisableAccountRequest request, CancellationToken token)
    {
        // Anti-self-disable: Caller cannot disable their own account
        if (id == security.CurrentUserId)
        {
            return ProblemResult(403, "accounts.cannot_disable_self");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return ProblemResult(404, "accounts.user_not_found");

        var revoked = await accountService.RevokeAllAsync(id, disable: true, token);
        if (!revoked) return ProblemResult(400, "accounts.disable_failed");

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "accounts.disabled",
            EntityName = "HrmUser",
            EntityId = id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request.Reason),
            Payload = AuditMasker.MaskObject(new { targetUserId = id, disabled = true })
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        return NoContent();
    }

    [HttpGet("{id:guid}/scopes", Name = "GetAccountScopes")]
    [Authorize(Policy = HrmPolicies.HrScopeManage)]
    [ProducesResponseType<List<HrScopeAssignmentResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<List<HrScopeAssignmentResponse>>> GetScopes(Guid id, CancellationToken token)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == id, token);
        if (!userExists) return ProblemResult(404, "accounts.user_not_found");

        var scopes = await db.HrEmployeeAssignments.AsNoTracking()
            .Where(a => a.HrUserId == id)
            .OrderBy(a => a.AssignedAtUtc)
            .Select(a => new HrScopeAssignmentResponse(a.Id, a.HrUserId, a.EmployeeId, a.AssignedAtUtc, a.AssignedBy))
            .ToListAsync(token);

        return Ok(scopes);
    }

    [HttpPost("{id:guid}/scopes", Name = "AssignAccountScopes")]
    [Authorize(Policy = HrmPolicies.HrScopeManage)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> AssignScopes(Guid id, [FromBody] AssignScopesRequest request, CancellationToken token)
    {
        // Anti-self-scope-escalation: Caller cannot assign scopes to themselves
        if (id == security.CurrentUserId)
        {
            return ProblemResult(403, "accounts.cannot_modify_own_scope");
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return ProblemResult(404, "accounts.user_not_found");

        if (request.EmployeeIds == null || request.EmployeeIds.Count == 0)
        {
            return ProblemResult(400, "accounts.empty_scope_list");
        }

        var distinctEmployeeIds = request.EmployeeIds.Distinct().ToList();

        // Validate all target employees exist
        var existingCount = await db.EmployeeProfiles.CountAsync(e => distinctEmployeeIds.Contains(e.Id), token);
        if (existingCount != distinctEmployeeIds.Count)
        {
            return ProblemResult(400, "accounts.invalid_employee_in_scope");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);

        var existingAssignments = await db.HrEmployeeAssignments
            .Where(a => a.HrUserId == id && distinctEmployeeIds.Contains(a.EmployeeId))
            .Select(a => a.EmployeeId)
            .ToListAsync(token);

        var now = clock.GetUtcNow();
        var added = new List<Guid>();
        foreach (var empId in distinctEmployeeIds)
        {
            if (!existingAssignments.Contains(empId))
            {
                db.HrEmployeeAssignments.Add(new HrEmployeeAssignment
                {
                    Id = Guid.NewGuid(),
                    HrUserId = id,
                    EmployeeId = empId,
                    AssignedAtUtc = now,
                    AssignedBy = security.CurrentUserId
                });
                added.Add(empId);
            }
        }

        await db.SaveChangesAsync(token);

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "accounts.scopes_assigned",
            EntityName = "HrEmployeeAssignment",
            EntityId = id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request.Reason),
            Payload = AuditMasker.MaskObject(new { targetHrUserId = id, assignedEmployeeIds = added })
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        return NoContent();
    }

    [HttpDelete("{id:guid}/scopes/{employeeId:guid}", Name = "RevokeAccountScope")]
    [Authorize(Policy = HrmPolicies.HrScopeManage)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> RevokeScope(Guid id, Guid employeeId, [FromBody] RevokeScopeRequest? request, CancellationToken token)
    {
        // Anti-self-scope-escalation: Caller cannot modify their own scopes
        if (id == security.CurrentUserId)
        {
            return ProblemResult(403, "accounts.cannot_modify_own_scope");
        }

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return ProblemResult(404, "accounts.user_not_found");

        await using var transaction = await db.Database.BeginTransactionAsync(token);

        var assignment = await db.HrEmployeeAssignments
            .SingleOrDefaultAsync(a => a.HrUserId == id && a.EmployeeId == employeeId, token);

        if (assignment is null) return ProblemResult(404, "accounts.scope_not_found");

        db.HrEmployeeAssignments.Remove(assignment);
        await db.SaveChangesAsync(token);

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "accounts.scopes_revoked",
            EntityName = "HrEmployeeAssignment",
            EntityId = id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request?.Reason ?? "Revoked employee scope"),
            Payload = AuditMasker.MaskObject(new { targetHrUserId = id, revokedEmployeeId = employeeId })
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        return NoContent();
    }
}

