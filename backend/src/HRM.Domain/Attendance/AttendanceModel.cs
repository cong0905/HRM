using HRM.Next.Domain.Calendar;

namespace HRM.Next.Domain.Attendance;

public sealed class AttendanceSummary
{
    public Guid Id {get;set;}
    public Guid EmployeeId {get;set;}
    public DateOnly BusinessDate {get;set;}
    public DateTimeOffset CheckInUtc {get;set;}
    public DateTimeOffset? CheckOutUtc {get;set;}
    public Guid CalendarVersionId {get;set;}
    public string CalendarDefinitionJson {get;set;}=string.Empty;
    public decimal EligibleMinutes {get;set;}
    public decimal LateMinutes {get;set;}
    public decimal EarlyLeaveMinutes {get;set;}
    public string Phase {get;set;}="Open";
    public DateTimeOffset UpdatedAtUtc {get;set;}
    public byte[] RowVersion {get;set;}=[];
}

public sealed class AttendanceEvent
{
    public Guid Id {get;set;}
    public Guid AttendanceId {get;set;}
    public Guid ActorId {get;set;}
    public string Kind {get;set;}=string.Empty;
    public DateTimeOffset OccurredAtUtc {get;set;}
    public DateTimeOffset EffectiveCheckInUtc {get;set;}
    public DateTimeOffset? EffectiveCheckOutUtc {get;set;}
}

public sealed class AttendanceAuditLog
{
    public Guid Id {get;set;}
    public Guid AttendanceId {get;set;}
    public Guid ActorId {get;set;}
    public string Reason {get;set;}=string.Empty;
    public DateTimeOffset RecordedAtUtc {get;set;}
    public DateTimeOffset BeforeCheckInUtc {get;set;}
    public DateTimeOffset? BeforeCheckOutUtc {get;set;}
    public DateTimeOffset AfterCheckInUtc {get;set;}
    public DateTimeOffset? AfterCheckOutUtc {get;set;}
    public decimal BeforeEligibleMinutes {get;set;}
    public decimal AfterEligibleMinutes {get;set;}
}

public sealed class AttendanceOvertimeEntry
{
    public Guid Id {get;set;}
    public Guid AttendanceId {get;set;}
    public Guid ConfirmedBy {get;set;}
    public decimal Minutes {get;set;}
    public string Reason {get;set;}=string.Empty;
    public DateTimeOffset ConfirmedAtUtc {get;set;}
}

public sealed class AttendanceIdempotencyRecord
{
    public Guid Id {get;set;}
    public Guid UserId {get;set;}
    public string Operation {get;set;}=string.Empty;
    public Guid Key {get;set;}
    public string RequestHash {get;set;}=string.Empty;
    public string ResponseJson {get;set;}=string.Empty;
    public DateTimeOffset CreatedAtUtc {get;set;}
}

public sealed record AttendanceMetrics(decimal EligibleMinutes,decimal LateMinutes,decimal EarlyLeaveMinutes);
public static class AttendanceMath
{
    public static AttendanceMetrics Calculate(WorkCalendarDefinition calendar,DateOnly date,DateTimeOffset checkIn,DateTimeOffset? checkOut)
    {
        var eligible=CalendarMath.EligibleMinutes(calendar,date,checkIn,checkOut);
        if(CalendarMath.StandardMinutes(calendar,date)==0m)return new(0m,0m,0m);
        var offset=TimeSpan.FromHours(7);
        var start=new DateTimeOffset(date.ToDateTime(calendar.ShiftStart),offset);
        var end=new DateTimeOffset(date.ToDateTime(calendar.ShiftEnd),offset);
        var late=checkIn<=start?0m:CalendarMath.EligibleMinutes(calendar,date,start,checkIn>=end?end:checkIn);
        var early=checkOut.HasValue && checkOut.Value<end
            ?CalendarMath.EligibleMinutes(calendar,date,checkOut.Value<=start?start:checkOut.Value,end):0m;
        return new(eligible,late,early);
    }
    public static string Status(AttendanceSummary summary,DateOnly today) => summary.CheckOutUtc.HasValue
        ? (summary.Phase=="Corrected" ? "Corrected":"Complete")
        : (summary.BusinessDate<today ? "MissingCheckout":(summary.Phase=="Corrected" ? "Incomplete":"CheckedIn"));
}
