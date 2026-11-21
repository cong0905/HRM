using System.ComponentModel.DataAnnotations;
using HRM.Next.Domain.Calendar;

namespace HRM.Next.Application.Calendar;

public sealed record CreateCalendarVersionRequest(DateOnly EffectiveFrom,
    [Required] WorkCalendarDefinition Definition, [Required, StringLength(500)] string Reason);
public sealed record CalendarVersionResponse(Guid Id, DateOnly EffectiveFrom,
    WorkCalendarDefinition Definition, DateTimeOffset CreatedAtUtc);
public sealed record CalendarDayResponse(DateOnly Date, Guid VersionId,
    WorkCalendarDefinition Definition, decimal StandardMinutes);
public sealed record CalendarMonthResponse(int Year, int Month, int StandardDays,
    decimal StandardMinutes, IReadOnlyList<CalendarDayResponse> Days);
public sealed record CalendarPeriodResponse(Guid PayrollPeriodId, DateTimeOffset CapturedAtUtc, CalendarMonthResponse Calendar);
public sealed record BusinessTimeResponse(DateTimeOffset ServerTimeUtc, DateOnly BusinessDate, string TimeZone);
public sealed record PeriodEditabilityResponse(int Year, int Month, bool IsClosed);

public sealed class BusinessClock(TimeProvider clock)
{
    public BusinessTimeResponse Now()
    {
        var utc = clock.GetUtcNow().ToUniversalTime();
        return new(utc, CalendarMath.BusinessDate(utc), CalendarMath.TimeZoneId);
    }
}

public interface IWorkCalendarService
{
    Task<PeriodEditabilityResponse> EditabilityAsync(int year, int month, CancellationToken token);
    Task<CalendarVersionResponse> CreateAsync(CreateCalendarVersionRequest request, CancellationToken token);
    Task<CalendarDayResponse> DayAsync(DateOnly date, CancellationToken token);
    Task<CalendarMonthResponse> MonthAsync(int year, int month, CancellationToken token);
    Task<CalendarPeriodResponse?> ReadSnapshotAsync(Guid periodId, CancellationToken token);
    Task<CalendarPeriodResponse?> CaptureSnapshotAsync(Guid periodId, CancellationToken token);
    Task CaptureForCalculationAsync(Guid periodId, string periodCode, CancellationToken token);
}
