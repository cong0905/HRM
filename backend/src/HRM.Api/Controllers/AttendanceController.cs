using System.ComponentModel.DataAnnotations;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/attendance")]
public sealed class AttendanceController(IAttendanceService attendanceService) : ControllerBase
{
    private ObjectResult ProblemResult(int status, string code) =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    private ObjectResult Missing(string code) => ProblemResult(404, code);

    [HttpGet(Name = "ListAttendance")]
    [Authorize(Policy = HrmPolicies.AttendanceReadAll)]
    [ProducesResponseType<PagedResult<AttendanceDayResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<AttendanceDayResponse>>> List(CancellationToken token,
        [FromQuery] Guid? employeeId = null, [FromQuery] DateOnly? from = null, [FromQuery] DateOnly? to = null,
        [FromQuery] string? status = null, [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20) =>
        Ok(await attendanceService.ListAsync(employeeId, from, to, status, page, pageSize, token));

    [HttpGet("me/today", Name = "GetOwnTodayAttendance")]
    [Authorize]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<AttendanceDayResponse>> GetOwnToday(CancellationToken token)
    {
        var result = await attendanceService.GetOwnDayAsync(null, token);
        return result is null ? Missing("attendance.not_found") : Ok(result);
    }

    [HttpGet("me/days/{day}", Name = "GetOwnAttendanceDay")]
    [Authorize]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<AttendanceDayResponse>> GetOwnDay(DateOnly day, CancellationToken token)
    {
        var result = await attendanceService.GetOwnDayAsync(day, token);
        return result is null ? Missing("attendance.not_found") : Ok(result);
    }

    [HttpGet("me/history", Name = "GetOwnAttendanceHistory")]
    [Authorize]
    [ProducesResponseType<PagedResult<AttendanceDayResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PagedResult<AttendanceDayResponse>>> GetOwnHistory(
        CancellationToken token,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20)
    {
        var result = await attendanceService.GetOwnHistoryAsync(page, pageSize, token);
        return Ok(result);
    }

    [HttpGet("employees/{employeeId:guid}/days/{day}", Name = "GetEmployeeAttendanceDay")]
    [Authorize(Policy = HrmPolicies.AttendanceReadAll)]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<AttendanceDayResponse>> GetEmployeeDay(Guid employeeId, DateOnly day, CancellationToken token)
    {
        var result = await attendanceService.GetEmployeeDayAsync(employeeId, day, token);
        return result is null ? Missing("attendance.not_found") : Ok(result);
    }

    [HttpPost("me/check-in", Name = "CheckIn")]
    [Authorize]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<AttendanceDayResponse>> CheckIn(
        [FromBody] AttendanceCommandRequest? request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken token)
    {
        var result = await attendanceService.CheckInAsync(request ?? new AttendanceCommandRequest(), idempotencyKey ?? string.Empty, token);
        return Ok(result);
    }

    [HttpPost("me/check-out", Name = "CheckOut")]
    [Authorize]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<AttendanceDayResponse>> CheckOut(
        [FromBody] AttendanceCommandRequest? request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken token)
    {
        var result = await attendanceService.CheckOutAsync(request ?? new AttendanceCommandRequest(), idempotencyKey ?? string.Empty, token);
        return Ok(result);
    }

    [HttpPost("employees/{employeeId:guid}/days/{day}/adjust", Name = "AdjustAttendance")]
    [Authorize(Policy = HrmPolicies.AttendanceManage)]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<AttendanceDayResponse>> Adjust(
        Guid employeeId,
        DateOnly day,
        [FromBody] AdjustAttendanceRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken token)
    {
        var result = await attendanceService.AdjustAsync(employeeId, day, request, idempotencyKey ?? string.Empty, token);
        return Ok(result);
    }

    [HttpPost("employees/{employeeId:guid}/days/{day}/overtime", Name = "ConfirmAttendanceOvertime")]
    [Authorize(Policy = HrmPolicies.AttendanceManage)]
    [ProducesResponseType<AttendanceDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<AttendanceDayResponse>> ConfirmOvertime(
        Guid employeeId,
        DateOnly day,
        [FromBody] ConfirmOvertimeRequest request,
        [FromHeader(Name = "Idempotency-Key"), Required] string idempotencyKey,
        CancellationToken token)
    {
        var result = await attendanceService.ConfirmOvertimeAsync(employeeId, day, request, idempotencyKey ?? string.Empty, token);
        return Ok(result);
    }
}
