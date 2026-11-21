using System.Net;
using System.Net.Http.Json;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Payroll;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain.Calendar;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    [Fact]
    public async Task Attendance_TimeReversalAndInvalidCorrection_RejectWithoutNewEvidence()
    {
        await using var s=await StartAttendanceAsync();var original=await AttInAsync(s);
        s.Harness.Clock.Advance(TimeSpan.FromMinutes(-30));
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid()))
        {Assert.Contains(r.StatusCode,new[]{HttpStatusCode.Conflict,HttpStatusCode.UnprocessableEntity});await ProblemAsync(r,r.StatusCode);}
        s.Harness.Clock.Advance(TimeSpan.FromHours(11));
        foreach(var request in new[]{
            new AdjustAttendanceRequest(DateTimeOffset.Parse("2026-11-11T01:30:00Z"),null,"Wrong business date correction",original.RowVersion),
            new AdjustAttendanceRequest(s.Harness.Clock.GetUtcNow().AddHours(1),null,"Future instant is not valid",original.RowVersion),
            new AdjustAttendanceRequest(original.CheckInUtc,original.CheckInUtc.AddMinutes(-1),"Checkout precedes checkin time",original.RowVersion)
        })using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),request,Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.UnprocessableEntity);
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AttendanceEvents.CountAsync());Assert.Equal(1,await db.AttendanceIdempotencyRecords.CountAsync());Assert.Equal(0,await db.AttendanceAuditLogs.CountAsync());
    }

    [Fact]
    public async Task Attendance_PinnedDayCalendar_SurvivesConfigurationChangeDuringPresence()
    {
        await using var s=await StartAttendanceAsync();var original=await AttInAsync(s);
        using(var c=await BearerClientAsync(s.Harness,s.Hr))using(var r=await c.PostAsJsonAsync("/api/v1/work-calendars",new CreateCalendarVersionRequest(new(2026,11,12),CalendarMathTests.Definition("T10-CHANGED") with {ShiftEnd=new(16,30)},"Change calendar without changing existing attendance")))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        s.Harness.Clock.Advance(TimeSpan.FromHours(9));using var checkout=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid());var actual=await AttOkAsync(checkout);Assert.Equal(480m,actual.EligibleMinutes);Assert.Equal(original.CalendarVersionId,actual.CalendarVersionId);
        using var hr=await BearerClientAsync(s.Harness,s.Hr);var current=await hr.GetFromJsonAsync<CalendarDayResponse>("/api/v1/work-calendars/days/2026-11-12");Assert.NotNull(current);Assert.Equal(420m,current.StandardMinutes);Assert.NotEqual(actual.CalendarVersionId,current.VersionId);
    }

    [Fact]
    public async Task Attendance_HrCanCorrectHistoricalDayAfterOffboarding_ButCannotCreateNewWork()
    {
        await using var s=await StartAttendanceAsync();var original=await AttCompleteAsync(s,true);
        using(var hr=await BearerClientAsync(s.Harness,s.Hr))
        {
            var profile=await hr.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{s.Profile.Id}");Assert.NotNull(profile?.RowVersion);
            using var off=await hr.PostAsJsonAsync($"/api/v1/employees/{s.Profile.Id}/offboard",new OffboardEmployeeRequest(new(2026,11,12),"Leave employment with pending correction",profile.RowVersion));Assert.Equal(HttpStatusCode.NoContent,off.StatusCode);
        }
        using var corrected=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(DateTimeOffset.Parse("2026-11-12T01:30:00Z"),DateTimeOffset.Parse("2026-11-12T10:30:00Z"),"Historical correction before termination date",original.RowVersion),Guid.NewGuid());var current=await AttOkAsync(corrected);Assert.Equal(480m,current.EligibleMinutes);
        await using var db=sql.CreateContext();Assert.False((await db.Users.SingleAsync(u=>u.Id==s.Worker.Id)).IsActive);Assert.Equal(3,await db.AttendanceEvents.CountAsync());Assert.Equal(1,await db.AttendanceAuditLogs.CountAsync());
    }

    [Fact]
    public async Task Attendance_HrClearedCheckout_CannotBeOverwrittenByEmployee()
    {
        await using var s=await StartAttendanceAsync();var original=await AttCompleteAsync(s);
        using var cleared=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(original.CheckInUtc,null,"HR marks checkout as requiring review",original.RowVersion),Guid.NewGuid());var current=await AttOkAsync(cleared);Assert.Equal(0m,current.EligibleMinutes);Assert.Equal("Incomplete",current.Status);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using(var db=sql.CreateContext()){Assert.Equal(1,await db.AttendanceEvents.CountAsync(e=>e.Kind=="CheckOut"));Assert.Null((await db.AttendanceSummaries.SingleAsync()).CheckOutUtc);}
        s.Harness.Clock.Advance(TimeSpan.FromDays(1));using var me=await BearerClientAsync(s.Harness,s.Worker);var day=await me.GetFromJsonAsync<AttendanceDayResponse>("/api/v1/attendance/me/days/2026-11-12");Assert.Equal("MissingCheckout",day!.Status);Assert.Equal(0m,day.EligibleMinutes);
    }

    [Fact]
    public async Task Attendance_ConcurrentOffboardingAndCheckIn_NoUsableWriteAfterCommit()
    {
        await using var s=await StartAttendanceAsync();using var me=await BearerClientAsync(s.Harness,s.Worker);using var hr=await BearerClientAsync(s.Harness,s.Hr);
        var profile=await hr.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{s.Profile.Id}");Assert.NotNull(profile?.RowVersion);
        using var message=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-in"){Content=JsonContent.Create(new{})};message.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());
        var offTask=hr.PostAsJsonAsync($"/api/v1/employees/{s.Profile.Id}/offboard",new OffboardEmployeeRequest(new(2026,11,12),"Concurrent offboarding fixture",profile.RowVersion));var punchTask=me.SendAsync(message);
        await Task.WhenAll(offTask,punchTask);using var off=await offTask;using var punch=await punchTask;Assert.Equal(HttpStatusCode.NoContent,off.StatusCode);Assert.Contains(punch.StatusCode,new[]{HttpStatusCode.OK,HttpStatusCode.Unauthorized,HttpStatusCode.Forbidden,HttpStatusCode.Conflict});
        using var retry=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-in"){Content=JsonContent.Create(new{})};retry.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());using var after=await me.SendAsync(retry);await ProblemAsync(after,HttpStatusCode.Unauthorized);
        await using var db=sql.CreateContext();Assert.InRange(await db.AttendanceEvents.CountAsync(),0,1);Assert.False((await db.Users.SingleAsync(u=>u.Id==s.Worker.Id)).IsActive);
    }

    [Fact]
    public async Task Attendance_ConcurrentPublishAndCorrection_CloseBoundaryIsEnforced()
    {
        await using var s=await StartAttendanceAsync();var original=await AttCompleteAsync(s,true);
        var reviewer=await CreateUserWithRolesAsync(s.Harness,"att-concurrent-review",HRM.Next.Application.Authorization.HrmRoles.Payroll);var id=await CalendarPeriodAsync(s.Harness,"2026-11");
        using(var c=await BearerClientAsync(s.Harness,s.Payroll))using(var r=await c.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/calculate",new PayrollActionRequest("Calculate closure boundary")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using var pub=await BearerClientAsync(s.Harness,reviewer);using(var r=await pub.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/approve",new PayrollActionRequest("Approve closure boundary")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        var request=new AdjustAttendanceRequest(DateTimeOffset.Parse("2026-11-12T01:30:00Z"),DateTimeOffset.Parse("2026-11-12T10:30:00Z"),"Concurrent correction before publication",original.RowVersion);
        var publishing=pub.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/publish",new PayrollActionRequest("Publish with pending attendance request"));var correcting=AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),request,Guid.NewGuid());
        await Task.WhenAll(publishing,correcting);using var published=await publishing;using var adjusted=await correcting;Assert.Equal(HttpStatusCode.NoContent,published.StatusCode);Assert.Contains(adjusted.StatusCode,new[]{HttpStatusCode.OK,HttpStatusCode.Conflict});
        using var hr=await BearerClientAsync(s.Harness,s.Hr);var fresh=await hr.GetFromJsonAsync<AttendanceDayResponse>(AttDay(s.Profile.Id));Assert.NotNull(fresh);
        using(var after=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),request with {RowVersion=fresh.RowVersion},Guid.NewGuid()))await ProblemAsync(after,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Equal("Published",(await db.PayrollPeriods.SingleAsync(p=>p.Id==id)).Status);Assert.InRange(await db.AttendanceAuditLogs.CountAsync(),0,1);
    }
}
