using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Infrastructure.Attendance;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

// Rendezvous at the first SQL application-lock call. Original code reaches it after
// receipt/status reads; a fixed implementation may lock the receipt before reading.
public sealed class LeaveApprovalBarrier : DbCommandInterceptor
{
    private int arrivals;
    public TaskCompletionSource Both { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (command.CommandText.Contains("sp_getapplock"))
        {
            if (Interlocked.Increment(ref arrivals) == 2) Both.TrySetResult();
            await Both.Task.WaitAsync(TimeSpan.FromSeconds(8), cancellationToken);
        }
        return result;
    }
}

public sealed partial class FoundationSqlTests
{
    private static async Task<LeaveRecordResponse> ReviewCreateLeaveAsync(LeaveScenario s)
    {
        using var c = await BearerClientAsync(s.Harness, s.Worker);
        using var r = await c.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new(2026,6,1),new(2026,6,2),"Codex independent leave review"));
        Assert.Equal(HttpStatusCode.Created,r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!;
    }

    private static async Task<HttpResponseMessage> ReviewApproveAsync(HttpClient client, LeaveRecordResponse leave, Guid key)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Post,$"/api/v1/leave-requests/{leave.Id}/approve")
        {Content=JsonContent.Create(new ApproveLeaveRequest("Codex independent approval",leave.RowVersion))};
        msg.Headers.Add("Idempotency-Key",key.ToString());
        return await client.SendAsync(msg);
    }

    [Fact]
    public async Task LeaveReview_HalfDayPayload_MustNotSilentlyCreateFullDay()
    {
        var s=await StartLeaveScenarioAsync();await using var h=s.Harness;
        using var c=await BearerClientAsync(h,s.Worker);
        using var r=await c.PostAsJsonAsync("/api/v1/leave-requests",new
        {startDate="2026-06-01",endDate="2026-06-01",reason="Half day request",halfDay=true});
        Assert.True(r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Half-day input must be rejected, actual {(int)r.StatusCode}: {await r.Content.ReadAsStringAsync()}");
        await using var db=sql.CreateContext();Assert.Empty(await db.LeaveRecords.ToListAsync());
    }

    [Fact]
    public async Task LeaveReview_ReplayAfterScopeRevocation_MustReauthorize()
    {
        var s=await StartLeaveScenarioAsync();await using var h=s.Harness;
        var leave=await ReviewCreateLeaveAsync(s);var key=Guid.NewGuid();
        using var c=await BearerClientAsync(h,s.Hr);
        using(var first=await ReviewApproveAsync(c,leave,key))Assert.Equal(HttpStatusCode.NoContent,first.StatusCode);
        await using(var db=sql.CreateContext())
        {await db.HrEmployeeAssignments.Where(a=>a.HrUserId==s.Hr.Id && a.EmployeeId==s.Profile.Id).ExecuteDeleteAsync();}
        using var replay=await ReviewApproveAsync(c,leave,key);
        await ProblemAsync(replay,HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task LeaveReview_DisabledEmployee_MustNotReceiveFreshApproval()
    {
        var s=await StartLeaveScenarioAsync();await using var h=s.Harness;
        var leave=await ReviewCreateLeaveAsync(s);
        await using(var db=sql.CreateContext())
        {var u=await db.Users.AsTracking().SingleAsync(u=>u.Id==s.Worker.Id);u.IsActive=false;await db.SaveChangesAsync();}
        using var c=await BearerClientAsync(h,s.Hr);using var r=await ReviewApproveAsync(c,leave,Guid.NewGuid());
        Assert.True(r.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity,
            $"Disabled target must reject new approval, actual {(int)r.StatusCode}");
        await using var verify=sql.CreateContext();Assert.Equal("Pending",(await verify.LeaveRecords.SingleAsync()).Status);
        Assert.Equal(0m,(await verify.LeaveBalances.SingleAsync()).UsedDays);
    }

    [Fact]
    public async Task LeaveReview_PublicationMonthLock_MustSerializeApproval()
    {
        var s=await StartLeaveScenarioAsync();await using var h=s.Harness;
        var leave=await ReviewCreateLeaveAsync(s);using var c=await BearerClientAsync(h,s.Hr);
        await using var publisher=sql.CreateContext();await using var tx=await publisher.Database.BeginTransactionAsync();
        await AttendanceLocks.MonthAsync(publisher,new(2026,6,1),true,CancellationToken.None);
        var approval=ReviewApproveAsync(c,leave,Guid.NewGuid());
        var completedWhilePublicationLockHeld=await Task.WhenAny(approval,Task.Delay(1200))==approval;
        await tx.RollbackAsync();
        using var response=await approval;
        Assert.Equal(HttpStatusCode.NoContent,response.StatusCode);
        Assert.False(completedWhilePublicationLockHeld,"Approval committed while exclusive publication month lock was held.");
    }

    [Fact]
    public async Task LeaveReview_ConcurrentSameKeyApproval_MustReplayWithout500()
    {
        var s=await StartLeaveScenarioAsync();await using var h=s.Harness;
        var leave=await ReviewCreateLeaveAsync(s);var barrier=new LeaveApprovalBarrier();
        await using var racing=new AuthHarness(sql.ConnectionString,configure:services=>
            services.AddDbContext<HrmDbContext>(options=>options.AddInterceptors(barrier)));
        using var c1=await BearerClientAsync(racing,s.Hr);using var c2=await BearerClientAsync(racing,s.Hr);
        var key=Guid.NewGuid();var a=ReviewApproveAsync(c1,leave,key);var b=ReviewApproveAsync(c2,leave,key);
        await Task.WhenAll(a,b);using var ra=await a;using var rb=await b;
        Assert.True(barrier.Both.Task.IsCompletedSuccessfully,"Both requests must reach the employee lock.");
        Assert.Equal(new[]{204,204},new[]{(int)ra.StatusCode,(int)rb.StatusCode}.Order().ToArray());
        await using var db=sql.CreateContext();Assert.Equal(2m,(await db.LeaveBalances.SingleAsync()).UsedDays);
        Assert.Single(await db.LeaveLedgerEntries.Where(e=>e.Action=="Debit").ToListAsync());
    }

    [Fact]
    public async Task LeaveReview_ZeroDayLegacyRequest_MustNotApproveWithoutDebit()
    {
        var s=await StartLeaveScenarioAsync();await using var h=s.Harness;
        var original=await ReviewCreateLeaveAsync(s);
        await using(var db=sql.CreateContext())
        {var row=await db.LeaveRecords.AsTracking().SingleAsync();row.WorkingDays=0m;await db.SaveChangesAsync();}
        using var c=await BearerClientAsync(h,s.Hr);
        using var r=await c.PostAsJsonAsync($"/api/v1/leave-requests/{original.Id}/approve",new ApproveLeaveRequest("Legacy pending request without computed days"));
        Assert.Equal(HttpStatusCode.Conflict,r.StatusCode);
        await using var verify=sql.CreateContext();Assert.Equal("Pending",(await verify.LeaveRecords.SingleAsync()).Status);
        Assert.Empty(await verify.LeaveLedgerEntries.Where(e=>e.Action=="Debit").ToListAsync());
    }
}
