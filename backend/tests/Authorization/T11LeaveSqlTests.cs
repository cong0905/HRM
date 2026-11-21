using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Next.Api.Serialization;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Application.Payroll;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Leave;
using HRM.Next.Domain.Payroll;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed record LeaveScenario(
    SqlDatabaseFixture Sql,
    AuthHarness Harness,
    HrmUser Worker,
    HrmUser Hr,
    HrmUser OtherHr,
    HrmUser Admin,
    EmployeeProfile Profile);

public sealed record LeaveFlushObservation(int Balances, int Ledgers, string LeaveStatus);
public sealed class LeaveFlushProbe
{
    public List<LeaveFlushObservation> Observations { get; } = [];
}

public sealed class LeaveAfterFlushAuditRepository(HrmDbContext db, LeaveFlushProbe probe) : IAuditRepository
{
    public async Task RecordAsync(AuditEvent entry, CancellationToken token)
    {
        if (entry.Action != "leaves.approved")
        {
            db.AuditEvents.Add(entry);
            await db.SaveChangesAsync(token);
            return;
        }

        db.AuditEvents.Add(entry);
        await db.SaveChangesAsync(token);

        var leave = await db.LeaveRecords.SingleOrDefaultAsync(l => l.Id == Guid.Parse(entry.EntityId), token);
        var balances = await db.LeaveBalances.CountAsync(token);
        var ledgers = await db.LeaveLedgerEntries.CountAsync(token);

        probe.Observations.Add(new LeaveFlushObservation(balances, ledgers, leave?.Status ?? "None"));

        throw new InvalidOperationException("Synthetic audit failure AFTER SQL mutation flush.");
    }

    public Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters query, CancellationToken token) =>
        Task.FromResult(new PagedResult<AuditEventResponse>([], query.Page, query.PageSize, 0));
}

public sealed partial class FoundationSqlTests
{
    private static readonly JsonSerializerOptions LeaveJsonOptions = CreateLeaveJsonOptions();
    private static JsonSerializerOptions CreateLeaveJsonOptions()
    {
        var opts = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        ContractJson.Configure(opts);
        return opts;
    }

    private static readonly WorkCalendarDefinition DemoLeaveCalendar = new(
        FixtureId: "FIXTURE-CAL-2026-LEAVE",
        TimeZone: "Asia/Ho_Chi_Minh",
        ShiftStart: new TimeOnly(8, 30),
        ShiftEnd: new TimeOnly(17, 30),
        Breaks: [new WorkBreak(new TimeOnly(12, 0), new TimeOnly(13, 0))],
        WorkingDays: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
        Holidays: [new DateOnly(2026, 4, 26), new DateOnly(2026, 4, 27), new DateOnly(2026, 4, 30)]
    );

    private async Task<LeaveScenario> StartLeaveScenarioAsync(decimal initialQuota = 12m)
    {
        await sql.ResetAsync();
        var h = new AuthHarness(sql.ConnectionString);
        h.Clock.Advance(DateTimeOffset.Parse("2026-04-01T08:30:00Z") - h.Clock.GetUtcNow());

        var worker = await CreateUserWithRolesAsync(h, "leave-worker", HrmRoles.Employee);
        var hr = await CreateUserWithRolesAsync(h, "leave-hr", HrmRoles.Hr);
        var otherHr = await CreateUserWithRolesAsync(h, "leave-other-hr", HrmRoles.Hr);
        var admin = await CreateUserWithRolesAsync(h, "leave-admin", HrmRoles.SystemAdmin);

        var profile = await CreateEmployeeProfileAsync(h, worker, "Leave Worker", "038099888111");
        await AssignHrScopeAsync(h, hr.Id, profile.Id);

        await using (var scope = h.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var tracked = await db.EmployeeProfiles.AsTracking().SingleAsync(e => e.Id == profile.Id);
            tracked.HireDate = new DateOnly(2026, 1, 1);
            await db.SaveChangesAsync();
        }

        // Setup calendar covering 2026
        using (var hrClient = await BearerClientAsync(h, hr))
        {
            using var calRes = await hrClient.PostAsJsonAsync("/api/v1/work-calendars",
                new CreateCalendarVersionRequest(new DateOnly(2026, 1, 1), DemoLeaveCalendar, "Leave test calendar"));
            Assert.Equal(HttpStatusCode.Created, calRes.StatusCode);

            // Grant initial leave balance
            if (initialQuota > 0m)
            {
                using var grantRes = await hrClient.PostAsJsonAsync("/api/v1/leave-balances/grant",
                    new GrantLeaveBalanceRequest(profile.Id, 2026, initialQuota, "Initial leave grant"), LeaveJsonOptions);
                Assert.Equal(HttpStatusCode.Created, grantRes.StatusCode);
            }
        }

        return new LeaveScenario(sql, h, worker, hr, otherHr, admin, profile);
    }

    [Fact]
    public async Task Leave_WorkingDates_ExcludeWeekendAndHoliday_FromQuotaDeduction()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        // 2026-04-24 is Friday (working), 04-25 is Sat (off), 04-26 is Sun (holiday/off), 04-27 is Mon (holiday in fixture!)
        // So 2026-04-24 to 2026-04-27 has exactly 1 working day!
        var createReq = new CreateLeaveRequest(new DateOnly(2026, 4, 24), new DateOnly(2026, 4, 27), "Spanning holiday weekend");
        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);
        Assert.Equal(1.0m, leave.WorkingDays);

        // HR approves
        using var approveRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Approved holiday span"));
        Assert.Equal(HttpStatusCode.NoContent, approveRes.StatusCode);

        // Verify balance in database: exactly 1.0 day deducted (12 - 1 = 11)
        await using var db = sql.CreateContext();
        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
        Assert.Equal(1.0m, balance.UsedDays);
        Assert.Equal(11.0m, balance.RemainingDays);

        var ledger = await db.LeaveLedgerEntries.SingleAsync(l => l.LeaveRequestId == leave.Id && l.Action == "Debit");
        Assert.Equal(1.0m, ledger.Days);
        Assert.Equal(11.0m, ledger.BalanceAfter);
    }

    [Fact]
    public async Task Leave_CrossYearAndNoWorkingDays_RejectedWithClearErrors()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        // 1. Cross-year
        var crossYear = new CreateLeaveRequest(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 2), "Cross year vacation");
        using (var res = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", crossYear))
        {
            var problem = await ProblemAsync(res, HttpStatusCode.UnprocessableEntity);
            Assert.Equal("leave.cross_year_unsupported", problem.GetProperty("errorCode").GetString());
        }

        // 2. Weekend only (Saturday 2026-06-06 to Sunday 2026-06-07)
        var weekendOnly = new CreateLeaveRequest(new DateOnly(2026, 6, 6), new DateOnly(2026, 6, 7), "Weekend trip");
        using (var res = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", weekendOnly))
        {
            var problem = await ProblemAsync(res, HttpStatusCode.UnprocessableEntity);
            Assert.Equal("leave.no_working_days", problem.GetProperty("errorCode").GetString());
        }

        // 3. Inverted dates
        var inverted = new CreateLeaveRequest(new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 1), "Inverted");
        using (var res = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", inverted))
        {
            var problem = await ProblemAsync(res, HttpStatusCode.UnprocessableEntity);
            Assert.Equal("leave.invalid_date_range", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_Overlap_PendingOrApproved_RejectsWithConflict()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        // Submit Request A (June 1 to June 5) -> Pending
        var reqA = new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5), "Request A");
        using var resA = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", reqA);
        Assert.Equal(HttpStatusCode.Created, resA.StatusCode);
        var leaveA = await resA.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leaveA);

        // Submit overlapping Request B (June 4 to June 8) -> Rejected with 409 Conflict
        var reqB = new CreateLeaveRequest(new DateOnly(2026, 6, 4), new DateOnly(2026, 6, 8), "Request B overlapping");
        using (var resB = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", reqB))
        {
            var problem = await ProblemAsync(resB, HttpStatusCode.Conflict);
            Assert.Equal("leave.date_overlap", problem.GetProperty("errorCode").GetString());
        }

        // HR approves Request A
        using var approveA = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leaveA.Id}/approve",
            new ApproveLeaveRequest("Approved A"));
        Assert.Equal(HttpStatusCode.NoContent, approveA.StatusCode);

        // Submit overlapping Request C against Approved A (June 2 to June 3) -> Rejected with 409 Conflict
        var reqC = new CreateLeaveRequest(new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 3), "Request C overlapping approved");
        using (var resC = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", reqC))
        {
            var problem = await ProblemAsync(resC, HttpStatusCode.Conflict);
            Assert.Equal("leave.date_overlap", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_Create_ChecksBalanceAtSubmission_PendingDoesNotDeductQuota()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 2m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        // Worker attempts to submit 3 working days (June 1 to June 3) when balance is only 2 -> 422
        var overdrawReq = new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3), "Overdrawing 3 days");
        using (var resOver = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", overdrawReq))
        {
            var problem = await ProblemAsync(resOver, HttpStatusCode.UnprocessableEntity);
            Assert.Equal("leave.insufficient_balance", problem.GetProperty("errorCode").GetString());
        }

        // Worker submits valid 2-day request (June 1 to June 2) -> 201 Created
        var validReq = new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "Valid 2 days");
        using (var resValid = await workerClient.PostAsJsonAsync("/api/v1/leave-requests", validReq))
        {
            Assert.Equal(HttpStatusCode.Created, resValid.StatusCode);
        }

        // Verify balance in database: Used remains 0, Remaining remains 2!
        await using var db = sql.CreateContext();
        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
        Assert.Equal(0.0m, balance.UsedDays);
        Assert.Equal(2.0m, balance.RemainingDays);
        Assert.Empty(await db.LeaveLedgerEntries.Where(l => l.Action == "Debit").ToListAsync());
    }

    [Fact]
    public async Task Leave_GrantQuota_CreatesBalanceAndLedger_DuplicateGrantFails()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 0m);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        // HR grants 12 days for Year 2026
        var grantReq = new GrantLeaveBalanceRequest(s.Profile.Id, 2026, 12m, "Annual grant 2026");
        using (var grantRes = await hrClient.PostAsJsonAsync("/api/v1/leave-balances/grant", grantReq, LeaveJsonOptions))
        {
            Assert.Equal(HttpStatusCode.Created, grantRes.StatusCode);
            var body = await grantRes.Content.ReadFromJsonAsync<LeaveBalanceResponse>(LeaveJsonOptions);
            Assert.NotNull(body);
            Assert.Equal(12m, body.AllocatedDays);
            Assert.Equal(0m, body.UsedDays);
            Assert.Equal(12m, body.RemainingDays);
        }

        // Verify ledger entry
        await using (var db = sql.CreateContext())
        {
            var ledger = await db.LeaveLedgerEntries.SingleAsync(l => l.EmployeeId == s.Profile.Id && l.Action == "Grant");
            Assert.Equal(12m, ledger.Days);
            Assert.Equal(0m, ledger.BalanceBefore);
            Assert.Equal(12m, ledger.BalanceAfter);
        }

        // Duplicate grant for same employee and year must fail with 409 Conflict
        using (var dupRes = await hrClient.PostAsJsonAsync("/api/v1/leave-balances/grant", grantReq, LeaveJsonOptions))
        {
            var problem = await ProblemAsync(dupRes, HttpStatusCode.Conflict);
            Assert.Equal("leave.already_granted", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_Approval_RecheckBalance_FailsWhenInsufficient_RollsBackAll()
    {
        // Setup: Balance is only 1 day remaining (Allocated = 12, Used = 11)
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        await using (var dbInit = sql.CreateContext())
        {
            var bal = await dbInit.LeaveBalances.AsTracking().SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
            bal.UsedDays = 11m;
            await dbInit.SaveChangesAsync();
        }

        // Insert a 3-day Pending leave directly (as if submitted when quota was available)
        Guid leaveId;
        await using (var dbInit = sql.CreateContext())
        {
            var leave = new LeaveRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = s.Profile.Id,
                StartDate = new DateOnly(2026, 6, 1),
                EndDate = new DateOnly(2026, 6, 3),
                WorkingDays = 3m,
                Reason = "Vacation requiring 3 days",
                Status = "Pending",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            dbInit.LeaveRecords.Add(leave);
            await dbInit.SaveChangesAsync();
            leaveId = leave.Id;
        }

        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        // HR attempts to approve 3-day leave when only 1 day is left
        using (var res = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leaveId}/approve",
            new ApproveLeaveRequest("Try approve with insufficient quota")))
        {
            var problem = await ProblemAsync(res, HttpStatusCode.Conflict);
            Assert.Equal("leave.insufficient_balance", problem.GetProperty("errorCode").GetString());
        }

        // Verify that database was completely rolled back and remains untouched
        await using (var db = sql.CreateContext())
        {
            var leave = await db.LeaveRecords.SingleAsync(l => l.Id == leaveId);
            Assert.Equal("Pending", leave.Status);

            var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
            Assert.Equal(11m, balance.UsedDays);
            Assert.Equal(1m, balance.RemainingDays);

            Assert.Empty(await db.LeaveLedgerEntries.Where(l => l.LeaveRequestId == leaveId).ToListAsync());
            Assert.Empty(await db.AuditEvents.Where(a => a.EntityId == leaveId.ToString() && a.Action == "leaves.approved").ToListAsync());
        }
    }

    [Fact]
    public async Task Leave_ConcurrentApprovals_TotalExceedsQuota_15Races_NeverOverdrawsOrDoubleDebits()
    {
        // 15 repeated race iterations as required by specification
        for (int iteration = 0; iteration < 15; iteration++)
        {
            var s = await StartLeaveScenarioAsync(initialQuota: 2m);
            using var hr1 = await BearerClientAsync(s.Harness, s.Hr);

            // Create second HR assigned to the same employee
            var hr2User = await CreateUserWithRolesAsync(s.Harness, $"race-hr-{iteration}", HrmRoles.Hr);
            await AssignHrScopeAsync(s.Harness, hr2User.Id, s.Profile.Id);
            using var hr2 = await BearerClientAsync(s.Harness, hr2User);

            // Create two pending requests:
            // Request A: 2 days (June 1-2)
            // Request B: 1 day (June 15)
            // Total = 3 days > Quota of 2 days!
            Guid leaveIdA, leaveIdB;
            using (var workerClient = await BearerClientAsync(s.Harness, s.Worker))
            {
                using var resA = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
                    new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "Req A (2 days)"));
                Assert.Equal(HttpStatusCode.Created, resA.StatusCode);
                leaveIdA = (await resA.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!.Id;

                using var resB = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
                    new CreateLeaveRequest(new DateOnly(2026, 6, 15), new DateOnly(2026, 6, 15), "Req B (1 day)"));
                Assert.Equal(HttpStatusCode.Created, resB.StatusCode);
                leaveIdB = (await resB.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!.Id;
            }

            // Launch concurrent approvals at the same instant
            var taskA = hr1.PostAsJsonAsync($"/api/v1/leave-requests/{leaveIdA}/approve",
                new ApproveLeaveRequest("HR1 approving A"));
            var taskB = hr2.PostAsJsonAsync($"/api/v1/leave-requests/{leaveIdB}/approve",
                new ApproveLeaveRequest("HR2 approving B"));

            await Task.WhenAll(taskA, taskB);

            using var respA = await taskA;
            using var respB = await taskB;

            var statusCodes = new[] { (int)respA.StatusCode, (int)respB.StatusCode }.Order().ToArray();

            // Exactly one succeeds (204) and one fails (409 Conflict)
            Assert.Equal(new[] { 204, 409 }, statusCodes);

            // Verify database state: NEVER negative balance!
            await using var db = sql.CreateContext();
            var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);

            Assert.True(balance.RemainingDays >= 0m, $"Iteration {iteration}: Balance cannot be negative!");
            Assert.True(balance.UsedDays <= balance.AllocatedDays, $"Iteration {iteration}: Used days cannot exceed allocated days!");

            var debitEntries = await db.LeaveLedgerEntries.Where(l => l.Action == "Debit").ToListAsync();
            Assert.Single(debitEntries); // Exactly one debit entry exists!
        }
    }

    [Fact]
    public async Task Leave_ApproveRetry_SameIdempotencyKey_ReplaysWithoutSecondDeduction()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 5m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "2-day request"));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        var key = Guid.NewGuid();

        async Task<HttpResponseMessage> SendApproveAsync()
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/leave-requests/{leave.Id}/approve")
            {
                Content = JsonContent.Create(new ApproveLeaveRequest("Approved with idempotency"))
            };
            msg.Headers.Add("Idempotency-Key", key.ToString());
            return await hrClient.SendAsync(msg);
        }

        // First attempt
        using (var res1 = await SendApproveAsync())
        {
            Assert.Equal(HttpStatusCode.NoContent, res1.StatusCode);
        }

        // Replay attempt with same key and payload
        using (var res2 = await SendApproveAsync())
        {
            Assert.Equal(HttpStatusCode.NoContent, res2.StatusCode);
        }

        // Verify balance in database: used exactly 2 days (5 - 2 = 3), NOT 4 days!
        await using var db = sql.CreateContext();
        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
        Assert.Equal(2.0m, balance.UsedDays);
        Assert.Equal(3.0m, balance.RemainingDays);

        var debitEntries = await db.LeaveLedgerEntries.Where(l => l.LeaveRequestId == leave.Id && l.Action == "Debit").ToListAsync();
        Assert.Single(debitEntries);
    }

    [Fact]
    public async Task Leave_ApproveAlreadyApproved_DifferentKey_Returns409Conflict_WithoutSecondDeduction()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 5m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "2-day request"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        // First approve
        using var firstApprove = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("First approve"));
        Assert.Equal(HttpStatusCode.NoContent, firstApprove.StatusCode);

        // Second approve with a different key (or no key)
        using var secondApprove = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Second approve attempt"));
        var problem = await ProblemAsync(secondApprove, HttpStatusCode.Conflict);
        Assert.Equal("leave.already_processed", problem.GetProperty("errorCode").GetString());

        // Balance remains deducted only once (2 days)
        await using var db = sql.CreateContext();
        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
        Assert.Equal(2.0m, balance.UsedDays);
        Assert.Equal(3.0m, balance.RemainingDays);
        Assert.Single(await db.LeaveLedgerEntries.Where(l => l.Action == "Debit").ToListAsync());
    }

    [Fact]
    public async Task Leave_AuditFailureAfterSqlFlush_RollsBackStatusBalanceLedgerAndAudit()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "2-day request"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        // Inject audit repository failure after SQL flush
        var probe = new LeaveFlushProbe();
        await using var faultyHarness = new AuthHarness(sql.ConnectionString,
            configure: services => services.AddScoped<IAuditRepository>(sp =>
                new LeaveAfterFlushAuditRepository(sp.GetRequiredService<HrmDbContext>(), probe)));
        faultyHarness.Clock.Advance(s.Harness.Clock.GetUtcNow() - faultyHarness.Clock.GetUtcNow());

        using var faultyHrClient = await BearerClientAsync(faultyHarness, s.Hr);

        // Call approve on faulty harness -> returns 500 InternalServerError
        using (var res = await faultyHrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Approval doomed to fail at audit")))
        {
            await ProblemAsync(res, HttpStatusCode.InternalServerError);
        }

        // Verify probe observed that SQL mutation HAD occurred before the injected exception
        Assert.Single(probe.Observations);
        Assert.Equal("Approved", probe.Observations[0].LeaveStatus);
        Assert.Equal(2, probe.Observations[0].Ledgers); // 1 initial Grant ledger + 1 flushed Debit ledger

        // Verify database after transaction rollback: completely intact!
        await using (var db = sql.CreateContext())
        {
            var rolledBackLeave = await db.LeaveRecords.SingleAsync(l => l.Id == leave.Id);
            Assert.Equal("Pending", rolledBackLeave.Status);

            var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
            Assert.Equal(0.0m, balance.UsedDays);
            Assert.Equal(12.0m, balance.RemainingDays);

            Assert.Empty(await db.LeaveLedgerEntries.Where(l => l.Action == "Debit").ToListAsync());
            Assert.Empty(await db.AuditEvents.Where(a => a.Action == "leaves.approved").ToListAsync());
        }
    }

    [Fact]
    public async Task Leave_CancelPending_ByOwner_DoesNotChangeQuota()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 10m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "To be cancelled"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        // Worker cancels own pending request
        using var cancelRes = await workerClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/cancel",
            new CancelLeaveRequest("Changed vacation plans"));
        Assert.Equal(HttpStatusCode.NoContent, cancelRes.StatusCode);

        // In DB: Status is Cancelled, Quota is untouched (Used = 0, Remaining = 10)
        await using var db = sql.CreateContext();
        var record = await db.LeaveRecords.SingleAsync(l => l.Id == leave.Id);
        Assert.Equal("Cancelled", record.Status);
        Assert.NotNull(record.CancelledAtUtc);
        Assert.Equal("Changed vacation plans", record.CancellationReason);

        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
        Assert.Equal(0.0m, balance.UsedDays);
        Assert.Equal(10.0m, balance.RemainingDays);
        Assert.Empty(await db.LeaveLedgerEntries.Where(l => l.Action == "Debit").ToListAsync());
    }

    [Fact]
    public async Task Leave_CancelApproved_RejectedAsUnsupportedInMvp()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 10m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "To be approved then cancel attempted"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        using var approveRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Approved"));
        Assert.Equal(HttpStatusCode.NoContent, approveRes.StatusCode);

        // Worker attempts to cancel already approved leave -> 409 Conflict
        using var cancelRes = await workerClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/cancel",
            new CancelLeaveRequest("Attempt cancel after approval"));
        var problem = await ProblemAsync(cancelRes, HttpStatusCode.Conflict);
        Assert.Equal("leave.cancel_approved_unsupported", problem.GetProperty("errorCode").GetString());

        // Status remains Approved
        await using var db = sql.CreateContext();
        var record = await db.LeaveRecords.SingleAsync(l => l.Id == leave.Id);
        Assert.Equal("Approved", record.Status);
    }

    [Fact]
    public async Task Leave_RejectPending_ByHr_SetsRejectedWithoutQuotaDeduction()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 10m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "Request to reject"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        using var rejectRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/reject",
            new RejectLeaveRequest("Critical project milestone during this period"));
        Assert.Equal(HttpStatusCode.NoContent, rejectRes.StatusCode);

        await using var db = sql.CreateContext();
        var record = await db.LeaveRecords.SingleAsync(l => l.Id == leave.Id);
        Assert.Equal("Rejected", record.Status);
        Assert.Equal("Critical project milestone during this period", record.DecisionReason);

        var balance = await db.LeaveBalances.SingleAsync(b => b.EmployeeId == s.Profile.Id && b.Year == 2026);
        Assert.Equal(0.0m, balance.UsedDays);
        Assert.Equal(10.0m, balance.RemainingDays);
        Assert.Empty(await db.LeaveLedgerEntries.Where(l => l.Action == "Debit").ToListAsync());
    }

    [Fact]
    public async Task Leave_AntiSelfApproval_HrCannotApproveOwnLeave()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);

        // Link HR user to an employee profile so they can submit a leave request
        var hrProfile = await CreateEmployeeProfileAsync(s.Harness, s.Hr, "HR With Profile", "038099888222");
        await AssignHrScopeAsync(s.Harness, s.Hr.Id, hrProfile.Id);

        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        // Grant balance to HR
        using var grantRes = await hrClient.PostAsJsonAsync("/api/v1/leave-balances/grant",
            new GrantLeaveBalanceRequest(hrProfile.Id, 2026, 12m, "HR annual grant"), LeaveJsonOptions);
        Assert.Equal(HttpStatusCode.Created, grantRes.StatusCode);

        // HR submits leave request for themselves
        using var createRes = await hrClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "HR self vacation"));
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        // HR tries to approve their own leave request -> 403 Forbidden!
        using var approveRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Self approval"));
        var problem = await ProblemAsync(approveRes, HttpStatusCode.Forbidden);
        Assert.Equal("leave.self_approval_forbidden", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Leave_ScopeViolation_UnassignedHrCannotApproveOrAccess()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var otherHrClient = await BearerClientAsync(s.Harness, s.OtherHr);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "Worker request"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        // Other HR (unassigned) tries to GET leave -> 403 Forbidden
        using (var getRes = await otherHrClient.GetAsync($"/api/v1/leave-requests/{leave.Id}"))
        {
            var problem = await ProblemAsync(getRes, HttpStatusCode.Forbidden);
            Assert.Equal("authorization.scope_forbidden", problem.GetProperty("errorCode").GetString());
        }

        // Other HR (unassigned) tries to approve -> 403 Forbidden
        using (var appRes = await otherHrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Unauthorized approval attempt")))
        {
            var problem = await ProblemAsync(appRes, HttpStatusCode.Forbidden);
            Assert.Equal("authorization.scope_forbidden", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_OffboardedEmployee_CannotSubmitOrApprovePastTermination()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        // Submit pending leave for December 2026
        Guid leaveId;
        using (var workerClient = await BearerClientAsync(s.Harness, s.Worker))
        {
            using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
                new CreateLeaveRequest(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2), "December holiday"));
            Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
            leaveId = (await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions))!.Id;
        }

        // Offboard employee effective November 11, 2026
        var getProfile = await hrClient.GetAsync($"/api/v1/employees/{s.Profile.Id}");
        var currentProfile = await getProfile.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
        Assert.NotNull(currentProfile);

        using var offRes = await hrClient.PostAsJsonAsync($"/api/v1/employees/{s.Profile.Id}/offboard",
            new OffboardEmployeeRequest(new DateOnly(2026, 4, 1), "Termination test", currentProfile.RowVersion!));
        Assert.Equal(HttpStatusCode.NoContent, offRes.StatusCode);

        // HR attempts to approve leave that occurs in December (AFTER termination date Nov 11) -> 409 Conflict!
        using (var appRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leaveId}/approve",
            new ApproveLeaveRequest("Post-termination approval attempt")))
        {
            var problem = await ProblemAsync(appRes, HttpStatusCode.Conflict);
            Assert.Equal("leave.approval_after_termination_forbidden", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_ClosedPayrollPeriod_BlocksSubmissionAndApproval()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);

        // Publish payroll period 2026-06
        await using (var db = sql.CreateContext())
        {
            db.PayrollPeriods.Add(new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-06",
                Status = "Published",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                PublishedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        // Worker attempts to submit leave in June 2026 -> 409 Conflict
        using (var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "In closed period")))
        {
            var problem = await ProblemAsync(createRes, HttpStatusCode.Conflict);
            Assert.Equal("leave.period_closed", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_StaleRowVersion_Returns409Conflict()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        using var hrClient = await BearerClientAsync(s.Harness, s.Hr);

        using var createRes = await workerClient.PostAsJsonAsync("/api/v1/leave-requests",
            new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "Stale test"));
        var leave = await createRes.Content.ReadFromJsonAsync<LeaveRecordResponse>(LeaveJsonOptions);
        Assert.NotNull(leave);

        var fakeStaleVersion = Convert.ToBase64String(new byte[] { 0, 0, 0, 0, 0, 0, 9, 9 });

        // HR attempts to approve with a stale row version
        using var approveRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leave.Id}/approve",
            new ApproveLeaveRequest("Approve with stale version", fakeStaleVersion));
        var problem = await ProblemAsync(approveRes, HttpStatusCode.Conflict);
        Assert.Equal("leave.stale_version", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task Leave_IdempotencyKeyMismatch_Returns409Conflict()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        var key = Guid.NewGuid();

        async Task<HttpResponseMessage> SendCreateAsync(string reason)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/leave-requests")
            {
                Content = JsonContent.Create(new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), reason))
            };
            msg.Headers.Add("Idempotency-Key", key.ToString());
            return await workerClient.SendAsync(msg);
        }

        // First attempt
        using (var res1 = await SendCreateAsync("Initial reason"))
        {
            Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        }

        // Second attempt with same key but different reason -> 409 Conflict
        using (var res2 = await SendCreateAsync("Different reason with same key"))
        {
            var problem = await ProblemAsync(res2, HttpStatusCode.Conflict);
            Assert.Equal("idempotency.payload_mismatch", problem.GetProperty("errorCode").GetString());
        }
    }

    [Fact]
    public async Task Leave_LedgerAndIdempotency_AreAppendOnly()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);

        await using (var db = sql.CreateContext())
        {
            var ledger = await db.LeaveLedgerEntries.AsTracking().FirstAsync();
            ledger.Reason = "Illegal ledger mutation";
            var ex = Assert.Throws<DomainRuleException>(() => db.SaveChanges());
            Assert.Equal("leave.immutable", ex.Code);
        }

        // Test mutating idempotency record
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);
        var key = Guid.NewGuid();
        using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/leave-requests")
        {
            Content = JsonContent.Create(new CreateLeaveRequest(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 2), "Idemp check"))
        };
        msg.Headers.Add("Idempotency-Key", key.ToString());
        using var res = await workerClient.SendAsync(msg);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        await using (var db = sql.CreateContext())
        {
            var idemp = await db.LeaveIdempotencyRecords.AsTracking().FirstAsync(r => r.Key == key);
            idemp.ResponseJson = "{}";
            var ex = await Assert.ThrowsAsync<DomainRuleException>(() => db.SaveChangesAsync());
            Assert.Equal("leave.immutable", ex.Code);
        }
    }

    [Fact]
    public async Task Leave_PersonalEndpoints_AllowEmployeeToQueryOwnBalancesAndRequests()
    {
        var s = await StartLeaveScenarioAsync(initialQuota: 12m);
        using var workerClient = await BearerClientAsync(s.Harness, s.Worker);

        // GET /api/v1/me/leave-balances
        using (var res = await workerClient.GetAsync("/api/v1/me/leave-balances?year=2026"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var balances = await res.Content.ReadFromJsonAsync<List<LeaveBalanceResponse>>(LeaveJsonOptions);
            Assert.NotNull(balances);
            Assert.Single(balances);
            Assert.Equal(12m, balances[0].RemainingDays);
        }

        // GET /api/v1/leave-balances?employeeId={otherId} -> 403 Forbidden
        var dummyId = Guid.NewGuid();
        using (var res = await workerClient.GetAsync($"/api/v1/leave-balances?employeeId={dummyId}"))
        {
            var problem = await ProblemAsync(res, HttpStatusCode.Forbidden);
            Assert.Equal("authorization.forbidden", problem.GetProperty("errorCode").GetString());
        }
    }
}
