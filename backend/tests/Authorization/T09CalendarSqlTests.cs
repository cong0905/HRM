using System.Net;
using System.Net.Http.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Payroll;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    private async Task<CalendarScenario> CalendarScenarioAsync()
    {
        await sql.ResetAsync();
        return new CalendarScenario(sql,new AuthHarness(sql.ConnectionString));
    }
    private static CreateCalendarVersionRequest CalendarRequest(DateOnly? effective=null) => new(effective ?? new(2026,1,1),
        CalendarMathTests.Definition("FIXTURE-CAL-2026-04-A",new(2026,4,26),new(2026,4,27),new(2026,4,30),new(2026,5,1)),"Calendar fixture setup");
    private static async Task<Guid> CalendarPeriodAsync(AuthHarness harness,string code="2026-04",string status="Draft")
    {
        await using var scope=harness.Factory.Services.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var period=new PayrollPeriodRecord {Id=Guid.NewGuid(),PeriodCode=code,Status=status,CreatedAtUtc=harness.Clock.GetUtcNow()};
        db.PayrollPeriods.Add(period);await db.SaveChangesAsync();return period.Id;
    }

    [Fact]
    public async Task Calendar_VersionEffectiveDates_AndMonthlyFixtures_AreConsistent()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var hr=await CreateUserWithRolesAsync(h,"calendar-hr",HrmRoles.Hr);using var client=await BearerClientAsync(h,hr);
        using(var r=await client.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest())) Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        foreach(var pair in new[]{(4,20),(5,20),(6,22)})
        {
            var result=await client.GetFromJsonAsync<CalendarMonthResponse>($"/api/v1/work-calendars/months/2026/{pair.Item1}");
            Assert.NotNull(result);Assert.Equal(pair.Item2,result.StandardDays);Assert.Equal(pair.Item2*480m,result.StandardMinutes);
        }
        var changed=CalendarRequest(new(2026,6,15));changed=changed with {Definition=changed.Definition with {FixtureId="JUNE-REV2",ShiftEnd=new(16,30)}};
        using(var r=await client.PostAsJsonAsync("/api/v1/work-calendars",changed))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var june=await client.GetFromJsonAsync<CalendarMonthResponse>("/api/v1/work-calendars/months/2026/6");
        Assert.NotNull(june);Assert.Equal(9840m,june.StandardMinutes);Assert.Equal(22,june.StandardDays);
        Assert.Equal(480m,june.Days.Single(d=>d.Date==new DateOnly(2026,6,12)).StandardMinutes);
        Assert.Equal(420m,june.Days.Single(d=>d.Date==new DateOnly(2026,6,15)).StandardMinutes);
    }

    [Fact]
    public async Task Calendar_ConcurrentVersionCreation_OneCreatedOneConflict_AndNoDefaultCoverage()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var hr=await CreateUserWithRolesAsync(h,"calendar-race",HrmRoles.Hr);using var a=await BearerClientAsync(h,hr);using var b=await BearerClientAsync(h,hr);
        using(var missing=await a.GetAsync("/api/v1/work-calendars/days/2026-01-01"))await ProblemAsync(missing,HttpStatusCode.UnprocessableEntity);
        var responses=await Task.WhenAll(a.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()),b.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()));
        try {Assert.Equal(new[]{201,409},responses.Select(r=>(int)r.StatusCode).Order().ToArray());}
        finally{foreach(var r in responses)r.Dispose();}
        await using var db=sql.CreateContext();Assert.Equal(1,await db.WorkCalendarVersions.CountAsync());
        Assert.Equal(1,await db.AuditEvents.CountAsync(a=>a.Action=="calendar.version_created"));
    }

    [Fact]
    public async Task Calendar_Authorization_SeparatesConfigurationFromPayrollAndEmployeeRead()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        using var anon=h.Client();using(var r=await anon.GetAsync("/api/v1/work-calendars/now"))await ProblemAsync(r,HttpStatusCode.Unauthorized);
        var employee=await CreateUserWithRolesAsync(h,"calendar-employee",HrmRoles.Employee);
        var admin=await CreateUserWithRolesAsync(h,"calendar-admin",HrmRoles.SystemAdmin);
        var payroll=await CreateUserWithRolesAsync(h,"calendar-payroll",HrmRoles.Payroll);
        using var emp=await BearerClientAsync(h,employee);using var adm=await BearerClientAsync(h,admin);using var pay=await BearerClientAsync(h,payroll);
        using(var r=await emp.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var r=await pay.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var r=await adm.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        using(var r=await emp.GetAsync("/api/v1/work-calendars/days/2026-04-01"))Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        using(var r=await adm.GetAsync($"/api/v1/work-calendars/payroll-periods/{Guid.NewGuid()}/snapshot"))await ProblemAsync(r,HttpStatusCode.Forbidden);
        using(var r=await emp.GetAsync($"/api/v1/work-calendars/payroll-periods/{Guid.NewGuid()}/snapshot"))await ProblemAsync(r,HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Calendar_PayrollCalculationPinsSnapshot_AndClosedMonthSurvivesFutureVersion()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var hr=await CreateUserWithRolesAsync(h,"calendar-setup",HrmRoles.Hr);using var config=await BearerClientAsync(h,hr);
        using(var r=await config.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var p1=await CreateUserWithRolesAsync(h,"calendar-calc",HrmRoles.Payroll);var p2=await CreateUserWithRolesAsync(h,"calendar-approve",HrmRoles.Payroll);
        using var calc=await BearerClientAsync(h,p1);using var approve=await BearerClientAsync(h,p2);
        var id=await CalendarPeriodAsync(h);
        using(var r=await calc.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/calculate",new PayrollActionRequest("Calculate with calendar")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        var original=await calc.GetFromJsonAsync<CalendarPeriodResponse>($"/api/v1/work-calendars/payroll-periods/{id}/snapshot");
        Assert.NotNull(original);Assert.Equal(9600m,original.Calendar.StandardMinutes);
        using(var r=await approve.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/approve",new PayrollActionRequest("Approve calendar fixture")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var r=await approve.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/publish",new PayrollActionRequest("Publish calendar fixture")))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        var future=CalendarRequest(new(2026,6,1));future=future with {Definition=future.Definition with {FixtureId="FUTURE-420",ShiftEnd=new(16,30)}};
        using(var r=await config.PostAsJsonAsync("/api/v1/work-calendars",future))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var after=await calc.GetFromJsonAsync<CalendarPeriodResponse>($"/api/v1/work-calendars/payroll-periods/{id}/snapshot");
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(original),System.Text.Json.JsonSerializer.Serialize(after));
        using(var r=await config.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest(new(2026,4,15))))await ProblemAsync(r,HttpStatusCode.Conflict);
        using(var r=await calc.PostAsJsonAsync($"/api/v1/work-calendars/payroll-periods/{id}/snapshot",new{}))Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        var june=await config.GetFromJsonAsync<CalendarMonthResponse>("/api/v1/work-calendars/months/2026/6");Assert.Equal(9240m,june!.StandardMinutes);
    }

    [Fact]
    public async Task Calendar_ConcurrentSnapshotCapture_IsDurablyIdempotent()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var hr=await CreateUserWithRolesAsync(h,"calendar-pin-setup",HrmRoles.Hr);using var config=await BearerClientAsync(h,hr);
        using(var r=await config.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var p=await CreateUserWithRolesAsync(h,"calendar-pin",HrmRoles.Payroll);using var a=await BearerClientAsync(h,p);using var b=await BearerClientAsync(h,p);
        var id=await CalendarPeriodAsync(h);
        var responses=await Task.WhenAll(a.PostAsJsonAsync($"/api/v1/work-calendars/payroll-periods/{id}/snapshot",new{}),b.PostAsJsonAsync($"/api/v1/work-calendars/payroll-periods/{id}/snapshot",new{}));
        try{Assert.All(responses,r=>Assert.Equal(HttpStatusCode.OK,r.StatusCode));Assert.Equal(await responses[0].Content.ReadAsStringAsync(),await responses[1].Content.ReadAsStringAsync());}
        finally{foreach(var r in responses)r.Dispose();}
        await using var db=sql.CreateContext();Assert.Equal(1,await db.CalendarPeriodSnapshots.CountAsync());
        Assert.Equal(1,await db.AuditEvents.CountAsync(a=>a.Action=="calendar.snapshot_created"));
    }

    [Fact]
    public async Task Calendar_AuditFailure_RollsBackConfigurationAndCalculationSnapshot()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var failing=new FailingAuditRepository{ShouldFail=true};
        await using var bad=new AuthHarness(sql.ConnectionString,configure:s=>s.AddScoped<IAuditRepository>(_=>failing));
        var hr=await CreateUserWithRolesAsync(h,"calendar-rollback-hr",HrmRoles.Hr);using var fail=await BearerClientAsync(bad,hr);
        using(var r=await fail.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))await ProblemAsync(r,HttpStatusCode.InternalServerError);
        await using(var db=sql.CreateContext())Assert.Equal(0,await db.WorkCalendarVersions.CountAsync());
        using var good=await BearerClientAsync(h,hr);using(var r=await good.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var p=await CreateUserWithRolesAsync(h,"calendar-rollback-pay",HrmRoles.Payroll);using var calc=await BearerClientAsync(bad,p);var id=await CalendarPeriodAsync(h);
        using(var r=await calc.PostAsJsonAsync($"/api/v1/payroll-periods/{id}/calculate",new PayrollActionRequest("Force audit failure")))await ProblemAsync(r,HttpStatusCode.InternalServerError);
        await using(var db=sql.CreateContext()){Assert.Equal(0,await db.CalendarPeriodSnapshots.CountAsync());var period=await db.PayrollPeriods.SingleAsync(x=>x.Id==id);Assert.Equal("Draft",period.Status);Assert.Null(period.CalculatedBy);}
    }

    [Fact]
    public async Task Calendar_ImmutableVersionsAndSnapshots_RejectTrackedMutation()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var hr=await CreateUserWithRolesAsync(h,"calendar-immutable-hr",HrmRoles.Hr);using var config=await BearerClientAsync(h,hr);
        using(var r=await config.PostAsJsonAsync("/api/v1/work-calendars",CalendarRequest()))Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        var p=await CreateUserWithRolesAsync(h,"calendar-immutable-pay",HrmRoles.Payroll);using var pay=await BearerClientAsync(h,p);var id=await CalendarPeriodAsync(h);
        using(var r=await pay.PostAsJsonAsync($"/api/v1/work-calendars/payroll-periods/{id}/snapshot",new{}))Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        await using(var db=sql.CreateContext()){var version=await db.WorkCalendarVersions.AsTracking().SingleAsync();version.DefinitionJson="{}";await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync());}
        await using(var db=sql.CreateContext()){var snapshot=await db.CalendarPeriodSnapshots.AsTracking().SingleAsync();snapshot.ContentJson="{}";await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync(true));}
        await using(var db=sql.CreateContext()){var version=await db.WorkCalendarVersions.AsTracking().SingleAsync();db.Remove(version);Assert.Throws<DomainRuleException>(()=>db.SaveChanges());}
        await using(var db=sql.CreateContext()){Assert.NotEqual("{}",(await db.WorkCalendarVersions.SingleAsync()).DefinitionJson);Assert.NotEqual("{}",(await db.CalendarPeriodSnapshots.SingleAsync()).ContentJson);}
    }

    [Fact]
    public async Task Calendar_InvalidRequestsAndMissingSnapshots_FailClosed()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var hr=await CreateUserWithRolesAsync(h,"calendar-invalid-hr",HrmRoles.Hr);using var config=await BearerClientAsync(h,hr);
        var request=CalendarRequest();
        foreach(var def in new[]{
            request.Definition with {ShiftStart=new(22,0),ShiftEnd=new(6,0)},
            request.Definition with {Breaks=[new(new(12,0),new(13,0)),new(new(12,30),new(13,30))]},
            request.Definition with {Breaks=[null!]},
            request.Definition with {TimeZone="UTC"}
        }) {using var r=await config.PostAsJsonAsync("/api/v1/work-calendars",request with {Definition=def});await ProblemAsync(r,HttpStatusCode.UnprocessableEntity);}
        using(var r=await config.GetAsync("/api/v1/work-calendars/months/2026/13"))await ProblemAsync(r,HttpStatusCode.BadRequest);
        var p=await CreateUserWithRolesAsync(h,"calendar-invalid-pay",HrmRoles.Payroll);using var pay=await BearerClientAsync(h,p);
        using(var r=await pay.GetAsync($"/api/v1/work-calendars/payroll-periods/{Guid.NewGuid()}/snapshot"))await ProblemAsync(r,HttpStatusCode.NotFound);
        using(var r=await pay.PostAsJsonAsync($"/api/v1/work-calendars/payroll-periods/{Guid.NewGuid()}/snapshot",new{}))await ProblemAsync(r,HttpStatusCode.NotFound);
        var closed=await CalendarPeriodAsync(h,"2026-04","Locked");
        using(var r=await pay.PostAsJsonAsync($"/api/v1/work-calendars/payroll-periods/{closed}/snapshot",new{}))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Equal(0,await db.WorkCalendarVersions.CountAsync());Assert.Equal(0,await db.CalendarPeriodSnapshots.CountAsync());
    }

    [Fact]
    public async Task Calendar_Clock_UsesInjectedUtcClock_AndIgnoresClientTime()
    {
        await using var scenario=await CalendarScenarioAsync();var h=scenario.Harness;
        var user=await CreateUserWithRolesAsync(h,"calendar-clock",HrmRoles.Employee);using var client=await BearerClientAsync(h,user);
        var target=DateTimeOffset.Parse("2026-04-01T23:30:00Z");h.Clock.Advance(target-h.Clock.GetUtcNow());
        var response=await client.GetFromJsonAsync<BusinessTimeResponse>("/api/v1/work-calendars/now?serverTimeUtc=2099-01-01");
        Assert.NotNull(response);Assert.Equal(target,response.ServerTimeUtc);Assert.Equal(new DateOnly(2026,4,2),response.BusinessDate);Assert.Equal(CalendarMath.TimeZoneId,response.TimeZone);
    }
}

public sealed class CalendarScenario(SqlDatabaseFixture sql,AuthHarness harness) : IAsyncDisposable
{
    public AuthHarness Harness {get;}=harness;
    public async ValueTask DisposeAsync(){await Harness.DisposeAsync();await sql.ResetAsync();}
}
