using System.ComponentModel.DataAnnotations;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/leave-balances")]
public sealed class LeaveBalancesController(ILeaveService leaveService) : ControllerBase
{
    [HttpPost("grant", Name = "GrantLeaveBalance")]
    [Authorize(Policy = HrmPolicies.LeavesApprove)]
    [ProducesResponseType<LeaveBalanceResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<LeaveBalanceResponse>> Grant(
        [FromBody] GrantLeaveBalanceRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken token)
    {
        var response = await leaveService.GrantBalanceAsync(request, idempotencyKey, token);
        return CreatedAtAction(nameof(GetBalances), new { employeeId = response.EmployeeId, year = response.Year }, response);
    }

    [HttpGet(Name = "GetLeaveBalances")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<LeaveBalanceResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<IReadOnlyList<LeaveBalanceResponse>>> GetBalances(
        CancellationToken token,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] int? year = null)
    {
        var result = await leaveService.GetBalancesAsync(employeeId, year, token);
        return Ok(result);
    }

    [HttpGet("ledger", Name = "GetLeaveLedger")]
    [Authorize]
    [ProducesResponseType<PagedResult<LeaveLedgerEntryResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<LeaveLedgerEntryResponse>>> GetLedger(
        CancellationToken token,
        [FromQuery, Required] Guid employeeId,
        [FromQuery] int? year = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var result = await leaveService.GetLedgerEntriesAsync(employeeId, year, page, pageSize, token);
        return Ok(result);
    }
}
