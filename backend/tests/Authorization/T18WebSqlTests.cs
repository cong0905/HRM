using System.Net;
using System.Net.Http.Json;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    [Fact]
    public async Task T18_AttendanceList_EnforcesScopePayrollPolicyAndServerPagination()
    {
        await using var s = await StartAttendanceAsync();
        var first = await AttInAsync(s);
        s.Harness.Clock.Advance(TimeSpan.FromDays(1));
        var other = await CreateUserWithRolesAsync(s.Harness, "t18-other-worker", HrmRoles.Employee);
        var otherProfile = await CreateEmployeeProfileAsync(s.Harness, other, "T18 Other worker", "t18-other-citizen");
        await AssignHrScopeAsync(s.Harness, s.OtherHr.Id, otherProfile.Id);
        using (var response = await AttPostAsync(s.Harness, other, "/api/v1/attendance/me/check-in", new { }, Guid.NewGuid())) await AttOkAsync(response);
        using var hr = await BearerClientAsync(s.Harness, s.Hr);
        using var payroll = await BearerClientAsync(s.Harness, s.Payroll);
        using var worker = await BearerClientAsync(s.Harness, s.Worker);
        using var admin = await BearerClientAsync(s.Harness, s.Admin);
        using var anonymous = s.Harness.Client();
        await ProblemAsync(await anonymous.GetAsync("/api/v1/attendance"), HttpStatusCode.Unauthorized);
        await ProblemAsync(await worker.GetAsync("/api/v1/attendance"), HttpStatusCode.Forbidden);
        await ProblemAsync(await admin.GetAsync("/api/v1/attendance"), HttpStatusCode.Forbidden);
        await ProblemAsync(await hr.GetAsync($"/api/v1/attendance?employeeId={otherProfile.Id}"), HttpStatusCode.Forbidden);
        var ownScope = (await hr.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance?status=MissingCheckout&pageSize=1"))!;
        Assert.Equal(first.Id, Assert.Single(ownScope.Items).Id);
        Assert.Equal("MissingCheckout", ownScope.Items[0].Status);
        Assert.Equal(1, ownScope.TotalCount);
        var page1 = (await payroll.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance?pageSize=1"))!;
        var page2 = (await payroll.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance?page=2&pageSize=1"))!;
        Assert.Equal(2, page1.TotalCount);
        Assert.NotEqual(Assert.Single(page1.Items).Id, Assert.Single(page2.Items).Id);
        var filtered = (await payroll.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance?from=2026-11-13&to=2026-11-13"))!;
        Assert.Equal(otherProfile.Id, Assert.Single(filtered.Items).EmployeeId);
        await ProblemAsync(await hr.GetAsync("/api/v1/attendance?from=2026-11-14&to=2026-11-12"), HttpStatusCode.BadRequest);
        await ProblemAsync(await hr.GetAsync("/api/v1/attendance?status=Invented"), HttpStatusCode.BadRequest);
        await ProblemAsync(await hr.GetAsync("/api/v1/attendance?pageSize=101"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T18_ListCorrectedAttendance_MatchesExistingDayAndOwnHistoryWithoutChangingMetrics()
    {
        await using var s = await StartAttendanceAsync();
        var original = await AttCompleteAsync(s);
        using var changed = await AttPostAsync(s.Harness, s.Hr, AttDay(s.Profile.Id, "/adjust"),
            new AdjustAttendanceRequest(original.CheckInUtc, original.CheckOutUtc, "T18 preserve exact instants", original.RowVersion), Guid.NewGuid());
        var corrected = await AttOkAsync(changed);
        using var hr = await BearerClientAsync(s.Harness, s.Hr);
        using var worker = await BearerClientAsync(s.Harness, s.Worker);
        var team = (await hr.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance?status=Corrected"))!;
        var own = (await worker.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance/me/history"))!;
        var teamRow = Assert.Single(team.Items);
        Assert.Equal(corrected, teamRow);
        Assert.Equal(teamRow, Assert.Single(own.Items));
        Assert.Equal(original.EligibleMinutes, teamRow.EligibleMinutes);
        Assert.Empty((await hr.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance?status=Complete"))!.Items);
    }

    [Fact]
    public async Task T18_PeriodEditability_MatchesExistingWriteGate_AndReadDoesNotExposePayroll()
    {
        await using var s = await StartAttendanceAsync();
        var original = await AttCompleteAsync(s);
        using var worker = await BearerClientAsync(s.Harness, s.Worker);
        using var anonymous = s.Harness.Client();
        await ProblemAsync(await anonymous.GetAsync("/api/v1/work-calendars/months/2026/11/editability"), HttpStatusCode.Unauthorized);
        Assert.False((await worker.GetFromJsonAsync<PeriodEditabilityResponse>("/api/v1/work-calendars/months/2026/11/editability"))!.IsClosed);
        await using (var db = sql.CreateContext())
        {
            db.PayrollPeriods.Add(new PayrollPeriodRecord { Id = Guid.NewGuid(), PeriodCode = "2026-11_T18", Status = "Locked", CreatedAtUtc = s.Harness.Clock.GetUtcNow() });
            await db.SaveChangesAsync();
        }
        using var state = await worker.GetAsync("/api/v1/work-calendars/months/2026/11/editability");
        var body = await state.Content.ReadAsStringAsync();
        Assert.True((await state.Content.ReadFromJsonAsync<PeriodEditabilityResponse>())!.IsClosed);
        Assert.DoesNotContain("periodCode", body);
        Assert.DoesNotContain("salary", body);
        Assert.False((await worker.GetFromJsonAsync<PeriodEditabilityResponse>("/api/v1/work-calendars/months/2026/12/editability"))!.IsClosed);
        await ProblemAsync(await worker.GetAsync("/api/v1/work-calendars/months/2026/13/editability"), HttpStatusCode.BadRequest);
        using var denied = await AttPostAsync(s.Harness, s.Hr, AttDay(s.Profile.Id, "/adjust"),
            new AdjustAttendanceRequest(original.CheckInUtc, original.CheckOutUtc, "T18 locked period must stay unchanged", original.RowVersion), Guid.NewGuid());
        await ProblemAsync(denied, HttpStatusCode.Conflict);
        using var hr = await BearerClientAsync(s.Harness, s.Hr);
        Assert.Equal(original, Assert.Single((await hr.GetFromJsonAsync<PagedResult<AttendanceDayResponse>>("/api/v1/attendance"))!.Items));
        await using var check = sql.CreateContext();
        Assert.False(await check.AuditEvents.AnyAsync(a => a.EntityId == original.Id.ToString() && a.Action == "attendance.adjusted"));
    }
}
