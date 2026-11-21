using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Application.Payroll;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Payroll;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed record T13Scenario(SqlDatabaseFixture Sql,LeaveScenario Seed,HrmUser Calculator,HrmUser Reviewer,Guid PeriodId) : IAsyncDisposable
{public async ValueTask DisposeAsync(){await Seed.Harness.DisposeAsync();await Sql.ResetAsync();}}
public sealed class T13AuditProbe
{public int Salaries;public int Batches;public int Slips;public int Calls;}
public sealed class T13FaultAudit(HrmDbContext db,T13AuditProbe probe,string action) : IAuditRepository
{
    public async Task RecordAsync(AuditEvent entry,CancellationToken token)
    {
        db.AuditEvents.Add(entry);await db.SaveChangesAsync(token);
        if(entry.Action==action)
        {probe.Calls++;probe.Salaries=await db.SalaryHistoryEntries.CountAsync(token);probe.Batches=await db.PayrollCalculationBatches.CountAsync(token);probe.Slips=await db.Payslips.CountAsync(token);throw new InvalidOperationException("T13 audit fault after actual SQL flush.");}
    }
    public Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters p,CancellationToken t)=>Task.FromResult(new PagedResult<AuditEventResponse>([],p.Page,p.PageSize,0));
}

public sealed partial class FoundationSqlTests
{
    private async Task<T13Scenario> T13StartAsync(decimal quota=12m)
    {
        var seed=await StartLeaveScenarioAsync(quota);seed.Harness.Clock.Advance(DateTimeOffset.Parse("2026-05-10T05:00:00Z")-seed.Harness.Clock.GetUtcNow());
        var calc=await CreateUserWithRolesAsync(seed.Harness,"t13-calc",HrmRoles.Payroll);var reviewer=await CreateUserWithRolesAsync(seed.Harness,"t13-review",HrmRoles.Payroll);
        using(var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiFactory.RepoRoot,"docs/fixtures/work-calendars.demo.json"))))
        {
            var item=doc.RootElement.GetProperty("fixtures").EnumerateArray().Single(x=>x.GetProperty("definition").GetProperty("fixtureId").GetString()=="FIXTURE-CAL-2026-04-A");
            var definition=item.GetProperty("definition").Deserialize<WorkCalendarDefinition>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            using var hr=await BearerClientAsync(seed.Harness,seed.Hr);using var created=await hr.PostAsJsonAsync("/api/v1/work-calendars",new CreateCalendarVersionRequest(new(2026,4,1),definition,"T13 actual T00 calendar fixture"));Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        }
        return new(sql,seed,calc,reviewer,await CalendarPeriodAsync(seed.Harness,"2026-04"));
    }
    private static string T13SalaryRoute(T13Scenario s)=>$"/api/v1/employees/{s.Seed.Profile.Id}/salary-history";
    private static async Task<SalaryHistoryResponse> T13SalaryAsync(T13Scenario s,string amount="12000000",DateOnly? from=null,Guid? key=null)
    {using var r=await AttPostAsync(s.Seed.Harness,s.Calculator,T13SalaryRoute(s),new{effectiveFrom=from??new DateOnly(2026,1,1),monthlyBase=amount,reason="T13 explicit salary decision"},key??Guid.NewGuid());Assert.Equal(HttpStatusCode.Created,r.StatusCode);return (await r.Content.ReadFromJsonAsync<SalaryHistoryResponse>(LeaveJsonOptions))!;}
    private static async Task<PayrollAdjustmentResponse> T13ManualAsync(T13Scenario s,string kind,string amount)
    {using var r=await AttPostAsync(s.Seed.Harness,s.Calculator,$"/api/v1/payroll-periods/{s.PeriodId}/adjustments",new{employeeId=s.Seed.Profile.Id,kind,amount,reason="T13 explicit demo adjustment"},Guid.NewGuid());Assert.Equal(HttpStatusCode.Created,r.StatusCode);return (await r.Content.ReadFromJsonAsync<PayrollAdjustmentResponse>(LeaveJsonOptions))!;}
    private static async Task<PayrollBatchResponse> T13CaptureAsync(T13Scenario s,Guid? key=null,AuthHarness? harness=null)
    {using var r=await AttPostAsync(harness??s.Seed.Harness,s.Calculator,$"/api/v1/payroll-periods/{s.PeriodId}/snapshots",new{reason="T13 capture complete immutable payroll inputs"},key??Guid.NewGuid());Assert.Equal(HttpStatusCode.Created,r.StatusCode);return (await r.Content.ReadFromJsonAsync<PayrollBatchResponse>(LeaveJsonOptions))!;}
    private static async Task<HttpResponseMessage> T13ActionAsync(T13Scenario s,string action,HrmUser? actor=null,AuthHarness? harness=null)
    {using var c=await BearerClientAsync(harness??s.Seed.Harness,actor??(action=="calculate"?s.Calculator:s.Reviewer));var state=await c.GetFromJsonAsync<PayrollWorkflowState>($"/api/v1/payroll-periods/{s.PeriodId}");using var msg=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/payroll-periods/{s.PeriodId}/{action}"){Content=JsonContent.Create(new PayrollActionRequest("T13 verified payroll workflow action",state!.RowVersion,state.RunVersion))};msg.Headers.Add("Idempotency-Key",Guid.NewGuid().ToString());return await c.SendAsync(msg);}
    private async Task T13WorkAsync(T13Scenario s,int? count=null,bool incomplete=false)
    {
        await using var db=sql.CreateContext();var version=await db.WorkCalendarVersions.SingleAsync(x=>x.EffectiveFrom==new DateOnly(2026,4,1));var definition=JsonSerializer.Deserialize<WorkCalendarDefinition>(version.DefinitionJson)!;
        var days=Enumerable.Range(1,30).Select(i=>new DateOnly(2026,4,i)).Where(d=>CalendarMath.StandardMinutes(definition,d)>0m).Take(count??31).ToList();
        foreach(var day in days)
        {
            var checkin=new DateTimeOffset(day.ToDateTime(new TimeOnly(1,30)),TimeSpan.Zero);DateTimeOffset? checkout=incomplete&&day==new DateOnly(2026,4,10)?null:checkin.AddHours(9);
            var row=new AttendanceSummary{Id=Guid.NewGuid(),EmployeeId=s.Seed.Profile.Id,BusinessDate=day,CheckInUtc=checkin,CheckOutUtc=checkout,CalendarVersionId=version.Id,CalendarDefinitionJson=version.DefinitionJson,EligibleMinutes=checkout.HasValue?480m:0m,LateMinutes=0m,EarlyLeaveMinutes=0m,Phase=checkout.HasValue?"Complete":"Open",UpdatedAtUtc=checkout??checkin};db.AttendanceSummaries.Add(row);
            db.AttendanceEvents.Add(new(){Id=Guid.NewGuid(),AttendanceId=row.Id,ActorId=s.Seed.Worker.Id,Kind="CheckIn",OccurredAtUtc=checkin,EffectiveCheckInUtc=checkin});
            if(checkout.HasValue)db.AttendanceEvents.Add(new(){Id=Guid.NewGuid(),AttendanceId=row.Id,ActorId=s.Seed.Worker.Id,Kind="CheckOut",OccurredAtUtc=checkout.Value,EffectiveCheckInUtc=checkin,EffectiveCheckOutUtc=checkout});
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task PayrollSql_SalaryAppend_ClosesPreviousAndConcurrentDateIsUnique()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s,"13200000");
        var body=new{effectiveFrom=new DateOnly(2026,6,15),monthlyBase="17600000",reason="Concurrent future salary decisions"};
        var a=AttPostAsync(h,s.Calculator,T13SalaryRoute(s),body,Guid.NewGuid());var b=AttPostAsync(h,s.Reviewer,T13SalaryRoute(s),body,Guid.NewGuid());await Task.WhenAll(a,b);using var ra=await a;using var rb=await b;Assert.Equal(new[]{201,409},new[]{(int)ra.StatusCode,(int)rb.StatusCode}.Order().ToArray());
        await using var db=sql.CreateContext();var rows=await db.SalaryHistoryEntries.OrderBy(x=>x.EffectiveFrom).ToListAsync();Assert.Equal(2,rows.Count);Assert.Equal(new DateOnly(2026,6,14),rows[0].EffectiveTo);Assert.Null(rows[1].EffectiveTo);
    }
    [Fact]
    public async Task PayrollSql_PermissionBoundary_HrAdminEmployeeCannotReadOrWriteSalary()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;
        foreach(var actor in new[]{s.Seed.Hr,s.Seed.Admin,s.Seed.Worker})
        {
            using(var r=await AttPostAsync(h,actor,T13SalaryRoute(s),new{effectiveFrom="2026-01-01",monthlyBase="12000000",reason="Unauthorized salary decision"},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Forbidden);
            using var c=await BearerClientAsync(h,actor);
            foreach(var route in new[]{T13SalaryRoute(s),$"/api/v1/payroll-periods/{s.PeriodId}/snapshots",$"/api/v1/payroll-periods/{s.PeriodId}/adjustments"})
            {using var r=await c.GetAsync(route);await ProblemAsync(r,HttpStatusCode.Forbidden);}
        }
        await using var db=sql.CreateContext();Assert.Empty(await db.SalaryHistoryEntries.ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_SalaryIdempotency_ConcurrentReplayAndPayloadMismatch()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;var key=Guid.NewGuid();var body=new{effectiveFrom="2026-01-01",monthlyBase="12000000",reason="T13 idempotent salary decision"};
        var a=AttPostAsync(h,s.Calculator,T13SalaryRoute(s),body,key);var b=AttPostAsync(h,s.Calculator,T13SalaryRoute(s),body,key);await Task.WhenAll(a,b);using var ra=await a;using var rb=await b;Assert.Equal(HttpStatusCode.Created,ra.StatusCode);Assert.Equal(HttpStatusCode.Created,rb.StatusCode);Assert.Equal(await ra.Content.ReadAsStringAsync(),await rb.Content.ReadAsStringAsync());
        using(var mismatch=await AttPostAsync(h,s.Calculator,T13SalaryRoute(s),new{effectiveFrom="2026-01-01",monthlyBase="16000000",reason=body.reason},key))await ProblemAsync(mismatch,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Single(await db.SalaryHistoryEntries.ToListAsync());Assert.Single(await db.PayrollInputReceipts.ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_Golden01_ActualFactsAndManuals_CreatePublishedOwnPayslip()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s,"20000000");await T13WorkAsync(s);
        await T13ManualAsync(s,"Allowance","1000000");await T13ManualAsync(s,"DemoInsurance","2100000");await T13ManualAsync(s,"DemoTax","500000");
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using var calc=await BearerClientAsync(h,s.Calculator);var batch=await calc.GetFromJsonAsync<PayrollBatchResponse>($"/api/v1/payroll-periods/{s.PeriodId}/snapshots",LeaveJsonOptions);Assert.NotNull(batch);var result=Assert.Single(batch.Snapshots).Output;Assert.Equal(18400000m,result.Net);Assert.Equal(9600m,result.CreditedMinutes);Assert.Equal(result.Net,result.Lines.Sum(x=>x.Amount));
        using(var r=await T13ActionAsync(s,"approve"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);using(var r=await T13ActionAsync(s,"publish"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using var worker=await BearerClientAsync(h,s.Seed.Worker);var slips=await worker.GetFromJsonAsync<PagedResult<PayslipResponse>>("/api/v1/me/payslips",LeaveJsonOptions);Assert.Equal(18400000m,Assert.Single(slips!.Items).Amount);
        await using var db=sql.CreateContext();Assert.Equal(40,await db.AttendanceEvents.CountAsync());Assert.Single(await db.PayrollCalculationSnapshots.ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_Golden09_LockedSnapshotRemains10740000_AfterFutureSalary()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);await T13WorkAsync(s);var adjustment=await T13ManualAsync(s,"OtherDeduction","1260000"); // Explicit supplemental split; T00 supplies base and locked net only.
        foreach(var action in new[]{"calculate","approve","publish"})using(await T13ActionAsync(s,action)){ }
        await using(var db=sql.CreateContext()){var p=await db.PayrollPeriods.AsTracking().SingleAsync();Assert.Equal("Published",p.Status);p.Status="Locked";await db.SaveChangesAsync();}
        await T13SalaryAsync(s,"16000000",new(2026,5,1));
        using var c=await BearerClientAsync(h,s.Reviewer);var old=await c.GetFromJsonAsync<PayrollBatchResponse>($"/api/v1/payroll-periods/{s.PeriodId}/snapshots?version=1",LeaveJsonOptions);Assert.Equal(10740000m,Assert.Single(old!.Snapshots).Output.Net);Assert.Equal(12000000m,Assert.Single(old.Snapshots[0].Input.Salaries).MonthlyBase);
        using(var r=await AttPostAsync(h,s.Calculator,T13SalaryRoute(s),new{effectiveFrom="2026-04-15",monthlyBase="18000000",reason="T13 reject retroactive locked salary"},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        using(var r=await AttPostAsync(h,s.Calculator,$"/api/v1/payroll-adjustments/{adjustment.Id}/void",new{reason="T13 reject changing locked deduction",rowVersion=adjustment.RowVersion},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        using var worker=await BearerClientAsync(h,s.Seed.Worker);Assert.Equal(10740000m,Assert.Single((await worker.GetFromJsonAsync<PagedResult<PayslipResponse>>("/api/v1/me/payslips",LeaveJsonOptions))!.Items).Amount);
    }
    [Fact]
    public async Task PayrollSql_Golden10_NegativeNetBlocksWorkflow_ThenVoidAndReplaceDeduction()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s,"10000000");await T13WorkAsync(s,4);var manual=await T13ManualAsync(s,"OtherDeduction","3000000");
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var r=await T13ActionAsync(s,"approve"))await ProblemAsync(r,HttpStatusCode.Conflict);
        // Fault-seed an Approved status to independently prove Publish also checks negative data.
        await using(var db=sql.CreateContext()){var p=await db.PayrollPeriods.AsTracking().SingleAsync();p.Status="Approved";p.ApprovedBy=s.Reviewer.Id;await db.SaveChangesAsync();}
        using(var r=await T13ActionAsync(s,"publish"))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using(var db=sql.CreateContext()){var p=await db.PayrollPeriods.AsTracking().SingleAsync();p.Status="Calculated";await db.SaveChangesAsync();}
        using(var r=await AttPostAsync(h,s.Calculator,$"/api/v1/payroll-adjustments/{manual.Id}/void",new{reason="Replace excessive demo deduction",rowVersion=manual.RowVersion},Guid.NewGuid()))Assert.Equal(HttpStatusCode.OK,r.StatusCode);
        await T13ManualAsync(s,"OtherDeduction","2000000");foreach(var action in new[]{"calculate","approve","publish"})using(var r=await T13ActionAsync(s,action))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        await using var verify=sql.CreateContext();Assert.Equal(0m,(await verify.Payslips.SingleAsync()).Amount);var snapshots=await verify.PayrollCalculationSnapshots.ToListAsync();Assert.Equal(2,snapshots.Count);Assert.Contains(snapshots,x=>x.Net==-1000000m&&!x.CanPublish);Assert.Contains(snapshots,x=>x.Net==0m&&x.CanPublish);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PayrollSql_MissingSalaryOrHireDate_RollsBackEntireBatch(bool missingHire)
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);var other=await CreateUserWithRolesAsync(h,"t13-missing",HrmRoles.Employee);var profile=await CreateEmployeeProfileAsync(h,other,"Missing salary employee","T13-missing-profile");
        await using(var db=sql.CreateContext()){var row=await db.EmployeeProfiles.AsTracking().SingleAsync(x=>x.Id==profile.Id);row.HireDate=missingHire?null:new(2026,1,1);await db.SaveChangesAsync();}
        using(var r=await AttPostAsync(h,s.Calculator,$"/api/v1/payroll-periods/{s.PeriodId}/snapshots",new{reason="T13 fail closed on incomplete employee inputs"},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.UnprocessableEntity);
        await using var verify=sql.CreateContext();Assert.Empty(await verify.PayrollCalculationBatches.ToListAsync());Assert.Empty(await verify.Payslips.ToListAsync());Assert.Empty(await verify.CalendarPeriodSnapshots.ToListAsync());Assert.False((await verify.PayrollPeriods.SingleAsync()).SnapshotRequired);
    }
    [Fact]
    public async Task PayrollSql_Golden02_MissingCheckoutBlocksUntilAuditedCorrection()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s,"16000000");await T13WorkAsync(s,incomplete:true);await T13ManualAsync(s,"DemoInsurance","1680000");
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);using(var r=await T13ActionAsync(s,"approve"))await ProblemAsync(r,HttpStatusCode.Conflict);
        AttendanceSummary day;await using(var db=sql.CreateContext())day=await db.AttendanceSummaries.SingleAsync(x=>x.BusinessDate==new DateOnly(2026,4,10));
        using(var r=await AttPostAsync(h,s.Seed.Hr,$"/api/v1/attendance/employees/{s.Seed.Profile.Id}/days/2026-04-10/adjust",new HRM.Next.Application.Attendance.AdjustAttendanceRequest(day.CheckInUtc,day.CheckInUtc.AddHours(9),"T13 confirmed missing checkout from reception record",Convert.ToBase64String(day.RowVersion)),Guid.NewGuid()))await AttOkAsync(r);
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);using(var r=await T13ActionAsync(s,"approve"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        await using var verify=sql.CreateContext();Assert.Equal(14320000m,(await verify.Payslips.SingleAsync()).Amount);Assert.Single(await verify.AttendanceAuditLogs.ToListAsync());Assert.Equal(2,await verify.PayrollCalculationBatches.CountAsync());
    }
    [Fact]
    public async Task PayrollSql_ChangedInputs_BlockBothApproveAndPublish()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);await T13WorkAsync(s);
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);await T13ManualAsync(s,"Allowance","100000");using(var r=await T13ActionAsync(s,"approve")){var p=await ProblemAsync(r,HttpStatusCode.Conflict);Assert.Equal("payroll.inputs_changed",p.GetProperty("errorCode").GetString());}
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);using(var r=await T13ActionAsync(s,"approve"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        await T13ManualAsync(s,"Bonus","100000");using(var r=await T13ActionAsync(s,"publish")){var p=await ProblemAsync(r,HttpStatusCode.Conflict);Assert.Equal("payroll.inputs_changed",p.GetProperty("errorCode").GetString());}
        await using var verify=sql.CreateContext();Assert.Equal("Approved",(await verify.PayrollPeriods.SingleAsync()).Status);Assert.Equal("Draft",(await verify.Payslips.SingleAsync()).Status);
    }
    [Fact]
    public async Task PayrollSql_ImmutabilityGuards_ProtectSnapshotSalaryManualAndPublishedSlip()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);var adjustment=await T13ManualAsync(s,"Allowance","1000");await T13CaptureAsync(s);
        await using(var db=sql.CreateContext()){var x=await db.PayrollCalculationSnapshots.AsTracking().SingleAsync();x.OutputJson="{}";Assert.Equal("payroll.snapshot_immutable",Assert.Throws<DomainRuleException>(()=>db.SaveChanges()).Code);}
        await using(var db=sql.CreateContext()){var x=await db.SalaryHistoryEntries.AsTracking().SingleAsync();x.MonthlyBase=99m;Assert.Equal("payroll.salary_immutable",(await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync())).Code);}
        await using(var db=sql.CreateContext()){var x=await db.PayrollManualAdjustments.AsTracking().SingleAsync();x.Amount=99m;await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync());}
        foreach(var action in new[]{"calculate","approve","publish"})using(var response=await T13ActionAsync(s,action))Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);
        await using(var db=sql.CreateContext()){var x=await db.Payslips.AsTracking().SingleAsync();x.Amount=999999m;await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync());}
    }
    [Fact]
    public async Task PayrollSql_SalaryAuditFailure_RollsBackOldClosureNewRateAndReceipt()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);var probe=new T13AuditProbe();
        await using(var faulty=new AuthHarness(sql.ConnectionString,configure:services=>services.AddScoped<IAuditRepository>(sp=>new T13FaultAudit(sp.GetRequiredService<HrmDbContext>(),probe,"payroll.salary_appended"))))
        {using var r=await AttPostAsync(faulty,s.Calculator,T13SalaryRoute(s),new{effectiveFrom="2026-06-15",monthlyBase="16000000",reason="T13 deliberately failing salary audit"},Guid.NewGuid());await ProblemAsync(r,HttpStatusCode.InternalServerError);}
        Assert.Equal(1,probe.Calls);Assert.Equal(2,probe.Salaries);await using var verify=sql.CreateContext();Assert.Null((await verify.SalaryHistoryEntries.SingleAsync()).EffectiveTo);Assert.Single(await verify.PayrollInputReceipts.ToListAsync());
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task PayrollSql_CaptureAuditFailure_RollsBackCalendarBatchSlipsAndState(bool workflow)
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);var probe=new T13AuditProbe();
        await using(var faulty=new AuthHarness(sql.ConnectionString,configure:services=>services.AddScoped<IAuditRepository>(sp=>new T13FaultAudit(sp.GetRequiredService<HrmDbContext>(),probe,"payroll.snapshot_created"))))
        {using var r=workflow?await T13ActionAsync(s,"calculate",harness:faulty):await AttPostAsync(faulty,s.Calculator,$"/api/v1/payroll-periods/{s.PeriodId}/snapshots",new{reason="T13 deliberately failing snapshot audit"},Guid.NewGuid());await ProblemAsync(r,HttpStatusCode.InternalServerError);}
        Assert.Equal(1,probe.Calls);Assert.Equal(1,probe.Batches);Assert.Equal(1,probe.Slips);await using var verify=sql.CreateContext();Assert.Empty(await verify.PayrollCalculationBatches.ToListAsync());Assert.Empty(await verify.Payslips.ToListAsync());Assert.Empty(await verify.CalendarPeriodSnapshots.ToListAsync());Assert.Equal("Draft",(await verify.PayrollPeriods.SingleAsync()).Status);
    }
    [Fact]
    public async Task PayrollSql_Golden05_InsufficientLeaveSubmission_Is422WithoutMutation()
    {
        await using var s=await T13StartAsync(1m);var h=s.Seed.Harness;
        using(var r=await AttPostAsync(h,s.Seed.Worker,"/api/v1/leave-requests",new{startDate="2026-05-13",endDate="2026-05-15",reason="T00 fixture05 request3 with balance1"},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.UnprocessableEntity);
        await using var db=sql.CreateContext();Assert.Empty(await db.LeaveRecords.ToListAsync());Assert.Equal(1m,(await db.LeaveBalances.SingleAsync()).RemainingDays);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task PayrollSql_Golden06_BothDeterministicApprovalOrders_NeverOverdraw(bool aWins)
    {
        await using var s=await T13StartAsync(2m);var h=s.Seed.Harness;await AssignHrScopeAsync(h,s.Seed.OtherHr.Id,s.Seed.Profile.Id);
        async Task<LeaveRecordResponse> Create(DateOnly start,DateOnly end){using var r=await AttPostAsync(h,s.Seed.Worker,"/api/v1/leave-requests",new{startDate=start,endDate=end,reason="T00 fixture06 deterministic leave"},Guid.NewGuid());Assert.Equal(HttpStatusCode.Created,r.StatusCode);return (await r.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!;}
        var a=await Create(new(2026,6,4),new(2026,6,5));var b=await Create(new(2026,6,18),new(2026,6,18));var first=aWins?a:b;var second=aWins?b:a;
        using(var r=await AttPostAsync(h,s.Seed.Hr,$"/api/v1/leave-requests/{first.Id}/approve",new{reason="T00 fixture06 controlled first commit",rowVersion=first.RowVersion},Guid.NewGuid()))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var r=await AttPostAsync(h,s.Seed.OtherHr,$"/api/v1/leave-requests/{second.Id}/approve",new{reason="T00 fixture06 controlled second request",rowVersion=second.RowVersion},Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Equal(aWins?0m:1m,(await db.LeaveBalances.SingleAsync()).RemainingDays);Assert.Single(await db.LeaveLedgerEntries.Where(x=>x.Action=="Debit").ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_ConcurrentCaptureSameKey_OneBatchAndIdenticalReplay()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);var key=Guid.NewGuid();var a=T13CaptureAsync(s,key);var b=T13CaptureAsync(s,key);await Task.WhenAll(a,b);Assert.Equal((await a).Id,(await b).Id);
        await using var db=sql.CreateContext();Assert.Single(await db.PayrollCalculationBatches.ToListAsync());Assert.Single(await db.Payslips.ToListAsync());Assert.Single(await db.PayrollInputReceipts.Where(x=>x.Operation=="payroll.capture").ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_ExactMoneyContract_RejectsUnknownNumericAndMissingKey()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;using var c=await BearerClientAsync(h,s.Calculator);
        using(var r=await c.PostAsJsonAsync(T13SalaryRoute(s),new{effectiveFrom="2026-01-01",monthlyBase="12000000",reason="T13 missing mandatory key"}))await ProblemAsync(r,HttpStatusCode.BadRequest);
        foreach(var body in new object[]{new{effectiveFrom="2026-01-01",monthlyBase=12000000,reason="T13 numeric money rejected"},new{effectiveFrom="2026-01-01",monthlyBase="12000000.001",reason="T13 precision cannot silently round"},new{effectiveFrom="2026-01-01",monthlyBase="12000000",reason="T13 no forged actor fields",actorId=s.Seed.Worker.Id}})
        {using var r=await AttPostAsync(h,s.Calculator,T13SalaryRoute(s),body,Guid.NewGuid());await ProblemAsync(r,HttpStatusCode.BadRequest);}
        await using(var changed=sql.CreateContext()){var profile=await changed.EmployeeProfiles.AsTracking().SingleAsync();profile.HireDate=new(2027,1,1);await changed.SaveChangesAsync();}
        using(var outside=await AttPostAsync(h,s.Calculator,$"/api/v1/payroll-periods/{s.PeriodId}/adjustments",new{employeeId=s.Seed.Profile.Id,kind="Allowance",amount="1000",reason="T13 cannot silently omit a future employee adjustment"},Guid.NewGuid()))await ProblemAsync(outside,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Empty(await db.SalaryHistoryEntries.ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_CaptureVersusSalaryEdit_IsEntireOldOrNewVersion()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);await T13WorkAsync(s);
        var capture=T13CaptureAsync(s);var edit=T13SalaryAsync(s,"16000000",new(2026,4,15));await Task.WhenAll(capture,edit);
        Assert.Contains(Assert.Single((await capture).Snapshots).Output.StandardPay,new[]{12000000m,14000000m});
        await using var db=sql.CreateContext();Assert.Equal(2,await db.SalaryHistoryEntries.CountAsync());Assert.Single(await db.PayrollCalculationBatches.ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_ReadSnapshotsAndPayslips_AreReadOnly()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);await T13CaptureAsync(s);var observer=new QueryObserver();
        await using var reader=new AuthHarness(sql.ConnectionString,configure:services=>services.AddDbContext<HrmDbContext>(o=>o.AddInterceptors(observer)));using var c=await BearerClientAsync(reader,s.Reviewer);observer.Reset();
        using(var a=await c.GetAsync($"/api/v1/payroll-periods/{s.PeriodId}/snapshots"))Assert.Equal(HttpStatusCode.OK,a.StatusCode);using(var b=await c.GetAsync($"/api/v1/payroll-periods/{s.PeriodId}/items"))Assert.Equal(HttpStatusCode.OK,b.StatusCode);
        Assert.Equal(0,observer.SaveCalls);Assert.DoesNotContain(observer.Commands,x=>x.Contains("INSERT",StringComparison.OrdinalIgnoreCase)||x.Contains("UPDATE",StringComparison.OrdinalIgnoreCase)||x.Contains("DELETE",StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public async Task PayrollSql_SalaryPhysicalConstraints_RejectDuplicateDateSecondOpenAndInvalidRange()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);
        foreach(var range in new[]{(new DateOnly(2026,1,1),(DateOnly?)new DateOnly(2026,1,31)),(new DateOnly(2026,2,1),(DateOnly?)null),(new DateOnly(2026,3,1),(DateOnly?)new DateOnly(2026,2,1))})
        {
            await using var db=sql.CreateContext();db.SalaryHistoryEntries.Add(new(){Id=Guid.NewGuid(),EmployeeId=s.Seed.Profile.Id,EffectiveFrom=range.Item1,EffectiveTo=range.Item2,MonthlyBase=12000000m,Reason="T13 physical constraint negative insertion",CreatedBy=s.Calculator.Id,CreatedAtUtc=h.Clock.GetUtcNow()});
            await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
        }
        await using var verify=sql.CreateContext();Assert.Single(await verify.SalaryHistoryEntries.ToListAsync());
    }
    [Fact]
    public async Task PayrollSql_OffboardedMidMonthEmployee_IsIncludedWithProratedPay()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);await T13WorkAsync(s);
        using var hr=await BearerClientAsync(h,s.Seed.Hr);var profile=await hr.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{s.Seed.Profile.Id}");
        using(var r=await hr.PostAsJsonAsync($"/api/v1/employees/{s.Seed.Profile.Id}/offboard",new OffboardEmployeeRequest(new(2026,4,14),"T13 payroll includes leaver's earned period",profile!.RowVersion!)))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var r=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using var calculator=await BearerClientAsync(h,s.Calculator);var batch=await calculator.GetFromJsonAsync<PayrollBatchResponse>($"/api/v1/payroll-periods/{s.PeriodId}/snapshots",LeaveJsonOptions);var item=Assert.Single(batch!.Snapshots);Assert.Equal(6000000m,item.Output.StandardPay);Assert.Equal(4800m,item.Output.CreditedMinutes);Assert.Equal(new DateOnly(2026,4,14),item.Input.TerminationDate);
        await using var db=sql.CreateContext();Assert.False((await db.Users.SingleAsync(x=>x.Id==s.Seed.Worker.Id)).IsActive);
    }
    [Fact]
    public async Task PayrollSql_DirectSnapshotCapture_RecordsActualCalculatorAndPreventsSelfApproval()
    {
        await using var s=await T13StartAsync();var h=s.Seed.Harness;await T13SalaryAsync(s);
        using(var calculated=await T13ActionAsync(s,"calculate"))Assert.Equal(HttpStatusCode.NoContent,calculated.StatusCode);
        using(var captured=await AttPostAsync(h,s.Reviewer,$"/api/v1/payroll-periods/{s.PeriodId}/snapshots",new{reason="T13 peer recalculates through snapshot endpoint"},Guid.NewGuid()))Assert.Equal(HttpStatusCode.Created,captured.StatusCode);
        using(var own=await T13ActionAsync(s,"approve",s.Reviewer))await ProblemAsync(own,HttpStatusCode.Forbidden);
        using(var other=await T13ActionAsync(s,"approve",s.Calculator))Assert.Equal(HttpStatusCode.NoContent,other.StatusCode);
        await using var db=sql.CreateContext();var p=await db.PayrollPeriods.SingleAsync();Assert.Equal(s.Reviewer.Id,p.CalculatedBy);Assert.Equal(s.Calculator.Id,p.ApprovedBy);Assert.Equal(2,await db.PayrollCalculationBatches.CountAsync());
    }
}
