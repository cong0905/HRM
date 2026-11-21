using System.Net;
using System.Net.Http.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Payroll;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    private async Task<T13Scenario> T14StartAsync(bool worked=true)
    {var s=await T13StartAsync();await T13SalaryAsync(s);if(worked)await T13WorkAsync(s);return s;}
    private static async Task<PayrollWorkflowState> T14StateAsync(T13Scenario s,HrmUser? actor=null,AuthHarness? h=null)
    {using var c=await BearerClientAsync(h??s.Seed.Harness,actor??s.Calculator);return (await c.GetFromJsonAsync<PayrollWorkflowState>($"/api/v1/payroll-periods/{s.PeriodId}"))!;}
    private static async Task<HttpResponseMessage> T14SendAsync(T13Scenario s,string action,HrmUser? actor=null,
        PayrollWorkflowState? state=null,Guid? key=null,AuthHarness? h=null)
    {
        actor??=action=="calculate"?s.Calculator:s.Reviewer;state??=await T14StateAsync(s,actor,h);
        return await AttPostAsync(h??s.Seed.Harness,actor,$"/api/v1/payroll-periods/{s.PeriodId}/workflow/{action}",new PayrollWorkflowRequest("T14 explicit versioned payroll action",state.RowVersion,state.RunVersion),key??Guid.NewGuid());
    }
    private static async Task T14OkAsync(T13Scenario s,string action,HrmUser? actor=null)
    {using var r=await T14SendAsync(s,action,actor);Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);}

    [Fact]
    public async Task Workflow_CreateRealPeriod_IdempotentAndNoMetadataFallbackWithoutSalary()
    {
        await using var s=await T13StartAsync();var key=Guid.NewGuid();var body=new CreatePayrollPeriodRequest(2026,5,"T14 create real monthly payroll");
        using var a=await AttPostAsync(s.Seed.Harness,s.Calculator,"/api/v1/payroll-periods",body,key);Assert.Equal(HttpStatusCode.Created,a.StatusCode);var state=(await a.Content.ReadFromJsonAsync<PayrollWorkflowState>())!;
        using(var replay=await AttPostAsync(s.Seed.Harness,s.Calculator,"/api/v1/payroll-periods",body,key)){Assert.Equal(HttpStatusCode.Created,replay.StatusCode);Assert.Equal(state.Id,(await replay.Content.ReadFromJsonAsync<PayrollWorkflowState>())!.Id);}
        using(var duplicate=await AttPostAsync(s.Seed.Harness,s.Calculator,"/api/v1/payroll-periods",body,Guid.NewGuid()))await ProblemAsync(duplicate,HttpStatusCode.Conflict);
        using(var r=await AttPostAsync(s.Seed.Harness,s.Calculator,$"/api/v1/payroll-periods/{state.Id}/workflow/calculate",new PayrollWorkflowRequest("T14 missing salary fails closed",state.RowVersion,0),Guid.NewGuid()))await ProblemAsync(r,HttpStatusCode.UnprocessableEntity);
        using var c=await BearerClientAsync(s.Seed.Harness,s.Calculator);using(var legacy=await c.PostAsJsonAsync($"/api/v1/payroll-periods/{state.Id}/calculate",new PayrollActionRequest("T14 no key must not silently calculate")))await ProblemAsync(legacy,HttpStatusCode.BadRequest);
        await using var db=sql.CreateContext();var p=await db.PayrollPeriods.SingleAsync(x=>x.Id==state.Id);Assert.True(p.SnapshotRequired);Assert.Equal("Draft",p.Status);Assert.Empty(await db.PayrollCalculationBatches.ToListAsync());
    }
    [Fact]
    public async Task Workflow_FullLifecycle_BindsRunAndLockedEmployeeCanStillReadPublishedSlip()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");await T14OkAsync(s,"publish");
        var published=await T14StateAsync(s);Assert.Equal(published.ActiveBatchId,published.ApprovedBatchId);Assert.Equal(published.ActiveBatchId,published.PublishedBatchId);
        await T13SalaryAsync(s,"16000000",new(2026,5,1));await T14OkAsync(s,"lock");var locked=await T14StateAsync(s);Assert.Equal("Locked",locked.Status);Assert.NotNull(locked.LockedAtUtc);Assert.Equal(published.PublishedBatchId,locked.PublishedBatchId);
        using var employee=await BearerClientAsync(s.Seed.Harness,s.Seed.Worker);var own=await employee.GetFromJsonAsync<HRM.Next.Application.Common.PagedResult<PayslipResponse>>("/api/v1/me/payslips",LeaveJsonOptions);Assert.Equal(12000000m,Assert.Single(own!.Items).Amount);
    }
    [Fact]
    public async Task Workflow_ConcurrentSameKeyCalculate_CreatesExactlyOneRun()
    {
        await using var s=await T14StartAsync();var state=await T14StateAsync(s);var key=Guid.NewGuid();var a=T14SendAsync(s,"calculate",state:state,key:key);var b=T14SendAsync(s,"calculate",state:state,key:key);await Task.WhenAll(a,b);using var ra=await a;using var rb=await b;Assert.Equal(HttpStatusCode.NoContent,ra.StatusCode);Assert.Equal(HttpStatusCode.NoContent,rb.StatusCode);
        await using var db=sql.CreateContext();Assert.Single(await db.PayrollCalculationBatches.ToListAsync());Assert.Equal(1,await db.PayrollInputReceipts.CountAsync(x=>x.Operation=="workflow.calculate"));Assert.Equal(1,await db.AuditEvents.CountAsync(x=>x.Action=="payroll.workflow_calculate"));
        using var mismatch=await AttPostAsync(s.Seed.Harness,s.Calculator,$"/api/v1/payroll-periods/{s.PeriodId}/workflow/calculate",new PayrollWorkflowRequest("T14 changed reason with same key",state.RowVersion,state.RunVersion),key);await ProblemAsync(mismatch,HttpStatusCode.Conflict);
    }
    [Fact]
    public async Task Workflow_ConcurrentDifferentKeySameVersion_OnlyOneCalculationCommits()
    {
        await using var s=await T14StartAsync();var state=await T14StateAsync(s);var a=T14SendAsync(s,"calculate",state:state);var b=T14SendAsync(s,"calculate",state:state);await Task.WhenAll(a,b);using var ra=await a;using var rb=await b;Assert.Equal(new[]{204,409},new[]{(int)ra.StatusCode,(int)rb.StatusCode}.Order().ToArray());
        await using var db=sql.CreateContext();Assert.Single(await db.PayrollCalculationBatches.ToListAsync());
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task Workflow_DuplicatePublish_OnePublishedRunAndOneAudit(bool sameKey)
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");var state=await T14StateAsync(s);var key=Guid.NewGuid();var a=T14SendAsync(s,"publish",state:state,key:key);var b=T14SendAsync(s,"publish",state:state,key:sameKey?key:Guid.NewGuid());await Task.WhenAll(a,b);using var ra=await a;using var rb=await b;
        Assert.Equal(sameKey?new[]{204,204}:new[]{204,409},new[]{(int)ra.StatusCode,(int)rb.StatusCode}.Order().ToArray());
        await using var db=sql.CreateContext();var p=await db.PayrollPeriods.SingleAsync();Assert.Equal("Published",p.Status);Assert.Equal(p.ActiveBatchId,p.PublishedBatchId);Assert.Single(await db.PayrollCalculationBatches.ToListAsync());Assert.Equal(1,await db.AuditEvents.CountAsync(x=>x.Action=="payroll.workflow_publish"));
    }
    [Fact]
    public async Task Workflow_RecalculateApproved_ClearsOnlyOldApprovalAndOldReplayDoesNotApproveNewRun()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");var beforeApproval=await T14StateAsync(s);var approveKey=Guid.NewGuid();using(var r=await T14SendAsync(s,"approve",state:beforeApproval,key:approveKey))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        var old=await T14StateAsync(s);await T13ManualAsync(s,"Bonus","100000");using(var stale=await T14SendAsync(s,"publish"))await ProblemAsync(stale,HttpStatusCode.Conflict);
        await T14OkAsync(s,"calculate");var newer=await T14StateAsync(s);Assert.Equal(2,newer.RunVersion);Assert.Equal("Calculated",newer.Status);Assert.Null(newer.ApprovedBatchId);Assert.Null(newer.ApprovedBy);Assert.NotEqual(old.ActiveBatchId,newer.ActiveBatchId);
        using(var oldReplay=await T14SendAsync(s,"approve",state:beforeApproval,key:approveKey))Assert.Equal(HttpStatusCode.NoContent,oldReplay.StatusCode);Assert.Null((await T14StateAsync(s)).ApprovedBatchId);
        await T14OkAsync(s,"approve");await T14OkAsync(s,"publish");await using var db=sql.CreateContext();var outputs=await db.PayrollCalculationSnapshots.OrderBy(x=>x.Net).ToListAsync();Assert.Equal(new[]{12000000m,12100000m},outputs.Select(x=>x.Net).ToArray());
    }
    [Fact]
    public async Task Workflow_RecalculateAuditFault_RestoresApprovedRunAndAllAmounts()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");await T13ManualAsync(s,"Bonus","100000");var before=await T14StateAsync(s);var probe=new T13AuditProbe();
        await using(var faulty=new AuthHarness(sql.ConnectionString,configure:services=>services.AddScoped<IAuditRepository>(sp=>new T13FaultAudit(sp.GetRequiredService<HrmDbContext>(),probe,"payroll.snapshot_created"))))
        {using var r=await T14SendAsync(s,"calculate",state:before,h:faulty);await ProblemAsync(r,HttpStatusCode.InternalServerError);}
        Assert.Equal(2,probe.Batches);Assert.Equal(1,probe.Slips);var restored=await T14StateAsync(s);Assert.Equal(before,restored);
        await using(var db=sql.CreateContext()){Assert.Single(await db.PayrollCalculationBatches.ToListAsync());Assert.Equal(12000000m,(await db.Payslips.SingleAsync()).Amount);}
        await T14OkAsync(s,"calculate");Assert.Equal(2,(await T14StateAsync(s)).RunVersion);
    }
    [Fact]
    public async Task Workflow_OneMissingEmployee_DoesNotReplaceAnyPartOfApprovedRun()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");var before=await T14StateAsync(s);var worker=await CreateUserWithRolesAsync(s.Seed.Harness,"t14-missing",HrmRoles.Employee);var profile=await CreateEmployeeProfileAsync(s.Seed.Harness,worker,"T14 no salary","T14-no-salary");
        await using(var db=sql.CreateContext()){var p=await db.EmployeeProfiles.AsTracking().SingleAsync(x=>x.Id==profile.Id);p.HireDate=new(2026,1,1);await db.SaveChangesAsync();}
        using(var failed=await T14SendAsync(s,"calculate"))await ProblemAsync(failed,HttpStatusCode.UnprocessableEntity);Assert.Equal(before,await T14StateAsync(s));
        await using var verify=sql.CreateContext();Assert.Single(await verify.PayrollCalculationBatches.ToListAsync());Assert.Single(await verify.Payslips.ToListAsync());Assert.Equal(12000000m,(await verify.Payslips.SingleAsync()).Amount);
    }
    [Fact]
    public async Task Workflow_PublishAuditFault_RollsBackSlipsMetadataAndReceiptThenRetrySucceeds()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");var before=await T14StateAsync(s);var key=Guid.NewGuid();var probe=new T13AuditProbe();
        await using(var faulty=new AuthHarness(sql.ConnectionString,configure:services=>services.AddScoped<IAuditRepository>(sp=>new T13FaultAudit(sp.GetRequiredService<HrmDbContext>(),probe,"payroll.workflow_publish"))))
        {using var failed=await T14SendAsync(s,"publish",state:before,key:key,h:faulty);await ProblemAsync(failed,HttpStatusCode.InternalServerError);}
        Assert.Equal(1,probe.Calls);Assert.Equal(1,probe.Slips);Assert.Equal(before,await T14StateAsync(s));
        await using(var db=sql.CreateContext()){Assert.Equal("Draft",(await db.Payslips.SingleAsync()).Status);Assert.Equal(0,await db.PayrollInputReceipts.CountAsync(x=>x.Operation=="workflow.publish"));}
        using var retry=await T14SendAsync(s,"publish",state:before,key:key);Assert.Equal(HttpStatusCode.NoContent,retry.StatusCode);
    }
    [Fact]
    public async Task Workflow_LostCompletionAfterPublishCommit_ReplaysWithoutSecondPublication()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");var before=await T14StateAsync(s);var key=Guid.NewGuid();var fault=new T12CommitFault(true);
        await using(var faulty=new AuthHarness(sql.ConnectionString,configure:services=>services.AddDbContext<HrmDbContext>(o=>o.AddInterceptors(fault))))
        {using var client=await BearerClientAsync(faulty,s.Reviewer);using var msg=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/payroll-periods/{s.PeriodId}/workflow/publish"){Content=JsonContent.Create(new PayrollWorkflowRequest("T14 explicit versioned payroll action",before.RowVersion,before.RunVersion))};msg.Headers.Add("Idempotency-Key",key.ToString());fault.Arm();using var r=await client.SendAsync(msg);await ProblemAsync(r,HttpStatusCode.InternalServerError);}
        Assert.Equal(1,fault.Fired);Assert.Equal("Published",(await T14StateAsync(s)).Status);using(var retry=await T14SendAsync(s,"publish",state:before,key:key))Assert.Equal(HttpStatusCode.NoContent,retry.StatusCode);
        await using var db=sql.CreateContext();Assert.Equal(1,await db.AuditEvents.CountAsync(x=>x.Action=="payroll.workflow_publish"));Assert.Single(await db.PayrollCalculationBatches.ToListAsync());
    }
    [Theory][InlineData("salary",false)][InlineData("salary",true)][InlineData("attendance",false)][InlineData("attendance",true)][InlineData("leave",false)][InlineData("leave",true)]
    public async Task Workflow_SourceChanges_BlockApproveOrPublishUntilNewCalculation(string source,bool afterApproval)
    {
        await using var s=await T14StartAsync(source!="leave");await T14OkAsync(s,"calculate");if(afterApproval)await T14OkAsync(s,"approve");
        if(source=="salary")await T13SalaryAsync(s,"16000000",new(2026,4,15));
        else if(source=="attendance")
        {await using var db=sql.CreateContext();var day=await db.AttendanceSummaries.SingleAsync(x=>x.BusinessDate==new DateOnly(2026,4,1));using var r=await AttPostAsync(s.Seed.Harness,s.Seed.Hr,$"/api/v1/attendance/employees/{s.Seed.Profile.Id}/days/2026-04-01/adjust",new HRM.Next.Application.Attendance.AdjustAttendanceRequest(day.CheckInUtc.AddMinutes(90),day.CheckOutUtc,"T14 source changed after payroll calculation",Convert.ToBase64String(day.RowVersion)),Guid.NewGuid());await AttOkAsync(r);}
        else
        {using var created=await AttPostAsync(s.Seed.Harness,s.Seed.Worker,"/api/v1/leave-requests",new{startDate="2026-04-01",endDate="2026-04-01",reason="T14 leave source changed after calculation"},Guid.NewGuid());Assert.Equal(HttpStatusCode.Created,created.StatusCode);var leave=(await created.Content.ReadFromJsonAsync<HRM.Next.Application.Leave.LeaveRecordResponse>(LeaveJsonOptions))!;using var approved=await AttPostAsync(s.Seed.Harness,s.Seed.Hr,$"/api/v1/leave-requests/{leave.Id}/approve",new{reason="T14 HR explicitly approves new paid leave",rowVersion=leave.RowVersion},Guid.NewGuid());Assert.Equal(HttpStatusCode.NoContent,approved.StatusCode);}
        using var blocked=await T14SendAsync(s,afterApproval?"publish":"approve");var problem=await ProblemAsync(blocked,HttpStatusCode.Conflict);Assert.Equal("payroll.inputs_changed",problem.GetProperty("errorCode").GetString());
    }
    [Fact]
    public async Task Workflow_CalculateVersusSalaryEdit_ProducesWholeVersionAndStaleCheckIsEnforced()
    {
        await using var s=await T14StartAsync();var state=await T14StateAsync(s);var calc=T14SendAsync(s,"calculate",state:state);var edit=T13SalaryAsync(s,"16000000",new(2026,4,15));await Task.WhenAll(calc,edit);using var calculated=await calc;Assert.Equal(HttpStatusCode.NoContent,calculated.StatusCode);
        using var c=await BearerClientAsync(s.Seed.Harness,s.Reviewer);var batch=(await c.GetFromJsonAsync<PayrollBatchResponse>($"/api/v1/payroll-periods/{s.PeriodId}/snapshots",LeaveJsonOptions))!;var pay=Assert.Single(batch.Snapshots).Output.StandardPay;Assert.Contains(pay,new[]{12000000m,14000000m});
        using var approve=await T14SendAsync(s,"approve");Assert.Equal(pay==14000000m?HttpStatusCode.NoContent:HttpStatusCode.Conflict,approve.StatusCode);
    }
    [Theory][InlineData("approve","Draft")][InlineData("publish","Draft")][InlineData("lock","Draft")][InlineData("calculate","Published")][InlineData("approve","Published")][InlineData("publish","Locked")]
    public async Task Workflow_InvalidTransitions_FailWithoutAnotherRun(string action,string status)
    {
        await using var s=await T14StartAsync();if(status!="Draft"){await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");await T14OkAsync(s,"publish");if(status=="Locked")await T14OkAsync(s,"lock");}
        using var failed=await T14SendAsync(s,action);await ProblemAsync(failed,HttpStatusCode.Conflict);Assert.Equal(status,(await T14StateAsync(s)).Status);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)]
    public async Task Workflow_InvalidVersionOrKey_IsRejectedWithoutRun(int variant)
    {
        await using var s=await T14StartAsync();var state=await T14StateAsync(s);using var c=await BearerClientAsync(s.Seed.Harness,s.Calculator);using var message=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/payroll-periods/{s.PeriodId}/workflow/calculate"){Content=JsonContent.Create(new PayrollWorkflowRequest("T14 malformed or stale command",variant==0?Convert.ToBase64String(new byte[8]):state.RowVersion,variant==1?1:0))};
        if(variant!=2)message.Headers.Add("Idempotency-Key",variant==3?"not-a-uuid":Guid.NewGuid().ToString());using var failed=await c.SendAsync(message);await ProblemAsync(failed,variant is 0 or 1?HttpStatusCode.Conflict:HttpStatusCode.BadRequest);
        await using var db=sql.CreateContext();Assert.Empty(await db.PayrollCalculationBatches.ToListAsync());
    }
    [Fact]
    public async Task Workflow_SeparationOfDuties_CalculatorCannotApproveOrPublishOwnRun()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");using(var ownApprove=await T14SendAsync(s,"approve",s.Calculator))await ProblemAsync(ownApprove,HttpStatusCode.Forbidden);await T14OkAsync(s,"approve");using(var ownPublish=await T14SendAsync(s,"publish",s.Calculator))await ProblemAsync(ownPublish,HttpStatusCode.Forbidden);
    }
    [Theory][InlineData("Employee")][InlineData("HR")][InlineData("SystemAdmin")]
    public async Task Workflow_PermissionBoundary_DeniesNonPayrollActors(string role)
    {
        await using var s=await T14StartAsync();var actor=role=="Employee"?s.Seed.Worker:role=="HR"?s.Seed.Hr:s.Seed.Admin;var state=await T14StateAsync(s);
        foreach(var action in new[]{"calculate","approve","publish","lock"})using(var deniedAction=await T14SendAsync(s,action,actor,state)){await ProblemAsync(deniedAction,HttpStatusCode.Forbidden);}
        using var denied=await T14SendAsync(s,"calculate",actor,state);await ProblemAsync(denied,HttpStatusCode.Forbidden);using var c=await BearerClientAsync(s.Seed.Harness,actor);using var view=await c.GetAsync($"/api/v1/payroll-periods/{s.PeriodId}");await ProblemAsync(view,HttpStatusCode.Forbidden);
        await using var db=sql.CreateContext();Assert.Empty(await db.PayrollCalculationBatches.ToListAsync());
    }
    [Fact]
    public async Task Workflow_GetStateItemsAndPublishedOwnProjection_DoNotWriteOrExposeOtherPayslips()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");await T14OkAsync(s,"publish");await T14OkAsync(s,"lock");var observer=new QueryObserver();
        await using var reader=new AuthHarness(sql.ConnectionString,configure:services=>services.AddDbContext<HrmDbContext>(o=>o.AddInterceptors(observer)));using var finance=await BearerClientAsync(reader,s.Reviewer);using var owner=await BearerClientAsync(reader,s.Seed.Worker);observer.Reset();
        foreach(var route in new[]{$"/api/v1/payroll-periods/{s.PeriodId}",$"/api/v1/payroll-periods/{s.PeriodId}/items"}){using var response=await finance.GetAsync(route);Assert.Equal(HttpStatusCode.OK,response.StatusCode);}
        using(var own=await owner.GetAsync("/api/v1/me/payslips"))Assert.Equal(HttpStatusCode.OK,own.StatusCode);Assert.Equal(0,observer.SaveCalls);Assert.DoesNotContain(observer.Commands,x=>x.Contains("INSERT",StringComparison.OrdinalIgnoreCase)||x.Contains("UPDATE",StringComparison.OrdinalIgnoreCase)||x.Contains("DELETE",StringComparison.OrdinalIgnoreCase));
        var other=await CreateUserWithRolesAsync(s.Seed.Harness,"t14-other",HrmRoles.Employee);await CreateEmployeeProfileAsync(s.Seed.Harness,other,"Another employee","T14-other-id");using var outsider=await BearerClientAsync(s.Seed.Harness,other);var page=await outsider.GetFromJsonAsync<HRM.Next.Application.Common.PagedResult<PayslipResponse>>("/api/v1/me/payslips",LeaveJsonOptions);Assert.Empty(page!.Items);
    }
    [Fact]
    public async Task Workflow_PublishedAndLockedPeriodGuards_BlockTrackedFinancialMutation()
    {
        await using var s=await T14StartAsync();await T14OkAsync(s,"calculate");await T14OkAsync(s,"approve");await T14OkAsync(s,"publish");
        await using(var db=sql.CreateContext()){var p=await db.PayrollPeriods.AsTracking().SingleAsync();p.ActiveBatchId=null;Assert.Equal("payroll.period_immutable",Assert.Throws<DomainRuleException>(()=>db.SaveChanges()).Code);}
        await T14OkAsync(s,"lock");await using(var db=sql.CreateContext()){var p=await db.PayrollPeriods.AsTracking().SingleAsync();p.Status="Draft";await Assert.ThrowsAsync<DomainRuleException>(()=>db.SaveChangesAsync());}
    }
}
