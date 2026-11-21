using System.ComponentModel.DataAnnotations;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/leave-requests")]
public sealed class LeaveRequestsController(ILeaveService leaveService) : ControllerBase
{
    private ObjectResult ProblemResult(int status, string code) =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpPost(Name = "CreateLeaveRequest")]
    [Authorize]
    [ProducesResponseType<LeaveRecordResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<LeaveRecordResponse>> Create(
        [FromBody] CreateLeaveRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken token)
    {
        var response = await leaveService.CreateRequestAsync(request, idempotencyKey, token);
        return CreatedAtRoute("GetLeaveRequestById", new { id = response.Id }, response);
    }

    [HttpGet("{id:guid}", Name = "GetLeaveRequestById")]
    [Authorize]
    [ProducesResponseType<LeaveRecordResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<LeaveRecordResponse>> GetById(Guid id, CancellationToken token)
    {
        var response = await leaveService.GetRequestByIdAsync(id, token);
        return Ok(response);
    }

    [HttpGet(Name = "ListLeaveRequests")]
    [Authorize]
    [ProducesResponseType<PagedResult<LeaveRecordResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<LeaveRecordResponse>>> List(
        CancellationToken token,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] int? year = null,
        [FromQuery] string? status = null,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var result = await leaveService.ListRequestsAsync(employeeId, year, status, page, pageSize, token);
        return Ok(result);
    }

    [HttpPost("{id:guid}/approve", Name = "ApproveLeaveRequest")]
    [Authorize(Policy = HrmPolicies.LeavesApprove)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveLeaveRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken token)
    {
        await leaveService.ApproveRequestAsync(id, request, idempotencyKey, token);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject", Name = "RejectLeaveRequest")]
    [Authorize(Policy = HrmPolicies.LeavesApprove)]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectLeaveRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken token)
    {
        await leaveService.RejectRequestAsync(id, request, idempotencyKey, token);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel", Name = "CancelLeaveRequest")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromBody] CancelLeaveRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken token)
    {
        await leaveService.CancelRequestAsync(id, request, idempotencyKey, token);
        return NoContent();
    }
}
