using System.Net;
using System.Net.Http.Json;
using HRM.Next.Api.Controllers;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain.Calendar;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    [Fact]
    public async Task T17_Organization_RejectsEmployeeAndStaleWrites_AuditsCrud()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr = await CreateUserWithRolesAsync(harness, "t17-org-hr", HrmRoles.Hr);
        var employee = await CreateUserWithRolesAsync(harness, "t17-org-employee", HrmRoles.Employee);
        using var denied = await BearerClientAsync(harness, employee);
        using var client = await BearerClientAsync(harness, hr);
        foreach (var kind in new[] { "departments", "positions" })
        {
            var name = "T17-" + Guid.NewGuid().ToString("N");
            await ProblemAsync(await denied.PostAsJsonAsync($"/api/v1/{kind}", new { name, reason = "Denied" }), HttpStatusCode.Forbidden);
            using var created = await client.PostAsJsonAsync($"/api/v1/{kind}", new { name, reason = "Create fixture" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var item = (await created.Content.ReadFromJsonAsync<PositionResponse>())!;
            using var renamed = await client.PutAsJsonAsync($"/api/v1/{kind}/{item.Id}", new { name = name + " edited", expectedName = name, reason = "Rename fixture" });
            Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
            await ProblemAsync(await client.PutAsJsonAsync($"/api/v1/{kind}/{item.Id}", new { name = "Lost update", expectedName = name, reason = "Stale fixture" }), HttpStatusCode.Conflict);
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/{kind}/{item.Id}") { Content = JsonContent.Create(new { expectedName = name + " edited", reason = "Delete fixture" }) };
            using var deleted = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
            await using var db = sql.CreateContext();
            Assert.Equal(3, await db.AuditEvents.CountAsync(a => a.EntityId == item.Id.ToString()));
        }
    }

    [Fact]
    public async Task T17_PositionAssignedToEmployee_CannotBeDeleted_UnknownPositionRejected()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr = await CreateUserWithRolesAsync(harness, "t17-position-hr", HrmRoles.Hr);
        using var client = await BearerClientAsync(harness, hr);
        using var created = await client.PostAsJsonAsync("/api/v1/positions", new { name = "T17 position", reason = "Fixture" });
        var position = (await created.Content.ReadFromJsonAsync<PositionResponse>())!;
        var request = ReviewEmployeeRequest(Guid.NewGuid().ToString("N")[..8]) with { PositionId = position.Id };
        using var response = await client.PostAsJsonAsync("/api/v1/employees", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var employee = (await response.Content.ReadFromJsonAsync<CreateEmployeeResponse>())!.Profile;
        await AssignHrScopeAsync(harness, hr.Id, employee.Id);
        using var deletion = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/positions/{position.Id}") { Content = JsonContent.Create(new { expectedName = position.Name, reason = "Referenced fixture" }) };
        await ProblemAsync(await client.SendAsync(deletion), HttpStatusCode.Conflict);
        await ProblemAsync(await client.PatchAsJsonAsync($"/api/v1/employees/{employee.Id}", new { positionId = Guid.NewGuid(), reason = "Unknown fixture", rowVersion = employee.RowVersion }), HttpStatusCode.BadRequest);
        var after = await client.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{employee.Id}");
        Assert.Equal(position.Id, after!.PositionId);
    }

    [Fact]
    public async Task T17_AccountsList_IsAdminOnly_ContainsNoCredentials_AndStatusFilterIsScoped()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var admin = await CreateUserWithRolesAsync(harness, "t17-admin", HrmRoles.SystemAdmin);
        var hr = await CreateUserWithRolesAsync(harness, "t17-list-hr", HrmRoles.Hr);
        var employee = await CreateUserWithRolesAsync(harness, "t17-list-employee", HrmRoles.Employee);
        var profile = await CreateEmployeeProfileAsync(harness, employee, "T17 own employee", "t17-list-citizen");
        await AssignHrScopeAsync(harness, hr.Id, profile.Id);
        using var adminClient = await BearerClientAsync(harness, admin);
        using var hrClient = await BearerClientAsync(harness, hr);
        using var employeeClient = await BearerClientAsync(harness, employee);
        await ProblemAsync(await hrClient.GetAsync("/api/v1/accounts"), HttpStatusCode.Forbidden);
        await ProblemAsync(await employeeClient.GetAsync("/api/v1/accounts"), HttpStatusCode.Forbidden);
        using var response = await adminClient.GetAsync("/api/v1/accounts?pageSize=1&username=t17-list-employee");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", text, StringComparison.OrdinalIgnoreCase);
        var list = await response.Content.ReadFromJsonAsync<PagedResult<AccountResponse>>();
        Assert.Single(list!.Items);
        var active = await hrClient.GetFromJsonAsync<PagedResult<EmployeeProfileResponse>>("/api/v1/employees?status=DangLamViec&pageSize=1");
        Assert.Equal(profile.Id, Assert.Single(active!.Items).Id);
        var inactive = await hrClient.GetFromJsonAsync<PagedResult<EmployeeProfileResponse>>("/api/v1/employees?status=DaNghiViec");
        Assert.Empty(inactive!.Items);
        await ProblemAsync(await hrClient.GetAsync("/api/v1/employees?status=Invented"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T17_CalendarPreview_UsesServerMathWithoutPersisting_AndMatchesCreatedMonth()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr = await CreateUserWithRolesAsync(harness, "t17-calendar-hr", HrmRoles.Hr);
        var employee = await CreateUserWithRolesAsync(harness, "t17-calendar-employee", HrmRoles.Employee);
        using var client = await BearerClientAsync(harness, hr);
        using var denied = await BearerClientAsync(harness, employee);
        var definition = new WorkCalendarDefinition("T17", "Asia/Ho_Chi_Minh", new(8, 0), new(17, 0), [new(new(12, 0), new(13, 0))],
            [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday], [new(2026, 11, 20)]);
        var body = new PreviewCalendarRequest(2026, 11, definition);
        await ProblemAsync(await denied.PostAsJsonAsync("/api/v1/work-calendars/preview", body), HttpStatusCode.Forbidden);
        using var response = await client.PostAsJsonAsync("/api/v1/work-calendars/preview", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = (await response.Content.ReadFromJsonAsync<CalendarMonthResponse>())!;
        await using (var db = sql.CreateContext()) Assert.False(await db.WorkCalendarVersions.AnyAsync());
        using var created = await client.PostAsJsonAsync("/api/v1/work-calendars", new CreateCalendarVersionRequest(new(2026, 11, 1), definition, "Fixture calendar"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var actual = (await client.GetFromJsonAsync<CalendarMonthResponse>("/api/v1/work-calendars/months/2026/11"))!;
        Assert.Equal(20, preview.StandardDays);
        Assert.Equal(9600m, preview.StandardMinutes);
        Assert.Equal(preview.StandardMinutes, actual.StandardMinutes);
        Assert.Equal(preview.Days.Select(d => d.StandardMinutes), actual.Days.Select(d => d.StandardMinutes));
    }
}
