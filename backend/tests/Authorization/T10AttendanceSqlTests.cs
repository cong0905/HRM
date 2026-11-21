using HRM.Next.Domain.Audit;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Personnel;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Payroll;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    private async Task<AttendanceScenario> StartAttendanceAsync()
    {
        await sql.ResetAsync();var h=new AuthHarness(sql.ConnectionString);
        h.Clock.Advance(DateTimeOffset.Parse("2026-11-12T01:30:00Z")-h.Clock.GetUtcNow());
        var worker=await CreateUserWithRolesAsync(h,"att-worker",HrmRoles.Employee);
        var hr=await CreateUserWithRolesAsync(h,"att-hr",HrmRoles.Hr);
        var otherHr=await CreateUserWithRolesAsync(h,"att-other-hr",HrmRoles.Hr);
        var payroll=await CreateUserWithRolesAsync(h,"att-payroll",HrmRoles.Payroll);
        var admin=await CreateUserWithRolesAsync(h,"att-admin",HrmRoles.SystemAdmin);
        var profile=await CreateEmployeeProfileAsync(h,worker,"Attendance worker","t10-worker-profile");
        await AssignHrScopeAsync(h,hr.Id,profile.Id);
        await using(var scope=h.Factory.Services.CreateAsyncScope())
        {
            var db=scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var tracked=await db.EmployeeProfiles.AsTracking().SingleAsync(e=>e.Id==profile.Id);tracked.HireDate=new(2026,11,1);await db.SaveChangesAsync();
        }
        using(var client=await BearerClientAsync(h,hr))
        using(var created=await client.PostAsJsonAsync("/api/v1/work-calendars",new CreateCalendarVersionRequest(new(2026,1,1),CalendarMathTests.Definition("T10-CALENDAR"),"Attendance fixture setup")))
            Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        return new(sql,h,worker,hr,otherHr,payroll,admin,profile);
    }
    private static string AttDay(Guid employeeId,string suffix="")=>$"/api/v1/attendance/employees/{employeeId}/days/2026-11-12"+suffix;
    private static async Task<HttpResponseMessage> AttPostAsync(AuthHarness h,HrmUser actor,string route,object body,Guid key)
    {
        using var client=await BearerClientAsync(h,actor);
        using var message=new HttpRequestMessage(HttpMethod.Post,route){Content=JsonContent.Create(body)};
        message.Headers.Add("Idempotency-Key",key.ToString());return await client.SendAsync(message);
    }
    private static async Task<AttendanceDayResponse> AttOkAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);var body=await response.Content.ReadFromJsonAsync<AttendanceDayResponse>();Assert.NotNull(body);return body;
    }
    private static async Task<AttendanceDayResponse> AttInAsync(AttendanceScenario s)
    {
        using var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},Guid.NewGuid());return await AttOkAsync(r);
    }
    private static async Task<AttendanceDayResponse> AttCompleteAsync(AttendanceScenario s,bool late=false)
    {
        if(late)s.Harness.Clock.Advance(TimeSpan.FromMinutes(90));
        await AttInAsync(s);s.Harness.Clock.Advance(TimeSpan.FromHours(9));
        using var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid());return await AttOkAsync(r);
    }

    [Fact]
    public async Task Attendance_ServerClock_IgnoresClientTimestamp_AndPinsCalendar()
    {
        await using var s=await StartAttendanceAsync();var now=s.Harness.Clock.GetUtcNow();
        using var response=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{note="Client clock must be ignored",clientTimeUtc=DateTimeOffset.Parse("2099-01-01T00:00:00Z")},Guid.NewGuid());
        var result=await AttOkAsync(response);Assert.Equal(now,result.CheckInUtc);Assert.Equal(new DateOnly(2026,11,12),result.BusinessDate);Assert.Equal(0m,result.EligibleMinutes);Assert.Null(result.CheckOutUtc);
        await using var db=sql.CreateContext();var summary=await db.AttendanceSummaries.SingleAsync();Assert.Equal(result.CalendarVersionId,summary.CalendarVersionId);Assert.Contains("T10-CALENDAR",summary.CalendarDefinitionJson);
        Assert.Equal(now,(await db.AttendanceEvents.SingleAsync()).OccurredAtUtc);
    }

    [Fact]
    public async Task Attendance_ConcurrentSameKeyCheckIn_ReplaysOneDurableEvent()
    {
        await using var s=await StartAttendanceAsync();using var client=await BearerClientAsync(s.Harness,s.Worker);var key=Guid.NewGuid();
        async Task<HttpResponseMessage> Send(){using var m=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-in"){Content=JsonContent.Create(new{})};m.Headers.Add("Idempotency-Key",key.ToString());return await client.SendAsync(m);}
        var responses=await Task.WhenAll(Send(),Send());
        try{Assert.All(responses,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));Assert.Equal(await responses[0].Content.ReadAsStringAsync(),await responses[1].Content.ReadAsStringAsync());}
        finally{foreach(var r in responses)r.Dispose();}
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AttendanceSummaries.CountAsync());Assert.Equal(1,await db.AttendanceEvents.CountAsync());Assert.Equal(1,await db.AttendanceIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task Attendance_ConcurrentDifferentKeys_OneCheckInOneConflict()
    {
        await using var s=await StartAttendanceAsync();using var client=await BearerClientAsync(s.Harness,s.Worker);
        async Task<HttpResponseMessage> Send(){using var m=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-in"){Content=JsonContent.Create(new{})};m.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());return await client.SendAsync(m);}
        var responses=await Task.WhenAll(Send(),Send());try{Assert.Equal(new[]{200,409},responses.Select(r=>(int)r.StatusCode).Order().ToArray());}finally{foreach(var r in responses)r.Dispose();}
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AttendanceEvents.CountAsync());Assert.Equal(1,await db.AttendanceSummaries.CountAsync());Assert.Equal(1,await db.AttendanceIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task Attendance_KeyPayloadMismatch_RejectsButJsonOrderIsCanonical()
    {
        await using var s=await StartAttendanceAsync();var key=Guid.NewGuid();var time=DateTimeOffset.Parse("2099-01-01T00:00:00Z");
        using var first=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{note="original",clientTimeUtc=time},key);await AttOkAsync(first);
        using var replay=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{clientTimeUtc=time,note="original"},key);Assert.Equal(await first.Content.ReadAsStringAsync(),await replay.Content.ReadAsStringAsync());
        using var mismatch=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{note="changed",clientTimeUtc=time},key);await ProblemAsync(mismatch,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AttendanceEvents.CountAsync());Assert.Equal(1,await db.AttendanceIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task Attendance_KeysAreScopedToActorAndOperation()
    {
        await using var s=await StartAttendanceAsync();var key=Guid.NewGuid();using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key))await AttOkAsync(r);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},key)){var day=await AttOkAsync(r);Assert.Equal(0m,day.EligibleMinutes);}
        var other=await CreateUserWithRolesAsync(s.Harness,"att-key-other",HrmRoles.Employee);await CreateEmployeeProfileAsync(s.Harness,other,"Other worker","t10-key-other");
        using(var r=await AttPostAsync(s.Harness,other,"/api/v1/attendance/me/check-in",new{},key))await AttOkAsync(r);
        await using var db=sql.CreateContext();Assert.Equal(3,await db.AttendanceIdempotencyRecords.CountAsync());Assert.Equal(3,await db.AttendanceEvents.CountAsync());
    }

    [Fact]
    public async Task Attendance_CheckoutBeforeCheckin_AndInvalidKeys_FailWithoutWrites()
    {
        await using var s=await StartAttendanceAsync();using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},Guid.Empty))await ProblemAsync(r,HttpStatusCode.BadRequest);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{userId=s.Hr.Id},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.BadRequest);
        using(var c=await BearerClientAsync(s.Harness,s.Worker))using(var r=await c.PostAsJsonAsync("/api/v1/attendance/me/check-in",new{}))await ProblemAsync(r,HttpStatusCode.BadRequest);
        await using var db=sql.CreateContext();Assert.Equal(0,await db.AttendanceSummaries.CountAsync());Assert.Equal(0,await db.AttendanceEvents.CountAsync());Assert.Equal(0,await db.AttendanceIdempotencyRecords.CountAsync());
    }

    [Fact]
    public async Task Attendance_IncompleteDay_IsMissingAfterMidnight_AndReplayKeepsOriginal()
    {
        await using var s=await StartAttendanceAsync();var key=Guid.NewGuid();using var first=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key);await AttOkAsync(first);
        s.Harness.Clock.Advance(TimeSpan.FromDays(1));
        using var replay=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key);Assert.Equal(await first.Content.ReadAsStringAsync(),await replay.Content.ReadAsStringAsync());
        using(var me=await BearerClientAsync(s.Harness,s.Worker))
        {var history=await me.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance/me/history");Assert.NotNull(history);Assert.Single(history.Items);Assert.Equal("MissingCheckout",history.Items[0].Status);Assert.Equal(0m,history.Items[0].EligibleMinutes);}
        using(var oldCheckout=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid()))await ProblemAsync(oldCheckout,HttpStatusCode.Conflict);
        using(var today=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},Guid.NewGuid()))Assert.Equal(new DateOnly(2026,11,13),(await AttOkAsync(today)).BusinessDate);
        await using var db=sql.CreateContext();Assert.Equal(2,await db.AttendanceEvents.CountAsync());Assert.All(await db.AttendanceSummaries.ToListAsync(),a=>Assert.Equal(0m,a.EligibleMinutes));
    }

    [Fact]
    public async Task Attendance_ConcurrentCheckout_ReplaysOnce_AndNeverAutoApprovesOvertime()
    {
        await using var s=await StartAttendanceAsync();s.Harness.Clock.Advance(TimeSpan.FromMinutes(90));await AttInAsync(s);s.Harness.Clock.Advance(TimeSpan.FromHours(9));
        using var client=await BearerClientAsync(s.Harness,s.Worker);var key=Guid.NewGuid();
        async Task<HttpResponseMessage> Send(){using var m=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-out"){Content=JsonContent.Create(new{})};m.Headers.Add("Idempotency-Key",key.ToString());return await client.SendAsync(m);}
        var responses=await Task.WhenAll(Send(),Send());try{var result=await AttOkAsync(responses[0]);await AttOkAsync(responses[1]);Assert.Equal(390m,result.EligibleMinutes);Assert.Equal(90m,result.LateMinutes);Assert.Equal(0m,result.ConfirmedOvertimeMinutes);Assert.Equal(await responses[0].Content.ReadAsStringAsync(),await responses[1].Content.ReadAsStringAsync());}finally{foreach(var r in responses)r.Dispose();}
        await using var db=sql.CreateContext();Assert.Equal(2,await db.AttendanceEvents.CountAsync());Assert.Equal(0,await db.AttendanceOvertimeEntries.CountAsync());
    }

    [Fact]
    public async Task Attendance_HrCorrection_PreservesOriginalEventsAndDedicatedAudit()
    {
        await using var s=await StartAttendanceAsync();var before=await AttCompleteAsync(s,true);
        var request=new AdjustAttendanceRequest(DateTimeOffset.Parse("2026-11-12T01:30:00Z"),DateTimeOffset.Parse("2026-11-12T10:30:00Z"),"Correct approved working hours",before.RowVersion);
        using var correction=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),request,Guid.NewGuid());var after=await AttOkAsync(correction);
        Assert.Equal(480m,after.EligibleMinutes);Assert.Equal("Corrected",after.Status);Assert.NotEqual(before.RowVersion,after.RowVersion);
        await using var db=sql.CreateContext();var original=await db.AttendanceEvents.Where(e=>e.Kind=="CheckIn" || e.Kind=="CheckOut").ToListAsync();Assert.Equal(2,original.Count);Assert.Equal(before.CheckInUtc,original.Single(e=>e.Kind=="CheckIn").EffectiveCheckInUtc);Assert.Equal(before.CheckOutUtc,original.Single(e=>e.Kind=="CheckOut").EffectiveCheckOutUtc);
        var audit=await db.AttendanceAuditLogs.SingleAsync();Assert.Equal(before.CheckInUtc,audit.BeforeCheckInUtc);Assert.Equal(after.CheckInUtc,audit.AfterCheckInUtc);Assert.Equal(390m,audit.BeforeEligibleMinutes);Assert.Equal(480m,audit.AfterEligibleMinutes);
    }

    [Fact]
    public async Task Attendance_ConcurrentHrCorrection_OneSuccessOneConflict_AndNoOpBumpsVersion()
    {
        await using var s=await StartAttendanceAsync();var before=await AttCompleteAsync(s,true);await AssignHrScopeAsync(s.Harness,s.OtherHr.Id,s.Profile.Id);
        var a=new AdjustAttendanceRequest(DateTimeOffset.Parse("2026-11-12T01:30:00Z"),DateTimeOffset.Parse("2026-11-12T10:30:00Z"),"First scoped HR correction",before.RowVersion);
        var b=a with {CheckInUtc=DateTimeOffset.Parse("2026-11-12T01:45:00Z"),Reason="Second scoped HR correction"};
        var responses=await Task.WhenAll(AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),a,Guid.NewGuid()),AttPostAsync(s.Harness,s.OtherHr,AttDay(s.Profile.Id,"/adjust"),b,Guid.NewGuid()));
        AttendanceDayResponse current;try{Assert.Equal(new[]{200,409},responses.Select(r=>(int)r.StatusCode).Order().ToArray());current=await AttOkAsync(responses.Single(r=>r.StatusCode==HttpStatusCode.OK));}finally{foreach(var r in responses)r.Dispose();}
        var noOp=new AdjustAttendanceRequest(current.CheckInUtc,current.CheckOutUtc,"Record reviewed without changing times",current.RowVersion);
        using var result=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),noOp,Guid.NewGuid());Assert.NotEqual(current.RowVersion,(await AttOkAsync(result)).RowVersion);
        await using var db=sql.CreateContext();Assert.Equal(2,await db.AttendanceAuditLogs.CountAsync());
    }

    [Fact]
    public async Task Attendance_ScopeAndPayrollProjection_PreventIdorAndPrivilegeEscalation()
    {
        await using var s=await StartAttendanceAsync();var day=await AttCompleteAsync(s);
        var adjust=new AdjustAttendanceRequest(day.CheckInUtc,day.CheckOutUtc,"Unauthorized correction attempt",day.RowVersion);
        using(var r=await AttPostAsync(s.Harness,s.Worker,AttDay(s.Profile.Id,"/adjust"),adjust,Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var r=await AttPostAsync(s.Harness,s.OtherHr,AttDay(s.Profile.Id,"/adjust"),adjust,Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var c=await BearerClientAsync(s.Harness,s.OtherHr))using(var r=await c.GetAsync(AttDay(s.Profile.Id)))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var c=await BearerClientAsync(s.Harness,s.Admin))using(var r=await c.GetAsync(AttDay(s.Profile.Id)))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var c=await BearerClientAsync(s.Harness,s.Payroll))using(var r=await c.GetAsync(AttDay(s.Profile.Id))){await AttOkAsync(r);var json=await r.Content.ReadAsStringAsync();Assert.DoesNotContain("citizenId",json);Assert.DoesNotContain("address",json);}
        using(var r=await AttPostAsync(s.Harness,s.Payroll,AttDay(s.Profile.Id,"/adjust"),adjust,Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Forbidden);
        var other=await CreateUserWithRolesAsync(s.Harness,"att-history-other",HrmRoles.Employee);await CreateEmployeeProfileAsync(s.Harness,other,"Separate employee","t10-other-history");using(var r=await AttPostAsync(s.Harness,other,"/api/v1/attendance/me/check-in",new{},Guid.NewGuid()))await AttOkAsync(r);
        using(var c=await BearerClientAsync(s.Harness,s.Worker)){var history=await c.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance/me/history");Assert.NotNull(history);Assert.All(history.Items,d=>Assert.Equal(s.Profile.Id,d.EmployeeId));using var badPage=await c.GetAsync("/api/v1/attendance/me/history?pageSize=101");await ProblemAsync(badPage,HttpStatusCode.BadRequest);}
    }

    [Fact]
    public async Task Attendance_OvertimeIsExplicitScopedAndIdempotent_WithNoDoubleCredit()
    {
        await using var s=await StartAttendanceAsync();var before=await AttCompleteAsync(s,true);var key=Guid.NewGuid();var request=new{minutes="90",reason="HR confirms explicit overtime minutes",rowVersion=before.RowVersion};
        using var response=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/overtime"),request,key);var after=await AttOkAsync(response);Assert.Equal(390m,after.EligibleMinutes);Assert.Equal(90m,after.ConfirmedOvertimeMinutes);Assert.NotEqual(before.RowVersion,after.RowVersion);
        using var replay=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/overtime"),request,key);Assert.Equal(await response.Content.ReadAsStringAsync(),await replay.Content.ReadAsStringAsync());
        using(var twice=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/overtime"),new{minutes="90",reason="Attempt duplicate overtime approval",rowVersion=after.RowVersion},Guid.NewGuid()))await ProblemAsync(twice,HttpStatusCode.Conflict);
        using(var edit=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(after.CheckInUtc,after.CheckOutUtc,"Cannot alter confirmed overtime day",after.RowVersion),Guid.NewGuid()))await ProblemAsync(edit,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AttendanceOvertimeEntries.CountAsync());
    }

    [Fact]
    public async Task Attendance_PublishedMonth_BlocksNewAndAdjustedWork_ButAllowsOriginalReplay()
    {
        await using var s=await StartAttendanceAsync();var key=Guid.NewGuid();using var initial=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key);await AttOkAsync(initial);s.Harness.Clock.Advance(TimeSpan.FromHours(9));using var checkout=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid());var day=await AttOkAsync(checkout);
        var reviewer=await CreateUserWithRolesAsync(s.Harness,"att-payroll-review",HrmRoles.Payroll);var periodId=await CalendarPeriodAsync(s.Harness,"2026-11");
        using(var c=await BearerClientAsync(s.Harness,s.Payroll))using(var r=await c.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/calculate",new PayrollActionRequest("Calculate attendance fixture")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var c=await BearerClientAsync(s.Harness,reviewer)){using(var r=await c.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/approve",new PayrollActionRequest("Approve attendance fixture")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);using(var r=await c.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/publish",new PayrollActionRequest("Publish attendance fixture")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);}
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(day.CheckInUtc,day.CheckOutUtc,"Cannot edit published attendance",day.RowVersion),Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/overtime"),new{minutes="30",reason="Cannot confirm in closed month",rowVersion=day.RowVersion},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        s.Harness.Clock.Advance(TimeSpan.FromDays(1));using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        using var replay=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key);Assert.Equal(await initial.Content.ReadAsStringAsync(),await replay.Content.ReadAsStringAsync());
        await using var db=sql.CreateContext();Assert.Equal(2,await db.AttendanceEvents.CountAsync());
    }

    [Fact]
    public async Task Attendance_Offboarding_RejectsNewWorkAndPreservesPriorHistory()
    {
        await using var s=await StartAttendanceAsync();using var oldClient=await BearerClientAsync(s.Harness,s.Worker);await AttInAsync(s);
        using(var hr=await BearerClientAsync(s.Harness,s.Hr)){var profile=await hr.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{s.Profile.Id}");Assert.NotNull(profile?.RowVersion);using var off=await hr.PostAsJsonAsync($"/api/v1/employees/{s.Profile.Id}/offboard",new OffboardEmployeeRequest(new(2026,11,12),"Offboard active attendance employee",profile.RowVersion));Assert.Equal(HttpStatusCode.NoContent,off.StatusCode);}
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-out"){Content=JsonContent.Create(new{})};request.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());using var rejected=await oldClient.SendAsync(request);await ProblemAsync(rejected,HttpStatusCode.Unauthorized);
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AttendanceEvents.CountAsync());Assert.Equal(1,await db.AttendanceSummaries.CountAsync());Assert.Null((await db.AttendanceSummaries.SingleAsync()).CheckOutUtc);
    }

    [Fact]
    public async Task Attendance_AuditFailure_RollsBackSummaryEventsAndReceipt_ThenSameKeyCanRetry()
    {
        await using var s=await StartAttendanceAsync();var probe=new AttendanceFlushProbe();await using var bad=new AuthHarness(sql.ConnectionString,configure:x=>x.AddScoped<IAuditRepository>(provider=>new AttendanceAfterFlushAuditRepository(provider.GetRequiredService<HrmDbContext>(),probe)));bad.Clock.Advance(s.Harness.Clock.GetUtcNow()-bad.Clock.GetUtcNow());var key=Guid.NewGuid();
        using(var r=await AttPostAsync(bad,s.Worker,"/api/v1/attendance/me/check-in",new{},key))await ProblemAsync(r,HttpStatusCode.InternalServerError);
        Assert.Single(probe.Observations);Assert.Equal(1,probe.Observations[0].Summaries);Assert.Equal(1,probe.Observations[0].Events);
        await using(var db=sql.CreateContext()){Assert.Equal(0,await db.AttendanceSummaries.CountAsync());Assert.Equal(0,await db.AttendanceEvents.CountAsync());Assert.Equal(0,await db.AttendanceIdempotencyRecords.CountAsync());}
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key))await AttOkAsync(r);
        s.Harness.Clock.Advance(TimeSpan.FromHours(9));using var done=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid());var before=await AttOkAsync(done);bad.Clock.Advance(s.Harness.Clock.GetUtcNow()-bad.Clock.GetUtcNow());
        using(var r=await AttPostAsync(bad,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(before.CheckInUtc,before.CheckOutUtc,"Rollback this correction on audit failure",before.RowVersion),Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.InternalServerError);
        Assert.Equal(2,probe.Observations.Count);Assert.Equal(1,probe.Observations[1].Audits);Assert.Equal(3,probe.Observations[1].Events);
        await using(var db=sql.CreateContext()){Assert.Equal(0,await db.AttendanceAuditLogs.CountAsync());Assert.Equal(2,await db.AttendanceEvents.CountAsync());Assert.Equal(2,await db.AttendanceIdempotencyRecords.CountAsync());Assert.Equal(before.RowVersion,Convert.ToBase64String((await db.AttendanceSummaries.SingleAsync()).RowVersion));}
    }

    [Fact]
    public async Task Attendance_OriginalEventsAndReceipts_AreAppendOnly_OnSyncAndAsyncSave()
    {
        await using var s=await StartAttendanceAsync();await AttInAsync(s);
        await using(var db=sql.CreateContext()){var e=await db.AttendanceEvents.AsTracking().SingleAsync();e.Kind="Tampered";await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync());}
        await using(var db=sql.CreateContext()){var receipt=await db.AttendanceIdempotencyRecords.AsTracking().SingleAsync();receipt.ResponseJson="{}";Assert.Throws<DomainRuleException>(()=>db.SaveChanges());}
        await using(var db=sql.CreateContext()){Assert.Equal("CheckIn",(await db.AttendanceEvents.SingleAsync()).Kind);Assert.NotEqual("{}",(await db.AttendanceIdempotencyRecords.SingleAsync()).ResponseJson);}
    }

    [Fact]
    public async Task Attendance_ReasonValidationAndSanitization_DoNotLeakCredentialText()
    {
        await using var s=await StartAttendanceAsync();var before=await AttCompleteAsync(s);var request=new AdjustAttendanceRequest(before.CheckInUtc,before.CheckOutUtc,"          ",before.RowVersion);
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),request,Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.BadRequest);
        const string marker="BluebirdFixture!478";request=request with {Reason="Support password="+marker};using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),request,Guid.NewGuid()))await AttOkAsync(r);
        await using var db=sql.CreateContext();Assert.DoesNotContain(marker,(await db.AttendanceAuditLogs.SingleAsync()).Reason);Assert.All(await db.AuditEvents.Where(a=>a.Action=="attendance.adjusted").ToListAsync(),a=>Assert.DoesNotContain(marker,a.Reason));
    }

    [Fact]
    public async Task Attendance_HrCannotAdjustOrApproveOvertimeForSelf_EvenWithScope()
    {
        await using var s=await StartAttendanceAsync();var self=await CreateEmployeeProfileAsync(s.Harness,s.Hr,"HR employee","t10-hr-self");await AssignHrScopeAsync(s.Harness,s.Hr.Id,self.Id);
        await using(var scope=s.Harness.Factory.Services.CreateAsyncScope())await scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>().AddToRoleAsync(s.Hr,HrmRoles.Employee);
        using(var r=await AttPostAsync(s.Harness,s.Hr,"/api/v1/attendance/me/check-in",new{},Guid.NewGuid()))await AttOkAsync(r);
        using var completed=await AttPostAsync(s.Harness,s.Hr,"/api/v1/attendance/me/check-out",new{},Guid.NewGuid());var before=await AttOkAsync(completed);
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(self.Id,"/adjust"),new AdjustAttendanceRequest(before.CheckInUtc,before.CheckOutUtc,"Cannot correct my own attendance",before.RowVersion),Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(self.Id,"/overtime"),new{minutes="30",reason="Cannot approve my own overtime",rowVersion=before.RowVersion},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Forbidden);
    }
}

public sealed class AttendanceScenario(SqlDatabaseFixture sql,AuthHarness harness,HrmUser worker,HrmUser hr,HrmUser otherHr,HrmUser payroll,HrmUser admin,EmployeeProfile profile) : IAsyncDisposable
{
    public AuthHarness Harness {get;}=harness;
    public HrmUser Worker {get;}=worker;
    public HrmUser Hr {get;}=hr;
    public HrmUser OtherHr {get;}=otherHr;
    public HrmUser Payroll {get;}=payroll;
    public HrmUser Admin {get;}=admin;
    public EmployeeProfile Profile {get;}=profile;
    public async ValueTask DisposeAsync(){await Harness.DisposeAsync();await sql.ResetAsync();}
}

public sealed record AttendanceFlushObservation(int Summaries,int Events,int Audits);
public sealed class AttendanceFlushProbe
{
    public List<AttendanceFlushObservation> Observations {get;}=[];
}
public sealed class AttendanceAfterFlushAuditRepository(HrmDbContext db,AttendanceFlushProbe probe) : IAuditRepository
{
    public async Task RecordAsync(AuditEvent entry,CancellationToken token)
    {
        db.AuditEvents.Add(entry);
        await db.SaveChangesAsync(token);
        // Query SQL inside the actual transaction, proving writes happened before the injected fault.
        probe.Observations.Add(new(await db.AttendanceSummaries.CountAsync(token),await db.AttendanceEvents.CountAsync(token),await db.AttendanceAuditLogs.CountAsync(token)));
        throw new InvalidOperationException("Synthetic audit failure AFTER SQL mutation flush.");
    }
    public Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters query,CancellationToken token) =>
        Task.FromResult(new PagedResult<AuditEventResponse>([],query.Page,query.PageSize,0));
}
