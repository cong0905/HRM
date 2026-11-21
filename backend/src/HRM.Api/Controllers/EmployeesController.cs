using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using HRM.Next.Infrastructure.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController(
    ISecurityContext security,
    HrmDbContext db,
    IAuditRepository auditRepo,
    TimeProvider clock,
    UserManager<HrmUser> userManager,
    AccountService accountService,
    IAccountMessageSender sender,
    RoleManager<IdentityRole<Guid>> roleManager) : ControllerBase
{
    private ObjectResult ProblemResult(int status, string code) =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    private static EmployeeProfileResponse MapToResponse(EmployeeProfile p, bool hasHrRead)
    {
        var rowVersionStr = p.RowVersion is { Length: > 0 } ? Convert.ToBase64String(p.RowVersion) : null;
        if (hasHrRead)
        {
            return new EmployeeProfileResponse(
                p.Id,
                p.UserId,
                p.FullName,
                p.CitizenId,
                p.Phone,
                p.Address,
                p.DepartmentId,
                p.EmployeeCode,
                p.Email,
                p.DateOfBirth,
                p.Gender,
                p.MaritalStatus,
                p.PositionId,
                p.HireDate,
                p.TerminationDate,
                p.Status,
                rowVersionStr);
        }

        return new EmployeeProfileResponse(
            p.Id,
            p.UserId,
            p.FullName,
            AuditMasker.MaskCitizenId(p.CitizenId),
            p.Phone,
            null,
            p.DepartmentId,
            p.EmployeeCode,
            p.Email,
            null,
            p.Gender,
            null,
            p.PositionId,
            p.HireDate,
            p.TerminationDate,
            p.Status,
            rowVersionStr);
    }

    [HttpGet(Name = "ListEmployees")]
    [Authorize]
    [ProducesResponseType<PagedResult<EmployeeProfileResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<EmployeeProfileResponse>>> List(
        CancellationToken token,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? name = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery, RegularExpression("^(DangLamViec|DaNghiViec)$")] string? status = null)
    {
        var hasHrRead = security.HasPermission(HrmPermissions.EmployeesRead);
        var hasPayrollRead = security.HasPermission(HrmPermissions.PayrollReadAll);

        if (!hasHrRead && !hasPayrollRead)
        {
            return ProblemResult(403, "authorization.forbidden");
        }

        IQueryable<EmployeeProfile> query = db.EmployeeProfiles.AsNoTracking();
        if (status is not null) query = query.Where(e => e.Status == status);

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(e => e.FullName.Contains(name.Trim()));
        }

        if (hasHrRead)
        {
            // Scoped strictly to HR user's assigned employees
            var assignedEmployeeIds = db.HrEmployeeAssignments
                .Where(a => a.HrUserId == security.CurrentUserId)
                .Select(a => a.EmployeeId);

            query = query.Where(e => assignedEmployeeIds.Contains(e.Id));
        }

        query = query.OrderBy(e => e.FullName).ThenBy(e => e.Id);

        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;
        var profiles = await query.Skip(offset).Take(pageSize).ToListAsync(token);

        var items = profiles.Select(p => MapToResponse(p, hasHrRead)).ToList();

        return Ok(new PagedResult<EmployeeProfileResponse>(items, page, pageSize, total));
    }

    [HttpPost("bulk", Name = "BulkGetEmployees")]
    [Authorize]
    [ProducesResponseType<List<EmployeeProfileResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<List<EmployeeProfileResponse>>> BulkGet(
        [FromBody] BulkGetEmployeesRequest request,
        CancellationToken token)
    {
        var hasHrRead = security.HasPermission(HrmPermissions.EmployeesRead);
        var hasPayrollRead = security.HasPermission(HrmPermissions.PayrollReadAll);

        if (!hasHrRead && !hasPayrollRead)
        {
            return ProblemResult(403, "authorization.forbidden");
        }

        if (request.EmployeeIds.Count > 100)
        {
            return ProblemResult(400, "employees.bulk_limit_exceeded");
        }

        var distinctIds = request.EmployeeIds.Distinct().ToList();

        if (hasHrRead)
        {
            // Must have scope for ALL requested employees; fail closed on mixed scope
            var assignedCount = await db.HrEmployeeAssignments
                .Where(a => a.HrUserId == security.CurrentUserId && distinctIds.Contains(a.EmployeeId))
                .CountAsync(token);

            if (assignedCount != distinctIds.Count)
            {
                return ProblemResult(403, "authorization.scope_forbidden");
            }
        }

        var profiles = await db.EmployeeProfiles.AsNoTracking()
            .Where(e => distinctIds.Contains(e.Id))
            .ToListAsync(token);

        var items = profiles.Select(p => MapToResponse(p, hasHrRead)).ToList();

        return Ok(items);
    }

    [HttpGet("{id:guid}", Name = "GetEmployeeById")]
    [Authorize]
    [ProducesResponseType<EmployeeProfileResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<EmployeeProfileResponse>> GetById(Guid id, CancellationToken token)
    {
        var hasHrRead = security.HasPermission(HrmPermissions.EmployeesRead);
        var hasPayrollRead = security.HasPermission(HrmPermissions.PayrollReadAll);
        var callerEmployeeId = await security.GetEmployeeIdAsync(token);
        var isSelf = callerEmployeeId.HasValue && callerEmployeeId.Value == id;

        if (!hasHrRead && !hasPayrollRead && !isSelf)
        {
            return ProblemResult(403, "authorization.forbidden");
        }

        var profile = await db.EmployeeProfiles.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == id, token);

        if (profile is null) return ProblemResult(404, "employee.not_found");

        if (isSelf)
        {
            return Ok(MapToResponse(profile, hasHrRead: true));
        }

        if (hasHrRead)
        {
            var isAssigned = await db.HrEmployeeAssignments
                .AnyAsync(a => a.HrUserId == security.CurrentUserId && a.EmployeeId == id, token);

            if (isAssigned)
            {
                return Ok(MapToResponse(profile, hasHrRead: true));
            }

            // Unassigned HR caller:
            // If they also hold Payroll permission, project compensation fields only; otherwise fail-closed with 403
            if (hasPayrollRead)
            {
                return Ok(MapToResponse(profile, hasHrRead: false));
            }

            return ProblemResult(403, "authorization.scope_forbidden");
        }

        // Caller has PayrollReadAll only
        return Ok(MapToResponse(profile, hasHrRead: false));
    }

    [HttpPost(Name = "CreateEmployee")]
    [Authorize(Policy = HrmPolicies.EmployeesManage)]
    [ProducesResponseType<CreateEmployeeResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<CreateEmployeeResponse>> Create([FromBody] CreateEmployeeRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.CitizenId) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Address) ||
            string.IsNullOrWhiteSpace(request.Reason))
        {
            return ProblemResult(400, "employees.validation_error");
        }

        // Validate chronology without introducing an unapproved minimum hiring age.
        if (request.DateOfBirth == DateOnly.MinValue || request.HireDate == DateOnly.MinValue ||
            request.DateOfBirth >= request.HireDate ||
            request.DateOfBirth > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime))
        {
            return ProblemResult(400, "employees.invalid_dates");
        }

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, token);
            if (!deptExists)
            {
                return ProblemResult(400, "employees.department_not_found");
            }
        }

        if (request.PositionId.HasValue && !await db.Positions.AnyAsync(p => p.Id == request.PositionId, token))
            return ProblemResult(400, "employees.position_not_found");
        var username = (request.Username ?? request.Email).Trim();
        if (await userManager.FindByNameAsync(username) is not null)
        {
            return ProblemResult(409, "identity.duplicate_username");
        }

        if (await userManager.FindByEmailAsync(request.Email.Trim()) is not null)
        {
            return ProblemResult(409, "identity.duplicate_email");
        }

        if (await db.EmployeeProfiles.AnyAsync(e => e.CitizenId == request.CitizenId.Trim(), token))
        {
            return ProblemResult(409, "employees.duplicate_citizen_id");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);

        var employeeCode = await db.GenerateEmployeeCodeAsync(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).Year, token);
        if (await db.EmployeeProfiles.AnyAsync(e => e.EmployeeCode == employeeCode, token))
        {
            return ProblemResult(409, "employees.duplicate_code");
        }

        var user = new HrmUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = request.Email.Trim(),
            IsActive = false // Identity inactive pending employee activation
        };

        var createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(400, "account.creation_failed");
        }

        // Fixed Employee role only; provisioning cannot grant administrative roles.
        if (!await roleManager.RoleExistsAsync(HrmRoles.Employee))
        {
            var bootstrapRole = await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = HrmRoles.Employee });
            if (!bootstrapRole.Succeeded) return ProblemResult(400, "account.role_assignment_failed");
        }
        var roleResult = await userManager.AddToRoleAsync(user, HrmRoles.Employee);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(400, "account.role_assignment_failed");
        }

        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            EmployeeCode = employeeCode,
            FullName = request.FullName.Trim(),
            CitizenId = request.CitizenId.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender.Trim(),
            MaritalStatus = request.MaritalStatus?.Trim(),
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            HireDate = request.HireDate,
            Status = "DangLamViec",
            CreatedAtUtc = clock.GetUtcNow()
        };

        db.EmployeeProfiles.Add(profile);

        // Activation outbox in same transaction
        var activationToken = await userManager.GenerateUserTokenAsync(user, HrmClaims.ActivationProvider, HrmClaims.ActivationPurpose);
        var deliveryStatus = "NotConfiguredPendingDelivery";

        var outboxMessage = new ActivationOutboxMessage
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = user.Email!,
            Purpose = "activation",
            Status = "Pending",
            CreatedAtUtc = clock.GetUtcNow(),
            DispatchedAtUtc = null
        };
        db.ActivationOutboxMessages.Add(outboxMessage);

        await db.SaveChangesAsync(token);

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "employees.created",
            EntityName = "EmployeeProfile",
            EntityId = profile.Id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request.Reason),
            Payload = AuditMasker.MaskObject(new
            {
                employeeId = profile.Id,
                userId = user.Id,
                employeeCode = profile.EmployeeCode,
                citizenId = profile.CitizenId,
                email = profile.Email,
                phone = profile.Phone,
                departmentId = profile.DepartmentId,
                hireDate = profile.HireDate
            })
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        // Never send message before transaction commit
        if (sender.Mode == "test-only")
        {
            try
            {
                await sender.SendAsync(new AccountMessage(user.Email!, user.Id, "activation", activationToken), token);
                outboxMessage.Status = "Dispatched";
                outboxMessage.DispatchedAtUtc = clock.GetUtcNow();
                await db.SaveChangesAsync(token);
                deliveryStatus = "TestDelivered";
            }
            catch (Exception) when (!token.IsCancellationRequested)
            {
                // Provisioning committed; delivery is separately retryable and must never be reported as sent.
                deliveryStatus = "DeliveryFailedPendingRetry";
            }
        }

        var responseProfile = MapToResponse(profile, hasHrRead: true);
        var response = new CreateEmployeeResponse(responseProfile, deliveryStatus, "InactivePendingActivation");

        return CreatedAtRoute("GetEmployeeById", new { id = profile.Id }, response);
    }

    [HttpPatch("{id:guid}", Name = "UpdateEmployee")]
    [Authorize(Policy = HrmPolicies.EmployeesManage)]
    [ProducesResponseType<EmployeeProfileResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<EmployeeProfileResponse>> Update(Guid id, [FromBody] UpdateEmployeeRequest request, CancellationToken token)
    {
        var profile = await db.EmployeeProfiles.AsTracking()
            .SingleOrDefaultAsync(e => e.Id == id, token);

        if (profile is null) return ProblemResult(404, "employee.not_found");

        // Enforce HR scope guard: caller must be assigned to manage this employee
        var isAssigned = await db.HrEmployeeAssignments
            .AnyAsync(a => a.HrUserId == security.CurrentUserId && a.EmployeeId == id, token);

        if (!isAssigned)
        {
            return ProblemResult(403, "authorization.scope_forbidden");
        }

        // Concurrency token validation
        if (string.IsNullOrWhiteSpace(request.RowVersion))
        {
            return ProblemResult(400, "concurrency.token_required");
        }

        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return ProblemResult(400, "concurrency.invalid_token");
        }

        if (profile.RowVersion is { Length: > 0 } && !expectedVersion.SequenceEqual(profile.RowVersion))
        {
            return ProblemResult(409, "resource.conflict");
        }

        // Offboarded employee cannot be modified
        if (profile.Status == "DaNghiViec")
        {
            return ProblemResult(409, "employees.already_terminated");
        }

        if ((request.IsSpecified(nameof(request.Phone)) && string.IsNullOrWhiteSpace(request.Phone)) ||
            (request.IsSpecified(nameof(request.Address)) && string.IsNullOrWhiteSpace(request.Address)))
            return ProblemResult(400, "employees.required_field_null");
        if (request.IsSpecified(nameof(request.PositionId)) && request.PositionId.HasValue &&
            !await db.Positions.AnyAsync(p => p.Id == request.PositionId, token))
            return ProblemResult(400, "employees.position_not_found");
        var nextBirth = request.IsSpecified(nameof(request.DateOfBirth)) ? request.DateOfBirth : profile.DateOfBirth;
        var nextHire = request.IsSpecified(nameof(request.HireDate)) ? request.HireDate : profile.HireDate;
        if ((request.IsSpecified(nameof(request.DateOfBirth)) && !nextBirth.HasValue) ||
            (request.IsSpecified(nameof(request.HireDate)) && !nextHire.HasValue) ||
            nextBirth == DateOnly.MinValue || nextHire == DateOnly.MinValue ||
            (nextBirth.HasValue && nextBirth > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime)) ||
            (nextBirth.HasValue && nextHire.HasValue && nextBirth >= nextHire))
            return ProblemResult(400, "employees.invalid_dates");

        await using var transaction = await db.Database.BeginTransactionAsync(token);

        var oldPhone = profile.Phone;
        var oldAddress = profile.Address;

        // Partial update: distinguish absent vs null vs value
        if (request.IsSpecified(nameof(request.FullName)))
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return ProblemResult(400, "employees.validation_error");
            }
            profile.FullName = request.FullName.Trim();
        }

        if (request.IsSpecified(nameof(request.Phone)))
        {
            profile.Phone = request.Phone?.Trim() ?? string.Empty;
        }

        if (request.IsSpecified(nameof(request.Address)))
        {
            profile.Address = request.Address?.Trim() ?? string.Empty;
        }

        if (request.IsSpecified(nameof(request.DateOfBirth)))
        {
            profile.DateOfBirth = request.DateOfBirth;
        }

        if (request.IsSpecified(nameof(request.Gender)))
        {
            profile.Gender = request.Gender?.Trim();
        }

        if (request.IsSpecified(nameof(request.MaritalStatus)))
        {
            profile.MaritalStatus = request.MaritalStatus?.Trim();
        }

        if (request.IsSpecified(nameof(request.DepartmentId)))
        {
            if (request.DepartmentId.HasValue)
            {
                var exists = await db.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value, token);
                if (!exists)
                {
                    return ProblemResult(400, "employees.department_not_found");
                }
            }
            profile.DepartmentId = request.DepartmentId;
        }

        if (request.IsSpecified(nameof(request.PositionId)))
        {
            profile.PositionId = request.PositionId;
        }

        if (request.IsSpecified(nameof(request.HireDate)))
        {
            profile.HireDate = request.HireDate;
        }

        profile.UpdatedAtUtc = clock.GetUtcNow();

        db.Entry(profile).Property(e => e.RowVersion).OriginalValue = expectedVersion;

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(409, "resource.conflict");
        }

        var payload = AuditMasker.MaskObject(new
        {
            citizenId = profile.CitizenId,
            oldPhone,
            newPhone = profile.Phone,
            oldAddress,
            newAddress = profile.Address
        });

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "employees.updated",
            EntityName = "EmployeeProfile",
            EntityId = id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request.Reason),
            Payload = payload
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        return Ok(MapToResponse(profile, hasHrRead: true));
    }

    [HttpPost("{id:guid}/offboard", Name = "OffboardEmployee")]
    [Authorize(Policy = HrmPolicies.EmployeesManage)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<IActionResult> Offboard(Guid id, [FromBody] OffboardEmployeeRequest request, CancellationToken token)
    {
        var profile = await db.EmployeeProfiles.AsTracking()
            .SingleOrDefaultAsync(e => e.Id == id, token);

        if (profile is null) return ProblemResult(404, "employee.not_found");

        // Scope guard: caller must be assigned to manage this employee
        var isAssigned = await db.HrEmployeeAssignments
            .AnyAsync(a => a.HrUserId == security.CurrentUserId && a.EmployeeId == id, token);

        if (!isAssigned)
        {
            return ProblemResult(403, "authorization.scope_forbidden");
        }

        if (string.IsNullOrWhiteSpace(request.RowVersion))
        {
            return ProblemResult(400, "concurrency.token_required");
        }

        byte[] expectedVersion;
        try
        {
            expectedVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch (FormatException)
        {
            return ProblemResult(400, "concurrency.invalid_token");
        }

        if (profile.RowVersion is { Length: > 0 } && !expectedVersion.SequenceEqual(profile.RowVersion))
        {
            return ProblemResult(409, "resource.conflict");
        }

        if (profile.Status == "DaNghiViec")
        {
            return ProblemResult(409, "employees.already_offboarded");
        }

        if (request.TerminationDate == DateOnly.MinValue ||
            request.TerminationDate > DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime) ||
            (profile.HireDate.HasValue && request.TerminationDate < profile.HireDate.Value))
        {
            return ProblemResult(422, "employees.invalid_termination_date");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await AttendanceLocks.EmployeeAsync(db, profile.Id, token);
        // Same user-before-profile row lock order as login, refresh and attendance writes.
        _ = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={profile.UserId}")
            .AsTracking().SingleOrDefaultAsync(token);

        profile.Status = "DaNghiViec";
        profile.TerminationDate = request.TerminationDate;
        profile.UpdatedAtUtc = clock.GetUtcNow();

        db.Entry(profile).Property(e => e.RowVersion).OriginalValue = expectedVersion;

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(409, "resource.conflict");
        }

        // Revoke all sessions, update security stamp, disable user in SAME transaction
        var revoked = await accountService.RevokeAllAsync(profile.UserId, disable: true, token);
        if (!revoked)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(500, "account.offboard_failed");
        }

        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = clock.GetUtcNow(),
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Action = "employees.offboarded",
            EntityName = "EmployeeProfile",
            EntityId = id.ToString(),
            Reason = AuditMasker.SanitizeFreeText(request.Reason),
            Payload = AuditMasker.MaskObject(new
            {
                employeeId = profile.Id,
                userId = profile.UserId,
                terminationDate = request.TerminationDate,
                newStatus = "DaNghiViec"
            })
        };

        await auditRepo.RecordAsync(auditEvent, token);
        await transaction.CommitAsync(token);

        return NoContent();
    }
}
