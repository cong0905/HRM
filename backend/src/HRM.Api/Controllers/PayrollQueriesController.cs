using System.ComponentModel.DataAnnotations;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Payroll;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/payroll-periods")]
public sealed class PayrollQueriesController(HrmDbContext db) : ControllerBase
{
    private static readonly string[] AllowedStatuses = ["Draft", "Calculated", "Approved", "Published", "Locked"];

    private ObjectResult ProblemResult(int status, string code, string? title = null) =>
        new(ApiProblems.Create(HttpContext, status, code, title)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpGet(Name = "ListPayrollPeriods")]
    [Authorize(Policy = HrmPolicies.PayrollRead)]
    [ProducesResponseType<PagedResult<PayrollWorkflowState>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<PayrollWorkflowState>>> ListPayrollPeriods(
        CancellationToken token,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery, Range(2000, 2100)] int? year = null,
        [FromQuery] string? status = null)
    {
        if ((long)(page - 1) * pageSize > int.MaxValue)
            return ProblemResult(400, "pagination.invalid", "Offset vượt quá giới hạn.");

        string? normalizedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            var trimmed = status.Trim();
            normalizedStatus = AllowedStatuses.FirstOrDefault(s => string.Equals(s, trimmed, StringComparison.OrdinalIgnoreCase));
            if (normalizedStatus is null)
                return ProblemResult(400, "payroll.invalid_status_filter", "Trạng thái kỳ lương lọc không hợp lệ. Cho phép: Draft, Calculated, Approved, Published, Locked.");
        }

        var query = db.PayrollPeriods.AsNoTracking();

        if (year.HasValue)
        {
            var prefix = $"{year.Value:D4}-";
            query = query.Where(p => p.PeriodCode.StartsWith(prefix));
        }

        if (normalizedStatus is not null)
        {
            query = query.Where(p => p.Status == normalizedStatus);
        }

        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var periods = await query
            .OrderByDescending(p => p.PeriodCode)
            .ThenBy(p => p.Id)
            .Skip(offset)
            .Take(pageSize)
            .ToListAsync(token);

        var pageIds = periods.Select(p => p.Id).ToList();
        var versionMap = pageIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await db.PayrollCalculationBatches.AsNoTracking()
                .Where(b => pageIds.Contains(b.PeriodId))
                .GroupBy(b => b.PeriodId)
                .Select(g => new { PeriodId = g.Key, MaxVersion = g.Max(b => (int?)b.Version) ?? 0 })
                .ToDictionaryAsync(x => x.PeriodId, x => x.MaxVersion, token);

        var items = periods.Select(p => new PayrollWorkflowState(
            p.Id,
            p.PeriodCode,
            p.Status,
            p.RowVersion != null && p.RowVersion.Length > 0 ? Convert.ToBase64String(p.RowVersion) : string.Empty,
            versionMap.GetValueOrDefault(p.Id, 0),
            p.ActiveBatchId,
            p.ApprovedBatchId,
            p.PublishedBatchId,
            p.CalculatedBy,
            p.ApprovedBy,
            p.PublishedBy,
            p.LockedAtUtc)).ToList();

        return Ok(new PagedResult<PayrollWorkflowState>(items, page, pageSize, total));
    }
}
