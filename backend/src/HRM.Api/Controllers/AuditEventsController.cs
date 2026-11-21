using System.ComponentModel.DataAnnotations;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/audit-events")]
[Authorize(Policy = HrmPolicies.AuditRead)]
public sealed class AuditEventsController(IAuditRepository auditRepo) : ControllerBase
{
    [HttpGet(Name = "ListAuditEvents")]
    [ProducesResponseType<PagedResult<AuditEventResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<AuditEventResponse>>> List(
        CancellationToken cancellationToken,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] Guid? actorId = null,
        [FromQuery] DateTimeOffset? fromUtc = null,
        [FromQuery] DateTimeOffset? toUtc = null,
        [FromQuery, RegularExpression("^(occurredAt|-occurredAt)$")] string sort = "-occurredAt")
    {
        var parameters = new AuditQueryParameters(
            Page: page,
            PageSize: pageSize,
            Action: action,
            ActorId: actorId,
            FromUtc: fromUtc,
            ToUtc: toUtc,
            Sort: sort);

        var result = await auditRepo.ListAsync(parameters, cancellationToken);
        return Ok(result);
    }
}

