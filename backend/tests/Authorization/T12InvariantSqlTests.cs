using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Common;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Leave;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Leave;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class T12CommitFault(bool afterCommit) : DbTransactionInterceptor
{
    private int armed;
    public int ObservedEvents { get; private set; }
    public int ObservedDebits { get; private set; }
    public int ObservedReceipts { get; private set; }
    public int Fired { get; private set; }
    public void Arm() => Interlocked.Exchange(ref armed,1);
    public override async ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction tx,
        TransactionEventData data,InterceptionResult result,CancellationToken token=default)
    {
        if(afterCommit || Interlocked.Exchange(ref armed,0)==0)return result;
        await using var c=tx.Connection!.CreateCommand();c.Transaction=tx;
        c.CommandText="SELECT (SELECT COUNT(*) FROM attendance.AttendanceEvents),(SELECT COUNT(*) FROM personnel.LeaveLedgerEntries WHERE Action=N'Debit'),(SELECT COUNT(*) FROM attendance.AttendanceIdempotencyRecords)+(SELECT COUNT(*) FROM personnel.LeaveIdempotencyRecords)";
        await using var reader=await c.ExecuteReaderAsync(token);Assert.True(await reader.ReadAsync(token));
        ObservedEvents=reader.GetInt32(0);ObservedDebits=reader.GetInt32(1);ObservedReceipts=reader.GetInt32(2);
        Fired++;throw new InvalidOperationException("T12 seeded failure immediately before SQL commit.");
    }
    public override Task TransactionCommittedAsync(DbTransaction tx,TransactionEndEventData data,CancellationToken token=default)
    {
        if(afterCommit && Interlocked.Exchange(ref armed,0)==1)
        {Fired++;throw new InvalidOperationException("T12 seeded lost completion after SQL commit.");}
        return Task.CompletedTask;
    }
}

public sealed class T12AuditFault(HrmDbContext db) : IAuditRepository
{
    public async Task RecordAsync(AuditEvent entry,CancellationToken token)
    {
        // LeaveService already flushed status/balance/debit before this call.
        // Observe via SQL without another SaveChanges, so an early commit mutant
        // is distinguishable from a rollback that truly protects the mutation.
        if(entry.Action=="leaves.approved")
            throw new InvalidOperationException("T12 audit unavailable after leave mutation flush.");
        db.AuditEvents.Add(entry);await db.SaveChangesAsync(token);
    }
    public Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters query,CancellationToken token)=>
        Task.FromResult(new PagedResult<AuditEventResponse>([],query.Page,query.PageSize,0));
}

public sealed partial class FoundationSqlTests
{
    // Reproducible request seeds; never production IDs or secrets.
    private static Guid T12Key(string name)=>new(SHA256.HashData(Encoding.UTF8.GetBytes("HRM-T12-seed-12026:"+name))[..16]);
    private static Guid T12V4(string name)
    {
        var b=T12Key(name).ToByteArray();b[7]=(byte)((b[7]&15)|64);b[8]=(byte)((b[8]&63)|128);return new(b);
    }
    private static readonly DateOnly T12Day=new(2026,11,12);
    private static async Task T12GrantAsync(AttendanceScenario s)
    {
        using var r=await AttPostAsync(s.Harness,s.Hr,"/api/v1/leave-balances/grant",new{employeeId=s.Profile.Id,year=2026,days="12",reason="T12 explicit seeded balance"},T12V4("grant"));
        Assert.Equal(HttpStatusCode.Created,r.StatusCode);
    }
    private static async Task<LeaveRecordResponse> T12LeaveAsync(AttendanceScenario s,DateOnly start,DateOnly? end=null,Guid? key=null)
    {
        using var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/leave-requests",new{startDate=start,endDate=end??start,reason="T12 seeded leave request"},key??T12V4("create"));
        Assert.Equal(HttpStatusCode.Created,r.StatusCode);return (await r.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!;
    }
    private static Task<HttpResponseMessage> T12ApproveAsync(AttendanceScenario s,LeaveRecordResponse leave,Guid? key=null,AuthHarness? h=null)=>
        AttPostAsync(h??s.Harness,s.Hr,$"/api/v1/leave-requests/{leave.Id}/approve",new{reason="T12 seeded approval",rowVersion=leave.RowVersion},key??T12V4("approve"));

    [Fact]
    public async Task T12_ScopedKeys_CrossActorAndOperation_CanonicalHashMismatch()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var key=T12V4("shared");
        var leave=await T12LeaveAsync(s,T12Day.AddDays(1),key:key);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key))await AttOkAsync(r);
        s.Harness.Clock.Advance(TimeSpan.FromHours(9));
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},key))await AttOkAsync(r);
        using(var r=await T12ApproveAsync(s,leave,key))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        // Same key and operation for another actor must not replay the first worker's record.
        var other=await CreateUserWithRolesAsync(s.Harness,"t12-other-key",HrmRoles.Employee);
        var profile=await CreateEmployeeProfileAsync(s.Harness,other,"T12 other","T12-other-id");
        using(var r=await AttPostAsync(s.Harness,other,"/api/v1/attendance/me/check-in",new{},key))
        {var day=await AttOkAsync(r);Assert.Equal(profile.Id,day.EmployeeId);Assert.NotEqual(s.Profile.Id,day.EmployeeId);}
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{note="changed canonical body"},key))await ProblemAsync(r,HttpStatusCode.Conflict);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/leave-requests",new{reason="T12 seeded leave request",endDate=T12Day.AddDays(1),startDate=T12Day.AddDays(1)},key))
        {Assert.Equal(HttpStatusCode.Created,r.StatusCode);Assert.Equal(leave.Id,(await r.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!.Id);}
        await using var db=sql.CreateContext();Assert.Equal(3,await db.AttendanceIdempotencyRecords.CountAsync());Assert.Equal(1,await db.LeaveLedgerEntries.CountAsync(l=>l.Action=="Debit"));
    }

    [Fact]
    public async Task T12_Retention_RestartAnd400DayRetry_KeepOriginalReceipt()
    {
        await using var s=await StartAttendanceAsync();var key=T12V4("old-receipt");
        using var first=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key);var original=await AttOkAsync(first);
        s.Harness.Clock.Advance(TimeSpan.FromDays(400));
        await using var restart=new AuthHarness(sql.ConnectionString);restart.Clock.Advance(s.Harness.Clock.GetUtcNow()-restart.Clock.GetUtcNow());
        using var retry=await AttPostAsync(restart,s.Worker,"/api/v1/attendance/me/check-in",new{},key);Assert.Equal(original,await AttOkAsync(retry));
        using(var mismatch=await AttPostAsync(restart,s.Worker,"/api/v1/attendance/me/check-in",new{note="different body after400days"},key))await ProblemAsync(mismatch,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Single(await db.AttendanceEvents.ToListAsync());Assert.Equal(T12Day,(await db.AttendanceSummaries.SingleAsync()).BusinessDate);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task T12_FaultAtCommit_Attendance_RollbackOrDurableReplay(bool afterCommit)
    {
        await using var s=await StartAttendanceAsync();var fault=new T12CommitFault(afterCommit);var key=T12V4("att-commit-fault");
        await using(var injected=new AuthHarness(sql.ConnectionString,configure:services=>services.AddDbContext<HrmDbContext>(o=>o.AddInterceptors(fault))))
        {
            injected.Clock.Advance(s.Harness.Clock.GetUtcNow()-injected.Clock.GetUtcNow());using var client=await BearerClientAsync(injected,s.Worker);
            fault.Arm();using var msg=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-in"){Content=JsonContent.Create(new{})};msg.Headers.Add("Idempotency-Key",key.ToString());
            using var failed=await client.SendAsync(msg);await ProblemAsync(failed,HttpStatusCode.InternalServerError);Assert.Equal(1,fault.Fired);
        }
        await using(var db=sql.CreateContext())
        {Assert.Equal(afterCommit?1:0,await db.AttendanceEvents.CountAsync());Assert.Equal(afterCommit?1:0,await db.AttendanceIdempotencyRecords.CountAsync());Assert.Equal(afterCommit?1:0,await db.AttendanceSummaries.CountAsync());}
        if(!afterCommit){Assert.Equal(1,fault.ObservedEvents);Assert.True(fault.ObservedReceipts>=1);}
        using(var retry=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},key))await AttOkAsync(retry);
        await using var verify=sql.CreateContext();Assert.Single(await verify.AttendanceEvents.ToListAsync());Assert.Single(await verify.AttendanceIdempotencyRecords.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task T12_FaultAtCommit_Leave_RollbackOrDurableReplay(bool afterCommit)
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day);var key=T12V4("leave-commit-fault");var fault=new T12CommitFault(afterCommit);
        await using(var injected=new AuthHarness(sql.ConnectionString,configure:services=>services.AddDbContext<HrmDbContext>(o=>o.AddInterceptors(fault))))
        {
            injected.Clock.Advance(s.Harness.Clock.GetUtcNow()-injected.Clock.GetUtcNow());using var client=await BearerClientAsync(injected,s.Hr);
            using var msg=new HttpRequestMessage(HttpMethod.Post,$"/api/v1/leave-requests/{leave.Id}/approve"){Content=JsonContent.Create(new{reason="T12 seeded approval",rowVersion=leave.RowVersion})};msg.Headers.Add("Idempotency-Key",key.ToString());
            fault.Arm();using var failed=await client.SendAsync(msg);await ProblemAsync(failed,HttpStatusCode.InternalServerError);Assert.Equal(1,fault.Fired);
        }
        await using(var db=sql.CreateContext())
        {Assert.Equal(afterCommit?1m:0m,(await db.LeaveBalances.SingleAsync()).UsedDays);Assert.Equal(afterCommit?"Approved":"Pending",(await db.LeaveRecords.SingleAsync()).Status);Assert.Equal(afterCommit?1:0,await db.LeaveLedgerEntries.CountAsync(e=>e.Action=="Debit"));}
        if(!afterCommit){Assert.Equal(1,fault.ObservedDebits);Assert.True(fault.ObservedReceipts>=3);}
        using(var retry=await T12ApproveAsync(s,leave,key))Assert.Equal(HttpStatusCode.NoContent,retry.StatusCode);
        await using var verify=sql.CreateContext();Assert.Equal(1m,(await verify.LeaveBalances.SingleAsync()).UsedDays);Assert.Single(await verify.LeaveLedgerEntries.Where(e=>e.Action=="Debit").ToListAsync());
    }

    [Fact]
    public async Task T12_ApprovedFullDayLeave_BlocksCheckIn()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day);
        using(var r=await T12ApproveAsync(s,leave))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var r=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},T12V4("leave-punch")))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Empty(await db.AttendanceEvents.ToListAsync());Assert.Equal(1m,(await db.LeaveBalances.SingleAsync()).UsedDays);
    }

    [Fact]
    public async Task T12_PendingLeaveAllowsWork_ApprovalRequiresZeroCorrection_ThenBlocksPaidWorkAndOt()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day);var worked=await AttCompleteAsync(s);
        using(var r=await T12ApproveAsync(s,leave))await ProblemAsync(r,HttpStatusCode.Conflict);
        var zero=DateTimeOffset.Parse("2026-11-12T00:00:00Z");
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(zero,zero,"T12 remove paid work before leave approval",worked.RowVersion),T12V4("zero-correction")))worked=await AttOkAsync(r);
        Assert.Equal(0m,worked.EligibleMinutes);
        using(var r=await T12ApproveAsync(s,leave))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/adjust"),new AdjustAttendanceRequest(DateTimeOffset.Parse("2026-11-12T01:30:00Z"),DateTimeOffset.Parse("2026-11-12T10:30:00Z"),"T12 must not restore double paid minutes",worked.RowVersion),T12V4("paid-correction")))await ProblemAsync(r,HttpStatusCode.Conflict);
        using(var r=await AttPostAsync(s.Harness,s.Hr,AttDay(s.Profile.Id,"/overtime"),new{minutes="30",reason="T12 no overtime on full day paid leave",rowVersion=worked.RowVersion},T12V4("leave-ot")))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Equal(0m,(await db.AttendanceSummaries.SingleAsync()).EligibleMinutes);Assert.Equal(1m,(await db.LeaveBalances.SingleAsync()).UsedDays);Assert.Empty(await db.AttendanceOvertimeEntries.ToListAsync());Assert.Equal(3,await db.AttendanceEvents.CountAsync());
    }

    [Fact]
    public async Task T12_CheckInVersusApprove_Seeded10Races_ExactlyOneWins()
    {
        for(var seed=0;seed<10;seed++)
        {
            await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day);
            var punch=AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-in",new{},T12V4("race-punch-"+seed));var approve=T12ApproveAsync(s,leave,T12V4("race-approve-"+seed));
            await Task.WhenAll(punch,approve);using var p=await punch;using var a=await approve;
            Assert.True((p.StatusCode==HttpStatusCode.OK && a.StatusCode==HttpStatusCode.Conflict)||(p.StatusCode==HttpStatusCode.Conflict && a.StatusCode==HttpStatusCode.NoContent),$"seed{seed}: checkin={(int)p.StatusCode},approve={(int)a.StatusCode}");
            await using var db=sql.CreateContext();var approved=(await db.LeaveRecords.SingleAsync()).Status=="Approved";
            Assert.Equal(approved?0:1,await db.AttendanceEvents.CountAsync());Assert.Equal(approved?1m:0m,(await db.LeaveBalances.SingleAsync()).UsedDays);
        }
    }

    [Fact]
    public async Task T12_NonWorkingDayWithinApprovedSpan_EarnsNoStandardOrAutomaticOt()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,new(2026,11,13),new(2026,11,16));Assert.Equal(2m,leave.WorkingDays);
        using(var r=await T12ApproveAsync(s,leave))Assert.Equal(HttpStatusCode.NoContent,r.StatusCode);
        s.Harness.Clock.Advance(TimeSpan.FromDays(2));await AttInAsync(s);s.Harness.Clock.Advance(TimeSpan.FromHours(10));
        using var r2=await AttPostAsync(s.Harness,s.Worker,"/api/v1/attendance/me/check-out",new{},T12V4("weekend-out"));var day=await AttOkAsync(r2);Assert.Equal(0m,day.EligibleMinutes);Assert.Equal(0m,day.ConfirmedOvertimeMinutes);
        await using var db=sql.CreateContext();Assert.Equal(2m,(await db.LeaveBalances.SingleAsync()).UsedDays);Assert.Empty(await db.AttendanceOvertimeEntries.ToListAsync());
    }

    [Fact]
    public async Task T12_Offboard_BlocksBothModules_AndPreservesLedger()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day.AddDays(1));
        using var worker=await BearerClientAsync(s.Harness,s.Worker);
        using var hr=await BearerClientAsync(s.Harness,s.Hr);var profile=await hr.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{s.Profile.Id}");
        using(var off=await hr.PostAsJsonAsync($"/api/v1/employees/{s.Profile.Id}/offboard",new OffboardEmployeeRequest(T12Day,"T12 offboarding both modules",profile!.RowVersion!)))Assert.Equal(HttpStatusCode.NoContent,off.StatusCode);
        using(var msg=new HttpRequestMessage(HttpMethod.Post,"/api/v1/attendance/me/check-in"){Content=JsonContent.Create(new{})})
        {msg.Headers.Add("Idempotency-Key",T12V4("off-punch").ToString());using var response=await worker.SendAsync(msg);await ProblemAsync(response,HttpStatusCode.Unauthorized);}
        using(var r=await worker.PostAsJsonAsync("/api/v1/leave-requests",new{startDate=T12Day,endDate=T12Day,reason="T12 offboard leave"}))await ProblemAsync(r,HttpStatusCode.Unauthorized);
        using(var r=await T12ApproveAsync(s,leave))await ProblemAsync(r,HttpStatusCode.Conflict);
        await using var db=sql.CreateContext();Assert.Empty(await db.AttendanceEvents.ToListAsync());Assert.Equal(0m,(await db.LeaveBalances.SingleAsync()).UsedDays);Assert.Single(await db.LeaveLedgerEntries.ToListAsync());
    }

    [Fact]
    public async Task T12_AuditFailure_MustRollbackAlreadyFlushedLeaveMutation()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day);
        await using(var injected=new AuthHarness(sql.ConnectionString,configure:services=>services.AddScoped<IAuditRepository,T12AuditFault>()))
        {using var r=await T12ApproveAsync(s,leave,h:injected);await ProblemAsync(r,HttpStatusCode.InternalServerError);}
        await using var db=sql.CreateContext();Assert.Equal("Pending",(await db.LeaveRecords.SingleAsync()).Status);Assert.Equal(0m,(await db.LeaveBalances.SingleAsync()).UsedDays);Assert.Empty(await db.LeaveLedgerEntries.Where(e=>e.Action=="Debit").ToListAsync());Assert.Empty(await db.LeaveIdempotencyRecords.Where(e=>e.Operation=="leave.approve").ToListAsync());
    }

    [Fact]
    public async Task T12_Idor_CrossModuleLinks_DoNotExposeOtherEmployees()
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);var leave=await T12LeaveAsync(s,T12Day.AddDays(1));await AttInAsync(s);
        var other=await CreateUserWithRolesAsync(s.Harness,"t12-idor",HrmRoles.Employee);await CreateEmployeeProfileAsync(s.Harness,other,"Other worker","t12-idor-profile");
        using var c=await BearerClientAsync(s.Harness,other);
        foreach(var route in new[]{$"/api/v1/leave-requests/{leave.Id}",$"/api/v1/leave-balances?employeeId={s.Profile.Id}",$"/api/v1/leave-balances/ledger?employeeId={s.Profile.Id}",AttDay(s.Profile.Id)})
        {using var response=await c.GetAsync(route);await ProblemAsync(response,HttpStatusCode.Forbidden);Assert.DoesNotContain("T12 seeded leave request",await response.Content.ReadAsStringAsync());}
    }

    [Theory]
    [InlineData("attendance")]
    [InlineData("debit")]
    [InlineData("balance")]
    public async Task T12_PhysicalConstraints_NegativeControlDetectsRemoval(string kind)
    {
        await using var s=await StartAttendanceAsync();await T12GrantAsync(s);
        var leave=await T12LeaveAsync(s,T12Day.AddDays(1));using(var a=await T12ApproveAsync(s,leave))Assert.Equal(HttpStatusCode.NoContent,a.StatusCode);await AttInAsync(s);
        await using var db=sql.CreateContext();await using var tx=await db.Database.BeginTransactionAsync();
        var table=kind=="attendance"?"attendance.AttendanceSummaries":kind=="debit"?"personnel.LeaveLedgerEntries":"personnel.LeaveBalances";
        string mutation,statement;
        if(kind=="attendance")
        {mutation="DROP INDEX IX_AttendanceSummaries_EmployeeId_BusinessDate ON attendance.AttendanceSummaries";statement="INSERT attendance.AttendanceSummaries (Id,EmployeeId,BusinessDate,CheckInUtc,CalendarVersionId,CalendarDefinitionJson,EligibleMinutes,LateMinutes,EarlyLeaveMinutes,Phase,UpdatedAtUtc) SELECT NEWID(),EmployeeId,BusinessDate,CheckInUtc,CalendarVersionId,CalendarDefinitionJson,EligibleMinutes,LateMinutes,EarlyLeaveMinutes,Phase,UpdatedAtUtc FROM attendance.AttendanceSummaries";}
        else if(kind=="debit")
        {mutation="DROP INDEX IX_LeaveLedgerEntries_LeaveRequestId_Action ON personnel.LeaveLedgerEntries";statement="INSERT personnel.LeaveLedgerEntries (Id,EmployeeId,LeaveRequestId,Year,Action,Days,BalanceBefore,BalanceAfter,Reason,ActorId,CreatedAtUtc) SELECT NEWID(),EmployeeId,LeaveRequestId,Year,Action,Days,BalanceBefore,BalanceAfter,Reason,ActorId,CreatedAtUtc FROM personnel.LeaveLedgerEntries WHERE Action=N'Debit'";}
        else
        {mutation="ALTER TABLE personnel.LeaveBalances DROP CONSTRAINT CK_LeaveBalances_NoOverdraft";statement="UPDATE personnel.LeaveBalances SET UsedDays=AllocatedDays+1";}
        // First prove the invariant with the actual schema, then demonstrate that
        // removing its physical defense permits invalid SQL. Always roll back DDL/data.
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.Database.ExecuteSqlRawAsync(statement));
        await db.Database.ExecuteSqlRawAsync(mutation);Assert.Equal(1,await db.Database.ExecuteSqlRawAsync(statement));
        await tx.RollbackAsync();
        await using var verify=sql.CreateContext();Assert.Single(await verify.AttendanceSummaries.ToListAsync());Assert.Single(await verify.LeaveLedgerEntries.Where(x=>x.Action=="Debit").ToListAsync());Assert.True((await verify.LeaveBalances.SingleAsync()).RemainingDays>=0m);
    }
}
