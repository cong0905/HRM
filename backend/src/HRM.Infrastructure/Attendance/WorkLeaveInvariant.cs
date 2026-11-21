using System.Text.Json;
using HRM.Next.Application.Common;
using HRM.Next.Domain;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Attendance;

// Called only inside the existing transaction and shared employee application lock.
// MVP has full-day leave; no proportional mixing of paid work and paid leave.
public static class WorkLeaveInvariant
{
    public static async Task EnsureAttendanceAsync(HrmDbContext db,Guid employeeId,DateOnly day,
        decimal eligibleMinutes,bool completed,decimal standardMinutes,bool overtime,CancellationToken token)
    {
        if(standardMinutes<=0m || (eligibleMinutes<=0m && completed && !overtime))return;
        if(await db.LeaveRecords.AsNoTracking().AnyAsync(l=>l.EmployeeId==employeeId &&
            l.Status=="Approved" && l.StartDate<=day && l.EndDate>=day,token))
            throw new ResourceConflictException("work_leave.paid_day_conflict",
                "A full paid-leave day cannot also receive open attendance, paid work or overtime.");
    }

    public static async Task EnsureApprovalAsync(HrmDbContext db,LeaveRecord leave,CancellationToken token)
    {
        var conflicts=await db.AttendanceSummaries.AsNoTracking().Where(s=>s.EmployeeId==leave.EmployeeId &&
            s.BusinessDate>=leave.StartDate && s.BusinessDate<=leave.EndDate &&
            (s.CheckOutUtc==null || s.EligibleMinutes>0m ||
                db.AttendanceOvertimeEntries.Any(ot=>ot.AttendanceId==s.Id && ot.Minutes>0m))).ToListAsync(token);
        foreach(var day in conflicts)
        {
            var definition=JsonSerializer.Deserialize<WorkCalendarDefinition>(day.CalendarDefinitionJson)
                ?? throw new DomainRuleException("calendar.invalid_definition","Stored attendance calendar is unavailable.");
            if(CalendarMath.StandardMinutes(definition,day.BusinessDate)>0m)
                throw new ResourceConflictException("work_leave.paid_day_conflict",
                    "Resolve open attendance, paid work and overtime before approving full-day paid leave.");
        }
    }
}
