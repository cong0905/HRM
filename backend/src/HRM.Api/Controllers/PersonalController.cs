using System.ComponentModel.DataAnnotations;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Application.Payroll;
using HRM.Next.Application.Personnel;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class PersonalController(ISecurityContext security, HrmDbContext db) : ControllerBase
{
    private ObjectResult ProblemResult(int status, string code) =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpGet("profile", Name = "GetMyProfile")]
    [ProducesResponseType<EmployeeProfileResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<EmployeeProfileResponse>> GetProfile(CancellationToken token)
    {
        var userId = security.CurrentUserId;
        if (!userId.HasValue) return ProblemResult(401, "auth.invalid");

        var profile = await db.EmployeeProfiles.AsNoTracking()
            .SingleOrDefaultAsync(e => e.UserId == userId.Value, token);

        if (profile is null) return ProblemResult(404, "employee.not_found");

        return Ok(new EmployeeProfileResponse(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.CitizenId,
            profile.Phone,
            profile.Address,
            profile.DepartmentId,
            profile.EmployeeCode,
            profile.Email,
            profile.DateOfBirth,
            profile.Gender,
            profile.MaritalStatus,
            profile.PositionId,
            profile.HireDate,
            profile.TerminationDate,
            profile.Status,
            profile.RowVersion.Length > 0 ? Convert.ToBase64String(profile.RowVersion) : null));
    }

    [HttpGet("leave-requests", Name = "GetMyLeaveRequests")]
    [ProducesResponseType<PagedResult<LeaveRecordResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<LeaveRecordResponse>>> GetLeaveRequests(
        CancellationToken token,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue) return ProblemResult(403, "authorization.employee_link_required");

        var query = db.LeaveRecords.AsNoTracking().Where(l => l.EmployeeId == employeeId.Value);
        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var list = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Select(l => new LeaveRecordResponse(
                l.Id,
                l.EmployeeId,
                l.StartDate,
                l.EndDate,
                l.Reason,
                l.Status,
                l.ApproverId,
                l.CreatedAtUtc,
                l.WorkingDays,
                l.RowVersion.Length > 0 ? Convert.ToBase64String(l.RowVersion) : null,
                l.DecisionReason,
                l.DecidedAtUtc,
                l.CancelledAtUtc,
                l.CancellationReason))
            .ToListAsync(token);

        return Ok(new PagedResult<LeaveRecordResponse>(list, page, pageSize, total));
    }

    [HttpGet("leave-balances", Name = "GetMyLeaveBalances")]
    [ProducesResponseType<IReadOnlyList<LeaveBalanceResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceResponse>>> GetLeaveBalances(
        CancellationToken token,
        [FromQuery] int? year = null)
    {
        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue) return ProblemResult(403, "authorization.employee_link_required");

        var query = db.LeaveBalances.AsNoTracking().Where(b => b.EmployeeId == employeeId.Value);
        if (year.HasValue) query = query.Where(b => b.Year == year.Value);

        var list = await query
            .OrderByDescending(b => b.Year)
            .Select(b => new LeaveBalanceResponse(
                b.Id,
                b.EmployeeId,
                b.Year,
                b.AllocatedDays,
                b.UsedDays,
                b.RemainingDays,
                b.UpdatedAtUtc,
                b.RowVersion.Length > 0 ? Convert.ToBase64String(b.RowVersion) : string.Empty))
            .ToListAsync(token);

        return Ok(list);
    }

    [HttpGet("payslips", Name = "GetMyPayslips")]
    [ProducesResponseType<PagedResult<PayslipResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<PayslipResponse>>> GetPayslips(
        CancellationToken token,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue) return ProblemResult(403, "authorization.employee_link_required");

        var query = db.Payslips.AsNoTracking().Where(p => p.EmployeeId == employeeId.Value && p.Status == "Published");
        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var list = await query
            .OrderByDescending(p => p.PublishedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Select(p => new PayslipResponse(
                p.Id,
                p.PeriodId,
                p.EmployeeId,
                p.Amount,
                p.Status,
                p.PublishedAtUtc))
            .ToListAsync(token);

        return Ok(new PagedResult<PayslipResponse>(list, page, pageSize, total));
    }
}

