using System.Net;
using System.Net.Http.Json;
using HRM.Next.Api.Auth;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Personnel;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    private static CreateEmployeeRequest ReviewEmployeeRequest(string suffix) => new(
        "Review employee", "review-" + suffix, suffix + "@example.invalid", "0901234567", "Original address",
        new DateOnly(2000, 1, 2), "Nam", new DateOnly(2026, 1, 1), "DocThan", null, null, "T08 review", "review-" + suffix);

    [Fact]
    public async Task Employee_Create_InvalidIdentityUsername_RollsBackProfileAccountAndOutbox()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr = await CreateUserWithRolesAsync(harness, "identity-failure-hr", HrmRoles.Hr);
        using var client = await BearerClientAsync(harness, hr);
        var request = ReviewEmployeeRequest(Guid.NewGuid().ToString("N")[..8]) with { Username = "invalid username with spaces" };
        using var response = await client.PostAsJsonAsync("/api/v1/employees", request);
        await ProblemAsync(response, HttpStatusCode.BadRequest);
        await using var db = sql.CreateContext();
        Assert.False(await db.Users.AnyAsync(u => u.Email == request.Email));
        Assert.False(await db.EmployeeProfiles.AnyAsync(e => e.CitizenId == request.CitizenId));
        Assert.False(await db.ActivationOutboxMessages.AnyAsync(m => m.Email == request.Email));
    }

    [Fact]
    public async Task Employee_Create_SenderFailure_PreservesCommittedWorkflowAndReportsPendingRetry()
    {
        await using var harness = new AuthHarness(sql.ConnectionString, configure: services =>
            services.AddSingleton<IAccountMessageSender>(new T08FailingSender()));
        var hr = await CreateUserWithRolesAsync(harness, "sender-failure-hr", HrmRoles.Hr);
        using var client = await BearerClientAsync(harness, hr);
        var request = ReviewEmployeeRequest(Guid.NewGuid().ToString("N")[..8]);
        using var response = await client.PostAsJsonAsync("/api/v1/employees", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<CreateEmployeeResponse>();
        Assert.NotNull(result);
        Assert.Equal("DeliveryFailedPendingRetry", result.DeliveryStatus);
        await using var db = sql.CreateContext();
        var profile = await db.EmployeeProfiles.SingleAsync(e => e.Id == result.Profile.Id);
        Assert.False((await db.Users.SingleAsync(u => u.Id == profile.UserId)).IsActive);
        var message = await db.ActivationOutboxMessages.SingleAsync(m => m.UserId == profile.UserId);
        Assert.Equal("Pending", message.Status);
        Assert.Null(message.DispatchedAtUtc);
        Assert.True(await db.AuditEvents.AnyAsync(a => a.EntityId == profile.Id.ToString() && a.Action == "employees.created"));
    }

    [Fact]
    public async Task Employee_Patch_NullRequiredAndInvalidDates_DoNotMutateProfile()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr = await CreateUserWithRolesAsync(harness, "required-fields-hr", HrmRoles.Hr);
        var user = await CreateUserWithRolesAsync(harness, "required-fields-employee", HrmRoles.Employee);
        var employee = await CreateEmployeeProfileAsync(harness, user, "Preserved name", "review-required-001");
        await AssignHrScopeAsync(harness, hr.Id, employee.Id);
        using var client = await BearerClientAsync(harness, hr);
        var before = await client.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{employee.Id}");
        Assert.NotNull(before);
        foreach (object request in new object[] {
            new { phone = (string?)null, reason = "Cannot clear phone", rowVersion = before.RowVersion },
            new { address = (string?)null, reason = "Cannot clear address", rowVersion = before.RowVersion },
            new { dateOfBirth = "2099-01-01", reason = "Invalid birth", rowVersion = before.RowVersion },
            new { hireDate = (string?)null, reason = "Cannot clear hire date", rowVersion = before.RowVersion }
        })
        {
            using var response = await client.PatchAsJsonAsync($"/api/v1/employees/{employee.Id}", request);
            await ProblemAsync(response, HttpStatusCode.BadRequest);
        }
        var after = await client.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{employee.Id}");
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task Employee_Patch_TrueConcurrentRequests_OneSuccessOneConflict()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr1 = await CreateUserWithRolesAsync(harness, "concurrent-hr1", HrmRoles.Hr);
        var hr2 = await CreateUserWithRolesAsync(harness, "concurrent-hr2", HrmRoles.Hr);
        var user = await CreateUserWithRolesAsync(harness, "concurrent-target", HrmRoles.Employee);
        var employee = await CreateEmployeeProfileAsync(harness, user, "Concurrent subject", "review-race-001");
        await AssignHrScopeAsync(harness, hr1.Id, employee.Id);
        await AssignHrScopeAsync(harness, hr2.Id, employee.Id);
        using var first = await BearerClientAsync(harness, hr1);
        using var second = await BearerClientAsync(harness, hr2);
        var before = await first.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{employee.Id}");
        Assert.NotNull(before);
        var responses = await Task.WhenAll(
            first.PatchAsJsonAsync($"/api/v1/employees/{employee.Id}", new { phone = "0911111111", reason = "Concurrent first", rowVersion = before.RowVersion }),
            second.PatchAsJsonAsync($"/api/v1/employees/{employee.Id}", new { phone = "0922222222", reason = "Concurrent second", rowVersion = before.RowVersion }));
        try {
            Assert.Equal(new[] { 200, 409 }, responses.Select(r => (int)r.StatusCode).Order().ToArray());
            await using var db = sql.CreateContext();
            var after = await db.EmployeeProfiles.SingleAsync(e => e.Id == employee.Id);
            Assert.Contains(after.Phone, new[] { "0911111111", "0922222222" });
            Assert.Equal(before.Address, after.Address);
            Assert.Equal(1, await db.AuditEvents.CountAsync(a => a.EntityId == employee.Id.ToString() && a.Action == "employees.updated"));
        } finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task Employee_Offboarding_RejectsCookieBearerRefreshAndRetainsSessionAudit()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var hr = await CreateUserWithRolesAsync(harness, "revocation-hr", HrmRoles.Hr);
        var user = await CreateUserWithRolesAsync(harness, "revocation-target", HrmRoles.Employee);
        var employee = await CreateEmployeeProfileAsync(harness, user, "Revocation subject", "review-revoke-001");
        await AssignHrScopeAsync(harness, hr.Id, employee.Id);
        using var hrClient = await BearerClientAsync(harness, hr);
        using var mobile = harness.Client();
        var pair = await MobileAsync(mobile, user);
        UseBearer(mobile, pair.AccessToken);
        using var web = harness.Client();
        await WebAsync(web, user);
        var before = await hrClient.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{employee.Id}");
        Assert.NotNull(before?.RowVersion);
        using var response = await hrClient.PostAsJsonAsync($"/api/v1/employees/{employee.Id}/offboard",
            new OffboardEmployeeRequest(new DateOnly(2026, 11, 11), "Review revocation", before.RowVersion));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var bearerRejected = await mobile.GetAsync("/api/v1/me/profile");
        using var cookieRejected = await web.GetAsync("/api/v1/me/profile");
        using var refreshClient = harness.Client();
        using var refreshRejected = await refreshClient.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(pair.RefreshToken));
        await ProblemAsync(bearerRejected, HttpStatusCode.Unauthorized);
        await ProblemAsync(cookieRejected, HttpStatusCode.Unauthorized);
        await ProblemAsync(refreshRejected, HttpStatusCode.Unauthorized);
        await using var db = sql.CreateContext();
        Assert.All(await db.AccountSessions.Where(s => s.UserId == user.Id).ToListAsync(), s => Assert.True(s.Revoked));
        Assert.True(await db.AuditEvents.AnyAsync(a => a.EntityId == employee.Id.ToString() && a.Action == "employees.offboarded"));
    }

    [Fact]
    public async Task Employee_Migration_ExistingProfilesBackfilledAndYearCounterContinues()
    {
        await sql.ResetAsync();
        await using var db = sql.CreateContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261111134124_HrEmployeeAssignments");
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        try {
            // Seed only columns which existed before T08; include no new-model assumptions.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [AspNetUsers] ([Id],[IsActive],[Email],[UserName],[NormalizedUserName],[NormalizedEmail],[EmailConfirmed],[PhoneNumberConfirmed],[TwoFactorEnabled],[LockoutEnabled],[AccessFailedCount]) VALUES ({userId},{true},{"upgrade@example.invalid"},{"upgrade-user"},{"UPGRADE-USER"},{"UPGRADE@EXAMPLE.INVALID"},{false},{false},{false},{false},{0})");
            var created = new DateTimeOffset(2025, 12, 31, 18, 0, 0, TimeSpan.Zero); // 2026 in Vietnam.
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [personnel].[EmployeeProfiles] ([Id],[UserId],[FullName],[CitizenId],[Phone],[Address],[CreatedAtUtc]) VALUES ({employeeId},{userId},{"Existing employee"},{"upgrade-001"},{"0900000001"},{"Kept address"},{created})");
        } finally { await migrator.MigrateAsync(); }
        var profile = await db.EmployeeProfiles.SingleAsync(e => e.Id == employeeId);
        Assert.Equal("NV20260001", profile.EmployeeCode);
        Assert.Equal("upgrade@example.invalid", profile.Email);
        Assert.Equal("DangLamViec", profile.Status);
        Assert.Equal("Kept address", profile.Address);
        Assert.Equal(userId, profile.UserId);
        Assert.Equal(8, profile.RowVersion.Length);
        Assert.Equal("NV20260002", await db.GenerateEmployeeCodeAsync(2026, CancellationToken.None));
        Assert.Equal("NV20270001", await db.GenerateEmployeeCodeAsync(2027, CancellationToken.None));
        Assert.False(db.Database.HasPendingModelChanges());
    }
}

public sealed class T08FailingSender : IAccountMessageSender
{
    public string Mode => "test-only";
    public Task SendAsync(AccountMessage message, CancellationToken cancellationToken) => throw new InvalidOperationException("Synthetic sender unavailable");
}
