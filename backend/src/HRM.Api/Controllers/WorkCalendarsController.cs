using System.ComponentModel.DataAnnotations;
using HRM.Next.Api.Errors;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Domain.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

public sealed record PreviewCalendarRequest([Range(1900, 9998)] int Year, [Range(1, 12)] int Month, WorkCalendarDefinition Definition);

[ApiController, Route("api/v1/work-calendars")]
public sealed class WorkCalendarsController(IWorkCalendarService calendars, BusinessClock clock) : ControllerBase
{
    private ObjectResult Missing(string code) => new(ApiProblems.Create(HttpContext,404,code))
        {StatusCode=404, ContentTypes={"application/problem+json"}};

    [HttpPost("preview", Name = "PreviewCalendar"), Authorize(Policy = HrmPolicies.CalendarManage)]
    [ProducesResponseType<CalendarMonthResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(422)]
    public CalendarMonthResponse Preview(PreviewCalendarRequest request)
    {
        CalendarMath.Validate(request.Definition);
        var days = Enumerable.Range(1, DateTime.DaysInMonth(request.Year, request.Month))
            .Select(d => new DateOnly(request.Year, request.Month, d))
            .Select(d => new CalendarDayResponse(d, Guid.Empty, request.Definition, CalendarMath.StandardMinutes(request.Definition, d))).ToList();
        return new(request.Year, request.Month, days.Count(d => d.StandardMinutes > 0), days.Sum(d => d.StandardMinutes), days);
    }

    [HttpGet("now", Name = "GetBusinessTime"), Authorize(Policy = HrmPolicies.CalendarRead)]
    [ProducesResponseType<BusinessTimeResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public BusinessTimeResponse Now() => clock.Now();

    [HttpGet("months/{year:int}/{month:int}/editability", Name = "GetPeriodEditability"), Authorize(Policy = HrmPolicies.CalendarRead)]
    [ProducesResponseType<PeriodEditabilityResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    public async Task<ActionResult<PeriodEditabilityResponse>> Editability([Range(1900,9998)] int year,
        [Range(1,12)] int month, CancellationToken token) => Ok(await calendars.EditabilityAsync(year, month, token));

    [HttpPost(Name = "CreateCalendarVersion"), Authorize(Policy = HrmPolicies.CalendarManage)]
    [ProducesResponseType<CalendarVersionResponse>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<CalendarVersionResponse>> Create(CreateCalendarVersionRequest request, CancellationToken token)
    {
        var created = await calendars.CreateAsync(request, token);
        return CreatedAtRoute("GetCalendarDay", new { date = created.EffectiveFrom.ToString("yyyy-MM-dd") }, created);
    }

    [HttpGet("days/{date}", Name = "GetCalendarDay"), Authorize(Policy = HrmPolicies.CalendarRead)]
    [ProducesResponseType<CalendarDayResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<CalendarDayResponse>> Day(DateOnly date, CancellationToken token) => Ok(await calendars.DayAsync(date, token));

    [HttpGet("months/{year:int}/{month:int}", Name = "GetCalendarMonth"), Authorize(Policy = HrmPolicies.CalendarRead)]
    [ProducesResponseType<CalendarMonthResponse>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<CalendarMonthResponse>> Month([Range(1900,9998)] int year, [Range(1,12)] int month, CancellationToken token) =>
        Ok(await calendars.MonthAsync(year, month, token));

    [HttpGet("payroll-periods/{id:guid}/snapshot", Name = "GetPeriodCalendarSnapshot"), Authorize(Policy = HrmPolicies.PayrollRead)]
    [ProducesResponseType<CalendarPeriodResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<CalendarPeriodResponse>> Snapshot(Guid id, CancellationToken token)
    {
        var result = await calendars.ReadSnapshotAsync(id, token);
        return result is null ? Missing("calendar.snapshot_not_found") : Ok(result);
    }

    [HttpPost("payroll-periods/{id:guid}/snapshot", Name = "CapturePeriodCalendarSnapshot"), Authorize(Policy = HrmPolicies.PayrollCalculate)]
    [ProducesResponseType<CalendarPeriodResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    [ProducesResponseType<ProblemDetails>(422)]
    public async Task<ActionResult<CalendarPeriodResponse>> Capture(Guid id, CancellationToken token)
    {
        var result = await calendars.CaptureSnapshotAsync(id, token);
        return result is null ? Missing("payroll.period_not_found") : Ok(result);
    }
}
