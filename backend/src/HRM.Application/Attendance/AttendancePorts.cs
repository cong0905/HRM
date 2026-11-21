using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HRM.Next.Application.Common;

namespace HRM.Next.Application.Attendance;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AttendanceCommandRequest([StringLength(200)] string? Note=null,DateTimeOffset? ClientTimeUtc=null);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AdjustAttendanceRequest(DateTimeOffset CheckInUtc,DateTimeOffset? CheckOutUtc,
    [Required,StringLength(500,MinimumLength=10)] string Reason,[Required] string RowVersion);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ConfirmOvertimeRequest(decimal Minutes,[Required,StringLength(500,MinimumLength=10)] string Reason,
    [Required] string RowVersion);
public sealed record AttendanceDayResponse(Guid Id,Guid EmployeeId,DateOnly BusinessDate,
    DateTimeOffset CheckInUtc,DateTimeOffset? CheckOutUtc,decimal EligibleMinutes,decimal LateMinutes,
    decimal EarlyLeaveMinutes,string Status,Guid CalendarVersionId,decimal ConfirmedOvertimeMinutes,
    DateTimeOffset UpdatedAtUtc,string RowVersion);
public sealed class AttendanceAccessDeniedException(string code) : Exception("Attendance access is not permitted.")
{ public string Code {get;}=code; }
public sealed class AttendanceNotFoundException(string code) : Exception("Attendance record not found.")
{ public string Code {get;}=code; }

public interface IAttendanceService
{
    Task<AttendanceDayResponse?> GetOwnDayAsync(DateOnly? day,CancellationToken token);
    Task<PagedResult<AttendanceDayResponse>> GetOwnHistoryAsync(int page,int pageSize,CancellationToken token);
    Task<PagedResult<AttendanceDayResponse>> ListAsync(Guid? employeeId, DateOnly? from, DateOnly? to, string? status, int page, int pageSize, CancellationToken token);
    Task<AttendanceDayResponse?> GetEmployeeDayAsync(Guid employeeId,DateOnly day,CancellationToken token);
    Task<AttendanceDayResponse> CheckInAsync(AttendanceCommandRequest request,string key,CancellationToken token);
    Task<AttendanceDayResponse> CheckOutAsync(AttendanceCommandRequest request,string key,CancellationToken token);
    Task<AttendanceDayResponse> AdjustAsync(Guid employeeId,DateOnly day,AdjustAttendanceRequest request,string key,CancellationToken token);
    Task<AttendanceDayResponse> ConfirmOvertimeAsync(Guid employeeId,DateOnly day,ConfirmOvertimeRequest request,string key,CancellationToken token);
}
