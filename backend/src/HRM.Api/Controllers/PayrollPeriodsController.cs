using HRM.Next.Infrastructure.Payroll;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain.Audit;
using HRM.Next.Infrastructure.Persistence;
using HRM.Next.Infrastructure.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/payroll-periods")]
public sealed class PayrollPeriodsController(
    ISecurityContext security,
    HrmDbContext db,
    IAuditRepository auditRepo,
    TimeProvider clock,
    IWorkCalendarService calendars,
    IPayrollCalculationService payroll,
    IPayrollWorkflowService workflow,
    IPayrollReadService reads) : ControllerBase
{
    private ObjectResult ProblemResult(int status, string code) =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpGet("{id:guid}/items", Name = "GetPayrollPeriodItems")]
    [Authorize(Policy = HrmPolicies.PayrollRead)]
    [ProducesResponseType<PagedResult<PayslipResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<PagedResult<PayslipResponse>>> GetItems(
        Guid id,
        CancellationToken token,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var periodExists = await db.PayrollPeriods.AnyAsync(p => p.Id == id, token);
        if (!periodExists) return ProblemResult(404, "payroll.period_not_found");

        var query = db.Payslips.AsNoTracking().Where(p => p.PeriodId == id);
        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var items = await query
            .OrderBy(p => p.EmployeeId)
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

        return Ok(new PagedResult<PayslipResponse>(items, page, pageSize, total));
    }

    [HttpPost("{id:guid}/calculate", Name = "CalculatePayrollPeriod")]
    [Authorize(Policy = HrmPolicies.PayrollCalculate)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Calculate(Guid id, [FromBody] PayrollActionRequest request, CancellationToken token, [FromHeader(Name="Idempotency-Key")] string? key=null)
    {
        if (key is not null || await payroll.RequiresSnapshotsAsync(id, token))
        {
            await workflow.ExecuteAsync(id, "calculate", new PayrollWorkflowRequest(request.Reason, request.RowVersion ?? "", request.RunVersion ?? -1), key ?? "", token);
            return NoContent();
        }
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await payroll.RequiresSnapshotsAsync(id, token))
            await PayrollDataLocks.SourceAsync(db, true, token);

        var period = await db.PayrollPeriods.AsTracking().SingleOrDefaultAsync(p => p.Id == id, token);
        if (period is null) return ProblemResult(404, "payroll.period_not_found");

        // Immutable status invariant: Published or Locked period cannot be recalculated
        if (period.Status is "Published" or "Locked")
        {
            return ProblemResult(409, "payroll.period_immutable");
        }
        if (period.Status is "Approved")
        {
            return ProblemResult(409, "payroll.period_already_approved");
        }

        var now = clock.GetUtcNow();
        period.Status = "Calculated";
        period.CalculatedBy = security.CurrentUserId;
        period.CalculatedAtUtc = now;

        try
        {
            if (await payroll.RequiresSnapshotsAsync(period.Id, token))
                await payroll.CaptureAsync(period.Id, new CapturePayrollRequest(request.Reason), null, token);
            await calendars.CaptureForCalculationAsync(period.Id, period.PeriodCode, token);
            await db.SaveChangesAsync(token);

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = now,
                ActorId = security.CurrentUserId,
                ActorUsername = security.CurrentUsername,
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Action = "payroll.calculated",
                EntityName = "PayrollPeriod",
                EntityId = id.ToString(),
                Reason = AuditMasker.SanitizeFreeText(request.Reason),
                Payload = AuditMasker.MaskObject(new { periodId = id, status = "Calculated", calculatedBy = security.CurrentUserId })
            };

            await auditRepo.RecordAsync(auditEvent, token);
            await transaction.CommitAsync(token);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(409, "payroll.concurrency_conflict");
        }
    }

    [HttpPost("{id:guid}/approve", Name = "ApprovePayrollPeriod")]
    [Authorize(Policy = HrmPolicies.PayrollApprove)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] PayrollActionRequest request, CancellationToken token, [FromHeader(Name="Idempotency-Key")] string? key=null)
    {
        if (key is not null || await payroll.RequiresSnapshotsAsync(id, token))
        {
            await workflow.ExecuteAsync(id, "approve", new PayrollWorkflowRequest(request.Reason, request.RowVersion ?? "", request.RunVersion ?? -1), key ?? "", token);
            return NoContent();
        }
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await payroll.RequiresSnapshotsAsync(id, token))
            await PayrollDataLocks.SourceAsync(db, true, token);

        var period = await db.PayrollPeriods.AsTracking().SingleOrDefaultAsync(p => p.Id == id, token);
        if (period is null) return ProblemResult(404, "payroll.period_not_found");

        if (period.Status is "Published" or "Locked")
        {
            return ProblemResult(409, "payroll.period_immutable");
        }
        if (period.Status is "Approved")
        {
            return ProblemResult(409, "payroll.period_already_approved");
        }
        if (period.Status is not "Calculated")
        {
            return ProblemResult(409, "payroll.period_not_calculated");
        }

        // Fail-closed guard: missing calculation metadata cannot be approved
        if (!period.CalculatedBy.HasValue)
        {
            return ProblemResult(409, "payroll.missing_calculation_metadata");
        }

        // Separation of duties / Anti-self-approval: Calculator cannot approve own calculation
        if (period.CalculatedBy.Value == security.CurrentUserId)
        {
            return ProblemResult(403, "payroll.cannot_self_approve");
        }

        await payroll.EnsurePublishableAsync(period.Id, token);

        var now = clock.GetUtcNow();
        period.Status = "Approved";
        period.ApprovedBy = security.CurrentUserId;
        period.ApprovedAtUtc = now;

        try
        {
            await db.SaveChangesAsync(token);

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = now,
                ActorId = security.CurrentUserId,
                ActorUsername = security.CurrentUsername,
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Action = "payroll.approved",
                EntityName = "PayrollPeriod",
                EntityId = id.ToString(),
                Reason = AuditMasker.SanitizeFreeText(request.Reason),
                Payload = AuditMasker.MaskObject(new { periodId = id, status = "Approved", approvedBy = security.CurrentUserId })
            };

            await auditRepo.RecordAsync(auditEvent, token);
            await transaction.CommitAsync(token);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(409, "payroll.concurrency_conflict");
        }
    }

    [HttpPost("{id:guid}/publish", Name = "PublishPayrollPeriod")]
    [Authorize(Policy = HrmPolicies.PayrollPublish)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PayrollActionRequest request, CancellationToken token, [FromHeader(Name="Idempotency-Key")] string? key=null)
    {
        if (key is not null || await payroll.RequiresSnapshotsAsync(id, token))
        {
            await workflow.ExecuteAsync(id, "publish", new PayrollWorkflowRequest(request.Reason, request.RowVersion ?? "", request.RunVersion ?? -1), key ?? "", token);
            return NoContent();
        }
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        if (await payroll.RequiresSnapshotsAsync(id, token))
            await PayrollDataLocks.SourceAsync(db, true, token);

        var period = await db.PayrollPeriods.AsTracking().SingleOrDefaultAsync(p => p.Id == id, token);
        if (period is null) return ProblemResult(404, "payroll.period_not_found");
        await AttendanceLocks.PublicationAsync(db, period.PeriodCode, token);

        if (period.Status is "Published" or "Locked")
        {
            return ProblemResult(409, "payroll.period_already_published");
        }
        if (period.Status is not "Approved")
        {
            return ProblemResult(409, "payroll.period_not_approved");
        }

        // Fail-closed guard: missing calculation or approval metadata cannot be published
        if (!period.CalculatedBy.HasValue || !period.ApprovedBy.HasValue)
        {
            return ProblemResult(409, "payroll.missing_approval_metadata");
        }

        // Separation of duties: Calculator cannot publish
        if (period.CalculatedBy.Value == security.CurrentUserId)
        {
            return ProblemResult(403, "payroll.calculator_cannot_publish");
        }

        await payroll.EnsurePublishableAsync(period.Id, token);

        var now = clock.GetUtcNow();
        period.Status = "Published";
        period.PublishedBy = security.CurrentUserId;
        period.PublishedAtUtc = now;

        var payslips = await db.Payslips.AsTracking().Where(p => p.PeriodId == id).ToListAsync(token);
        foreach (var slip in payslips)
        {
            slip.Status = "Published";
            slip.PublishedAtUtc = now;
        }

        try
        {
            await db.SaveChangesAsync(token);

            var auditEvent = new AuditEvent
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = now,
                ActorId = security.CurrentUserId,
                ActorUsername = security.CurrentUsername,
                TraceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Action = "payroll.published",
                EntityName = "PayrollPeriod",
                EntityId = id.ToString(),
                Reason = AuditMasker.SanitizeFreeText(request.Reason),
                Payload = AuditMasker.MaskObject(new { periodId = id, publishedCount = payslips.Count, publishedBy = security.CurrentUserId })
            };

            await auditRepo.RecordAsync(auditEvent, token);
            await transaction.CommitAsync(token);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            return ProblemResult(409, "payroll.concurrency_conflict");
        }
    }

    [HttpGet("{id:guid}/export", Name = "ExportPayrollPeriod")]
    [Authorize(Policy = HrmPolicies.PayrollExport)]
    [Produces("text/csv")]
    [ProducesResponseType<FileContentResult>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Export(Guid id, CancellationToken token,
        [FromQuery, Range(1, 1000)] int maxRows = 500)
    {
        var file = await reads.ExportAsync(id, maxRows, token);
        return File(file.Content, "text/csv; charset=utf-8", file.FileName);
    }
}
