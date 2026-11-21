using HRM.Next.Api.Auth;
using HRM.Next.Api.Controllers;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Departments;
using HRM.Next.Domain.Leave;
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
    [Fact]
    public async Task Employee_Create_WithInactiveAccount_AndFixedEmployeeRole_Success()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-creator", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        Guid deptId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var dept = Department.Create("Engineering", clock.GetUtcNow());
            db.Departments.Add(dept);
            await db.SaveChangesAsync();
            deptId = dept.Id;
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"emp-{suffix}@example.invalid";
        var cccd = "038099" + suffix[..6];

        var createReq = new CreateEmployeeRequest(
            FullName: "Nguyen Van A",
            CitizenId: cccd,
            Email: email,
            Phone: "0901112233",
            Address: "123 Main St",
            DateOfBirth: new DateOnly(1995, 5, 20),
            Gender: "Nam",
            HireDate: new DateOnly(2026, 1, 15),
            MaritalStatus: "DocThan",
            DepartmentId: deptId,
            PositionId: null,
            Reason: "Onboarding new software engineer");

        using var response = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        // Token must NEVER be returned in response body
        Assert.DoesNotContain("token", responseBody, StringComparison.OrdinalIgnoreCase);

        var result = await response.Content.ReadFromJsonAsync<CreateEmployeeResponse>();
        Assert.NotNull(result);
        Assert.Equal("TestDelivered", result.DeliveryStatus);
        Assert.Equal("InactivePendingActivation", result.ProvisioningStatus);
        Assert.Equal("Nguyen Van A", result.Profile.FullName);
        Assert.StartsWith("NV2026", result.Profile.EmployeeCode);
        Assert.NotNull(result.Profile.RowVersion);

        // Verify AspNetUsers: account exists, IsActive is false
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();

            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.False(user.IsActive);

            // Fixed role Employee
            var roles = await userManager.GetRolesAsync(user);
            Assert.Single(roles);
            Assert.Equal(HrmRoles.Employee, roles[0]);

            // Activation outbox in DB
            var outbox = await db.ActivationOutboxMessages.FirstOrDefaultAsync(o => o.UserId == user.Id);
            Assert.NotNull(outbox);
            Assert.Equal("Dispatched", outbox.Status);
            Assert.Equal("activation", outbox.Purpose);
        }

        // Test sender received message with activation token
        Assert.NotEmpty(harness.Sender.Messages);
        var message = harness.Sender.Messages.FirstOrDefault(m => m.Email == email && m.Purpose == "activation");
        Assert.NotNull(message);
        Assert.False(string.IsNullOrWhiteSpace(message.Token));

        // Employee activates account using this token
        using var anonClient = harness.Client();
        var activateReq = new ResetRequest(message.UserId, message.Token, AuthPassword);

        using var actRes = await anonClient.PostAsJsonAsync("/api/v1/auth/activate", activateReq);
        Assert.Equal(HttpStatusCode.NoContent, actRes.StatusCode);

        // After activation, user IsActive is true and can log in
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.True(user.IsActive);
        }

        var loginRes = await MobileAsync(anonClient, (await harness.Factory.Services.CreateAsyncScope().ServiceProvider.GetRequiredService<UserManager<HrmUser>>().FindByEmailAsync(email))!);
        Assert.NotNull(loginRes);
        Assert.False(string.IsNullOrWhiteSpace(loginRes.AccessToken));
    }

    [Fact]
    public async Task Employee_Create_DuplicateCodeOrEmailOrUsername_FailsWithConflict()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-dup", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"dup-{suffix}@example.invalid";
        var cccd = "038099" + suffix[..6];

        var createReq = new CreateEmployeeRequest(
            FullName: "Duplicate Candidate",
            CitizenId: cccd,
            Email: email,
            Phone: "0900000001",
            Address: "Dup Address",
            DateOfBirth: new DateOnly(1990, 1, 1),
            Gender: "Nu",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DaKetHon",
            DepartmentId: null,
            PositionId: null,
            Reason: "Initial creation");

        using (var res1 = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq))
        {
            Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        }

        int userCountBefore;
        int profileCountBefore;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            userCountBefore = await db.Users.CountAsync();
            profileCountBefore = await db.EmployeeProfiles.CountAsync();
        }

        // 1. Attempting duplicate email -> 409 Conflict
        var dupEmailReq = createReq with { CitizenId = "038099" + Guid.NewGuid().ToString("N")[..6], Reason = "Duplicate email attempt" };
        using (var res2 = await hrClient.PostAsJsonAsync("/api/v1/employees", dupEmailReq))
        {
            await ProblemAsync(res2, HttpStatusCode.Conflict);
        }

        // 2. Attempting duplicate citizen id -> 409 Conflict
        var dupCccdReq = createReq with { Email = $"other-{suffix}@example.invalid", Reason = "Duplicate cccd attempt" };
        using (var res3 = await hrClient.PostAsJsonAsync("/api/v1/employees", dupCccdReq))
        {
            await ProblemAsync(res3, HttpStatusCode.Conflict);
        }

        // Verify rollback: no orphaned user or profile rows added
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            Assert.Equal(userCountBefore, await db.Users.CountAsync());
            Assert.Equal(profileCountBefore, await db.EmployeeProfiles.CountAsync());
        }
    }

    [Fact]
    public async Task Employee_Create_InvalidDates_Returns400()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-underage", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        // Birth after the hire date is invalid chronology. No unapproved hiring-age policy.
        var createReq = new CreateEmployeeRequest(
            FullName: "Young Worker",
            CitizenId: "038099888777",
            Email: $"young-{Guid.NewGuid():N}@example.invalid",
            Phone: "0900000002",
            Address: "Underage Address",
            DateOfBirth: new DateOnly(2027, 6, 1),
            Gender: "Nam",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DocThan",
            DepartmentId: null,
            PositionId: null,
            Reason: "Underage check");

        using var response = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq);
        await ProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Employee_Create_InvalidDepartment_Returns400()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-baddept", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var nonExistentDept = Guid.NewGuid();
        var createReq = new CreateEmployeeRequest(
            FullName: "Bad Dept Worker",
            CitizenId: "038099888666",
            Email: $"baddept-{Guid.NewGuid():N}@example.invalid",
            Phone: "0900000003",
            Address: "Bad Dept Address",
            DateOfBirth: new DateOnly(1992, 4, 1),
            Gender: "Nu",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DocThan",
            DepartmentId: nonExistentDept,
            PositionId: null,
            Reason: "Invalid dept check");

        using var response = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq);
        await ProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Employee_Create_AuditFailure_RollsBackAllChanges()
    {
        var failingAuditRepo = new FailingAuditRepository();
        await using var harness = new AuthHarness(sql.ConnectionString, configure: services =>
        {
            services.AddScoped<IAuditRepository>(_ => failingAuditRepo);
        });

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-auditfail", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var email = $"auditfail-{Guid.NewGuid():N}@example.invalid";
        var createReq = new CreateEmployeeRequest(
            FullName: "Audit Fail Worker",
            CitizenId: "038099888555",
            Email: email,
            Phone: "0900000004",
            Address: "Audit Fail Address",
            DateOfBirth: new DateOnly(1991, 1, 1),
            Gender: "Nam",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DocThan",
            DepartmentId: null,
            PositionId: null,
            Reason: "Audit fail test");

        // Armed audit failure triggers exception inside transaction
        failingAuditRepo.ShouldFail = true;

        using var response = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq);
        // Expect server error / rollback
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        // Verify transaction rolled back: user and employee profile do not exist
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.Null(user);
        var profile = await db.EmployeeProfiles.FirstOrDefaultAsync(p => p.CitizenId == "038099888555");
        Assert.Null(profile);
    }

    [Fact]
    public async Task Employee_Create_SenderNotConfigured_ReportsPendingNotSent()
    {
        // Test sender disabled -> uses UnconfiguredAccountMessageSender
        await using var harness = new AuthHarness(sql.ConnectionString, testSender: false);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-notconf", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var email = $"notconf-{Guid.NewGuid():N}@example.invalid";
        var createReq = new CreateEmployeeRequest(
            FullName: "Not Configured Worker",
            CitizenId: "038099888444",
            Email: email,
            Phone: "0900000005",
            Address: "Not Config Address",
            DateOfBirth: new DateOnly(1993, 3, 3),
            Gender: "Nu",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DocThan",
            DepartmentId: null,
            PositionId: null,
            Reason: "Not configured sender test");

        using var response = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CreateEmployeeResponse>();
        Assert.NotNull(result);
        // Must NOT report sent
        Assert.NotEqual("Sent", result.DeliveryStatus);
        Assert.Equal("NotConfiguredPendingDelivery", result.DeliveryStatus);

        // In DB outbox: Pending status
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var outbox = await db.ActivationOutboxMessages.FirstOrDefaultAsync(o => o.Email == email);
        Assert.NotNull(outbox);
        Assert.Equal("Pending", outbox.Status);
    }

    [Fact]
    public async Task Employee_Create_DoesNotAutoAssignHrScope_RequiresAdminAssignment()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-scope-check", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var createReq = new CreateEmployeeRequest(
            FullName: "Unscoped Employee",
            CitizenId: "038099888333",
            Email: $"unscoped-{Guid.NewGuid():N}@example.invalid",
            Phone: "0900000006",
            Address: "Unscoped Address",
            DateOfBirth: new DateOnly(1994, 4, 4),
            Gender: "Nam",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DocThan",
            DepartmentId: null,
            PositionId: null,
            Reason: "Scope check");

        Guid newEmpId;
        using (var res = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq))
        {
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var created = await res.Content.ReadFromJsonAsync<CreateEmployeeResponse>();
            Assert.NotNull(created);
            newEmpId = created.Profile.Id;
        }

        // 1. HR immediately calls GET on the newly created employee -> 403 Forbidden!
        // New record is NOT auto-scoped to HR; awaits admin assignment
        using (var getRes = await hrClient.GetAsync($"/api/v1/employees/{newEmpId}"))
        {
            await ProblemAsync(getRes, HttpStatusCode.Forbidden);
        }

        // 2. Admin explicitly assigns scope via existing T07 endpoint
        var adminUser = await CreateUserWithRolesAsync(harness, "admin-assigner", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        using (var assignRes = await adminClient.PostAsJsonAsync($"/api/v1/accounts/{hrUser.Id}/scopes", new AssignScopesRequest([newEmpId], "Assign new hire")))
        {
            Assert.Equal(HttpStatusCode.NoContent, assignRes.StatusCode);
        }

        // 3. Now HR can read the employee profile
        using (var getRes2 = await hrClient.GetAsync($"/api/v1/employees/{newEmpId}"))
        {
            Assert.Equal(HttpStatusCode.OK, getRes2.StatusCode);
            var profile = await getRes2.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal("Unscoped Employee", profile.FullName);
        }
    }

    [Fact]
    public async Task Employee_CodeGeneration_ConcurrentRequests_ProduceUniqueCodes()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        const int concurrency = 20;
        var tasks = Enumerable.Range(0, concurrency).Select(async _ =>
        {
            await using var scope = harness.Factory.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            return await db.GenerateEmployeeCodeAsync(2026, CancellationToken.None);
        });

        var codes = await Task.WhenAll(tasks);
        Assert.Equal(concurrency, codes.Length);
        Assert.Equal(concurrency, codes.Distinct().Count());
        Assert.All(codes, code => Assert.Matches(@"^NV2026\d{4}$", code));
    }

    [Fact]
    public async Task Employee_CodeGeneration_Exceeds9999_FailsWithClearError()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            // Exhaust an isolated year; do not rewind an active year counter.
            await db.Database.ExecuteSqlRawAsync("INSERT INTO [personnel].[EmployeeCodeCounters] ([Year],[LastValue]) VALUES (2088,9998);");

            // Sequence value 9999 succeeds
            var code9999 = await db.GenerateEmployeeCodeAsync(2088, CancellationToken.None);
            Assert.Equal("NV20889999", code9999);

            // Attempting value 10000 throws DomainRuleException
            var ex = await Assert.ThrowsAsync<DomainRuleException>(() => db.GenerateEmployeeCodeAsync(2088, CancellationToken.None));
            Assert.Equal("employees.code_exhausted", ex.Code);

            // Another year has its own range, even after 2088 is exhausted.
            Assert.Equal("NV20890001", await db.GenerateEmployeeCodeAsync(2089, CancellationToken.None));
        }
    }

    [Fact]
    public async Task Employee_Patch_AbsentVsNullVsValue()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-patcher", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "patch-target", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Patch Subject", "038099888222");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // Set initial address and marital status
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var p = await db.EmployeeProfiles.AsTracking().SingleAsync(e => e.Id == emp.Id);
            p.Phone = "0901111111";
            p.Address = "Original Address";
            p.MaritalStatus = "DocThan";
            await db.SaveChangesAsync();
        }

        // 1. Absent fields: PATCH with ONLY phone keeps address and marital status intact
        var getRes1 = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current1 = await getRes1.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var patch1Json = JsonSerializer.Serialize(new
        {
            phone = "0902222222",
            reason = "Change phone only",
            rowVersion = current1!.RowVersion
        });

        using (var res = await hrClient.PatchAsync($"/api/v1/employees/{emp.Id}", new StringContent(patch1Json, Encoding.UTF8, "application/json")))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var updated = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(updated);
            Assert.Equal("0902222222", updated.Phone);
            Assert.Equal("Original Address", updated.Address); // Preserved
            Assert.Equal("DocThan", updated.MaritalStatus); // Preserved
        }

        // 2. Explicit null on optional marital status clears that field, preserving address.
        var getRes2 = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current2 = await getRes2.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var patch2Json = JsonSerializer.Serialize(new
        {
            maritalStatus = (string?)null,
            reason = "Clear optional marital status",
            rowVersion = current2!.RowVersion
        });

        using (var res = await hrClient.PatchAsync($"/api/v1/employees/{emp.Id}", new StringContent(patch2Json, Encoding.UTF8, "application/json")))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var updated = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(updated);
            Assert.Null(updated.MaritalStatus);
            Assert.Equal("Original Address", updated.Address);
            Assert.Equal("0902222222", updated.Phone); // Phone intact
        }

        // 3. Explicit null on required field (fullName) returns 400 Bad Request
        var getRes3 = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current3 = await getRes3.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var patch3Json = JsonSerializer.Serialize(new
        {
            fullName = (string?)null,
            reason = "Illegal clear of required name",
            rowVersion = current3!.RowVersion
        });

        using (var res = await hrClient.PatchAsync($"/api/v1/employees/{emp.Id}", new StringContent(patch3Json, Encoding.UTF8, "application/json")))
        {
            await ProblemAsync(res, HttpStatusCode.BadRequest);
        }

        // 4. Sensitive unknown fields (salary, role, employeeCode, status, userId) have no effect
        var getRes4 = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current4 = await getRes4.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var patch4Json = JsonSerializer.Serialize(new
        {
            phone = "0903333333",
            reason = "Attempt sensitive field overwrite",
            rowVersion = current4!.RowVersion,
            salary = 50000000m,
            role = "SystemAdmin",
            employeeCode = "NV20999999",
            status = "DaNghiViec",
            userId = Guid.NewGuid()
        });

        using (var res = await hrClient.PatchAsync($"/api/v1/employees/{emp.Id}", new StringContent(patch4Json, Encoding.UTF8, "application/json")))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var updated = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(updated);
            Assert.Equal("0903333333", updated.Phone);
            Assert.Equal("DangLamViec", updated.Status); // Unchanged
            Assert.NotEqual("NV20999999", updated.EmployeeCode); // Unchanged
            Assert.Equal(targetUser.Id, updated.UserId); // Unchanged
        }
    }

    [Fact]
    public async Task Employee_Patch_ConcurrencyRowVersion_OneSuccessOneConflict()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser1 = await CreateUserWithRolesAsync(harness, "hr-conc1", HrmRoles.Hr);
        var hrUser2 = await CreateUserWithRolesAsync(harness, "hr-conc2", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "conc-target", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Concurrency Subject", "038099888111");

        await AssignHrScopeAsync(harness, hrUser1.Id, emp.Id);
        await AssignHrScopeAsync(harness, hrUser2.Id, emp.Id);

        using var client1 = await BearerClientAsync(harness, hrUser1);
        using var client2 = await BearerClientAsync(harness, hrUser2);

        // Both read the initial profile and obtain the same RowVersion
        var get1 = await client1.GetAsync($"/api/v1/employees/{emp.Id}");
        var profile1 = await get1.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
        Assert.NotNull(profile1?.RowVersion);

        var get2 = await client2.GetAsync($"/api/v1/employees/{emp.Id}");
        var profile2 = await get2.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
        Assert.NotNull(profile2?.RowVersion);
        Assert.Equal(profile1.RowVersion, profile2.RowVersion);

        // First HR updates successfully
        var update1 = new UpdateEmployeeRequest("0901111111", "First Address", "First update", profile1.RowVersion);
        using (var res1 = await client1.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", update1))
        {
            Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        }

        // Second HR attempts update using the now-stale RowVersion -> 409 Conflict
        var update2 = new UpdateEmployeeRequest("0902222222", "Second Address", "Second update", profile2.RowVersion);
        using (var res2 = await client2.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", update2))
        {
            await ProblemAsync(res2, HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Employee_Offboarding_RevokesSessions_DisablesUser_AndSetsDaNghiViec()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-offboarder", HrmRoles.Hr);
        var empUser = await CreateUserWithRolesAsync(harness, "emp-offboardee", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, empUser, "Offboard Subject", "038099888000");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // Employee establishes an active session
        using var empClient = harness.Client();
        var mobileGrant = await MobileAsync(empClient, empUser);
        UseBearer(empClient, mobileGrant.AccessToken);

        // Employee can initially call /api/v1/me/profile
        using (var meRes = await empClient.GetAsync("/api/v1/me/profile"))
        {
            Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
        }

        // HR reads employee to get current RowVersion
        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
        Assert.NotNull(current?.RowVersion);

        // HR offboards employee
        var offboardReq = new OffboardEmployeeRequest(
            TerminationDate: new DateOnly(2026, 11, 11),
            Reason: "Voluntary departure approved",
            RowVersion: current.RowVersion);

        using (var offboardRes = await hrClient.PostAsJsonAsync($"/api/v1/employees/{emp.Id}/offboard", offboardReq))
        {
            Assert.Equal(HttpStatusCode.NoContent, offboardRes.StatusCode);
        }

        // In DB: Status is DaNghiViec, user IsActive is false, TerminationDate is saved
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var p = await db.EmployeeProfiles.SingleAsync(e => e.Id == emp.Id);
            Assert.Equal("DaNghiViec", p.Status);
            Assert.Equal(new DateOnly(2026, 11, 11), p.TerminationDate);

            var u = await db.Users.SingleAsync(u => u.Id == empUser.Id);
            Assert.False(u.IsActive);
        }

        // Immediate subsequent request with existing bearer token -> 401 Unauthorized
        using (var rejectedRes = await empClient.GetAsync("/api/v1/me/profile"))
        {
            await ProblemAsync(rejectedRes, HttpStatusCode.Unauthorized);
        }

        // Attempting to log in again with offboarded account -> rejected
        using var newLoginClient = harness.Client();
        using (var loginAttempt = await newLoginClient.PostAsJsonAsync("/api/v1/auth/mobile/login", new { username = empUser.UserName, password = AuthPassword }))
        {
            await ProblemAsync(loginAttempt, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Employee_Offboarded_InactiveInvitedUser_CannotActivateOrResetEvenWithToken()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-invite-off", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var email = $"invited-off-{Guid.NewGuid():N}@example.invalid";
        var createReq = new CreateEmployeeRequest(
            FullName: "Invited Then Offboarded",
            CitizenId: "038099777111",
            Email: email,
            Phone: "0901234567",
            Address: "Pending Address",
            DateOfBirth: new DateOnly(1995, 1, 1),
            Gender: "Nam",
            HireDate: new DateOnly(2026, 1, 1),
            MaritalStatus: "DocThan",
            DepartmentId: null,
            PositionId: null,
            Reason: "New hire before offboard");

        Guid empId;
        using (var res = await hrClient.PostAsJsonAsync("/api/v1/employees", createReq))
        {
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var created = await res.Content.ReadFromJsonAsync<CreateEmployeeResponse>();
            Assert.NotNull(created);
            empId = created.Profile.Id;
        }

        // Admin assigns scope so HR can offboard
        var adminUser = await CreateUserWithRolesAsync(harness, "admin-invite-scope", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);
        await adminClient.PostAsJsonAsync($"/api/v1/accounts/{hrUser.Id}/scopes", new AssignScopesRequest([empId], "Scope for offboard"));

        // Capture invitation message
        var message = harness.Sender.Messages.FirstOrDefault(m => m.Email == email && m.Purpose == "activation");
        Assert.NotNull(message);

        // HR offboards this inactive/invited employee
        var getRes = await hrClient.GetAsync($"/api/v1/employees/{empId}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var offboardReq = new OffboardEmployeeRequest(
            TerminationDate: new DateOnly(2026, 1, 15),
            Reason: "Candidate revoked acceptance before start date",
            RowVersion: current!.RowVersion!);

        using (var offRes = await hrClient.PostAsJsonAsync($"/api/v1/employees/{empId}/offboard", offboardReq))
        {
            Assert.Equal(HttpStatusCode.NoContent, offRes.StatusCode);
        }

        // Attempting to activate with the previous token -> fails
        using var anonClient = harness.Client();
        var activateReq = new ResetRequest(message.UserId, message.Token, AuthPassword);
        using (var actRes = await anonClient.PostAsJsonAsync("/api/v1/auth/activate", activateReq))
        {
            var activationProblem = await ProblemAsync(actRes, HttpStatusCode.BadRequest);
            Assert.Equal("auth.activation_invalid", activationProblem.GetProperty("errorCode").GetString());
        }

        // Even with a freshly generated activation token, ResetAsync must reject activation while DaNghiViec
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
            var accountService = scope.ServiceProvider.GetRequiredService<AccountService>();
            var user = (await userManager.FindByEmailAsync(email))!;
            var freshToken = await userManager.GenerateUserTokenAsync(user, HrmClaims.ActivationProvider, HrmClaims.ActivationPurpose);

            var activated = await accountService.ResetAsync(user.Id, freshToken, AuthPassword, activate: true, CancellationToken.None);
            Assert.False(activated);
        }
    }

    [Fact]
    public async Task Employee_Offboarding_RetainsPayslipAndLeaveHistory_PreventsLeaveApprovalAfterTermination()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-retainer", HrmRoles.Hr);
        var empUser = await CreateUserWithRolesAsync(harness, "emp-retainee", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, empUser, "Retained History Subject", "038099777222");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        Guid leaveId;
        Guid payslipId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            var period = new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-10-RET",
                Status = "Published",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.PayrollPeriods.Add(period);

            var payslip = new PayslipRecord
            {
                Id = Guid.NewGuid(),
                PeriodId = period.Id,
                EmployeeId = emp.Id,
                Amount = 20000000m,
                Status = "Published",
                PublishedAtUtc = clock.GetUtcNow()
            };
            db.Payslips.Add(payslip);
            payslipId = payslip.Id;

            // Leave scheduled for December 2026
            var leave = new LeaveRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                StartDate = new DateOnly(2026, 12, 1),
                EndDate = new DateOnly(2026, 12, 5),
                Reason = "Future vacation",
                Status = "Pending",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.LeaveRecords.Add(leave);
            leaveId = leave.Id;

            await db.SaveChangesAsync();
        }

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // HR offboards employee effective November 11, 2026
        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var offboardReq = new OffboardEmployeeRequest(
            TerminationDate: new DateOnly(2026, 11, 11),
            Reason: "Offboarding with retained history",
            RowVersion: current!.RowVersion!);

        using (var offRes = await hrClient.PostAsJsonAsync($"/api/v1/employees/{emp.Id}/offboard", offboardReq))
        {
            Assert.Equal(HttpStatusCode.NoContent, offRes.StatusCode);
        }

        // Verify history is retained: payslip and leave record remain in DB
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var ps = await db.Payslips.SingleOrDefaultAsync(p => p.Id == payslipId);
            Assert.NotNull(ps);
            Assert.Equal("Published", ps.Status);

            var lr = await db.LeaveRecords.SingleOrDefaultAsync(l => l.Id == leaveId);
            Assert.NotNull(lr);
            Assert.Equal("Pending", lr.Status); // Not auto-cancelled
        }

        // Attempting to approve leave whose date (December 2026) is AFTER termination date (Nov 15) -> 409 Conflict
        using (var appRes = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leaveId}/approve", new ApproveLeaveRequest("Illegal post-termination approval")))
        {
            await ProblemAsync(appRes, HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task Employee_Offboarded_CannotBePatchedOrOffboardedAgain()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-double-off", HrmRoles.Hr);
        var empUser = await CreateUserWithRolesAsync(harness, "emp-double-off", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, empUser, "Double Offboard Subject", "038099777333");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var offboardReq = new OffboardEmployeeRequest(
            TerminationDate: new DateOnly(2026, 11, 1),
            Reason: "First offboard",
            RowVersion: current!.RowVersion!);

        using (var res1 = await hrClient.PostAsJsonAsync($"/api/v1/employees/{emp.Id}/offboard", offboardReq))
        {
            Assert.Equal(HttpStatusCode.NoContent, res1.StatusCode);
        }

        // Read again to get latest token
        var getRes2 = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current2 = await getRes2.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        // 1. Calling offboard again on DaNghiViec employee -> 409 Conflict
        using (var res2 = await hrClient.PostAsJsonAsync($"/api/v1/employees/{emp.Id}/offboard", offboardReq with { RowVersion = current2!.RowVersion! }))
        {
            await ProblemAsync(res2, HttpStatusCode.Conflict);
        }

        // 2. Calling PATCH on DaNghiViec employee -> 409 Conflict
        var patchReq = new UpdateEmployeeRequest("0909999999", null, "Attempt modify offboarded", current2.RowVersion);
        using (var patchRes = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", patchReq))
        {
            await ProblemAsync(patchRes, HttpStatusCode.Conflict);
        }
    }
}

public sealed class FailingAuditRepository : IAuditRepository
{
    public bool ShouldFail { get; set; }
    public Task RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("Simulated audit store database failure for rollback test.");
        }
        return Task.CompletedTask;
    }

    public Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters parameters, CancellationToken token) =>
        Task.FromResult(new PagedResult<AuditEventResponse>([], parameters.Page, parameters.PageSize, 0));
}
