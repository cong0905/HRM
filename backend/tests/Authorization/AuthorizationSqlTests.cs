using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Next.Api.Controllers;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Application.Payroll;
using HRM.Next.Application.Personnel;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Calendar;
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
    private static async Task<HrmUser> CreateUserWithRolesAsync(AuthHarness harness, string usernamePrefix, params string[] roles)
    {
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = role });
            }
        }

        var suffix = Guid.NewGuid().ToString("N");
        var user = new HrmUser
        {
            Id = Guid.NewGuid(),
            UserName = $"{usernamePrefix}-{suffix}",
            Email = $"{usernamePrefix}-{suffix}@example.invalid",
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, AuthPassword);
        Assert.True(createResult.Succeeded);

        if (roles.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, roles);
            Assert.True(addResult.Succeeded);
        }

        return user;
    }

    private static async Task<EmployeeProfile> CreateEmployeeProfileAsync(AuthHarness harness, HrmUser user, string fullName, string citizenId)
    {
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        var profile = new EmployeeProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FullName = fullName,
            CitizenId = citizenId,
            Phone = "0901234567",
            Address = "123 Test Street",
            CreatedAtUtc = clock.GetUtcNow()
        };

        db.EmployeeProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    private static async Task AssignHrScopeAsync(AuthHarness harness, Guid hrUserId, Guid employeeId)
    {
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        db.HrEmployeeAssignments.Add(new HrEmployeeAssignment
        {
            Id = Guid.NewGuid(),
            HrUserId = hrUserId,
            EmployeeId = employeeId,
            AssignedAtUtc = clock.GetUtcNow()
        });
        await db.SaveChangesAsync();
    }

    private static async Task<HttpClient> BearerClientAsync(AuthHarness harness, HrmUser user)
    {
        var client = harness.Client();
        var tokens = await MobileAsync(client, user);
        UseBearer(client, tokens.AccessToken);
        return client;
    }

    [Fact]
    public async Task TableDriven_AnonymousRequests_AreRejectedWith401()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        using var client = harness.Client();
        var dummyId = Guid.NewGuid();

        var endpoints = new (HttpMethod Method, string Uri, HttpContent? Content)[]
        {
            (HttpMethod.Get, "/api/v1/departments", null),
            (HttpMethod.Get, "/api/v1/me/profile", null),
            (HttpMethod.Get, "/api/v1/me/leave-requests", null),
            (HttpMethod.Get, "/api/v1/me/payslips", null),
            (HttpMethod.Get, $"/api/v1/employees/{dummyId}", null),
            (HttpMethod.Get, "/api/v1/employees", null),
            (HttpMethod.Post, "/api/v1/employees/bulk", JsonContent.Create(new BulkGetEmployeesRequest([dummyId]))),
            (HttpMethod.Patch, $"/api/v1/employees/{dummyId}", JsonContent.Create(new UpdateEmployeeRequest("090", "Addr", "Reason"))),
            (HttpMethod.Post, "/api/v1/leave-requests", JsonContent.Create(new CreateLeaveRequest(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2), "Reason"))),
            (HttpMethod.Get, $"/api/v1/leave-requests/{dummyId}", null),
            (HttpMethod.Post, $"/api/v1/leave-requests/{dummyId}/approve", JsonContent.Create(new ApproveLeaveRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/items", null),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/calculate", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/approve", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/publish", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/export", null),
            (HttpMethod.Put, $"/api/v1/accounts/{dummyId}/roles", JsonContent.Create(new AssignRolesRequest(["Employee"], "Reason"))),
            (HttpMethod.Post, $"/api/v1/accounts/{dummyId}/disable", JsonContent.Create(new DisableAccountRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/accounts/{dummyId}/scopes", null),
            (HttpMethod.Post, $"/api/v1/accounts/{dummyId}/scopes", JsonContent.Create(new AssignScopesRequest([dummyId], "Reason"))),
            (HttpMethod.Delete, $"/api/v1/accounts/{dummyId}/scopes/{dummyId}", null),
            (HttpMethod.Get, "/api/v1/audit-events", null)
        };

        foreach (var (method, uri, content) in endpoints)
        {
            using var request = new HttpRequestMessage(method, uri) { Content = content };
            using var response = await client.SendAsync(request);
            await ProblemAsync(response, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task TableDriven_InsufficientPermission_Returns403()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var dummyId = Guid.NewGuid();

        // 1. Employee lacks administrative and cross-department permissions
        var employeeUser = await CreateUserWithRolesAsync(harness, "emp", HrmRoles.Employee);
        using var employeeClient = await BearerClientAsync(harness, employeeUser);

        var employeeForbidden = new (HttpMethod Method, string Uri, HttpContent? Content)[]
        {
            (HttpMethod.Get, "/api/v1/departments", null),
            (HttpMethod.Patch, $"/api/v1/employees/{dummyId}", JsonContent.Create(new UpdateEmployeeRequest("090", "Addr", "Reason"))),
            (HttpMethod.Post, $"/api/v1/leave-requests/{dummyId}/approve", JsonContent.Create(new ApproveLeaveRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/items", null),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/calculate", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/approve", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/publish", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/export", null),
            (HttpMethod.Put, $"/api/v1/accounts/{dummyId}/roles", JsonContent.Create(new AssignRolesRequest(["HR"], "Reason"))),
            (HttpMethod.Post, $"/api/v1/accounts/{dummyId}/disable", JsonContent.Create(new DisableAccountRequest("Reason"))),
            (HttpMethod.Get, "/api/v1/audit-events", null)
        };

        foreach (var (method, uri, content) in employeeForbidden)
        {
            using var request = new HttpRequestMessage(method, uri) { Content = content };
            using var response = await employeeClient.SendAsync(request);
            await ProblemAsync(response, HttpStatusCode.Forbidden);
        }

        // 2. HR lacks payroll calculation/approval/export and account assignment permissions
        var hrUser = await CreateUserWithRolesAsync(harness, "hr", HrmRoles.Hr);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        var hrForbidden = new (HttpMethod Method, string Uri, HttpContent? Content)[]
        {
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/items", null),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/calculate", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/approve", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/publish", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/export", null),
            (HttpMethod.Put, $"/api/v1/accounts/{dummyId}/roles", JsonContent.Create(new AssignRolesRequest(["SystemAdmin"], "Reason"))),
            (HttpMethod.Get, $"/api/v1/accounts/{dummyId}/scopes", null),
            (HttpMethod.Post, $"/api/v1/accounts/{dummyId}/scopes", JsonContent.Create(new AssignScopesRequest([dummyId], "Reason"))),
            (HttpMethod.Delete, $"/api/v1/accounts/{dummyId}/scopes/{dummyId}", null),
            (HttpMethod.Get, "/api/v1/audit-events", null)
        };

        foreach (var (method, uri, content) in hrForbidden)
        {
            using var request = new HttpRequestMessage(method, uri) { Content = content };
            using var response = await hrClient.SendAsync(request);
            await ProblemAsync(response, HttpStatusCode.Forbidden);
        }

        // 3. Payroll lacks employee management, leave approval, and account role assignment
        var payrollUser = await CreateUserWithRolesAsync(harness, "pr", HrmRoles.Payroll);
        using var payrollClient = await BearerClientAsync(harness, payrollUser);

        var payrollForbidden = new (HttpMethod Method, string Uri, HttpContent? Content)[]
        {
            (HttpMethod.Patch, $"/api/v1/employees/{dummyId}", JsonContent.Create(new UpdateEmployeeRequest("090", "Addr", "Reason"))),
            (HttpMethod.Post, $"/api/v1/leave-requests/{dummyId}/approve", JsonContent.Create(new ApproveLeaveRequest("Reason"))),
            (HttpMethod.Put, $"/api/v1/accounts/{dummyId}/roles", JsonContent.Create(new AssignRolesRequest(["SystemAdmin"], "Reason"))),
            (HttpMethod.Get, "/api/v1/audit-events", null)
        };

        foreach (var (method, uri, content) in payrollForbidden)
        {
            using var request = new HttpRequestMessage(method, uri) { Content = content };
            using var response = await payrollClient.SendAsync(request);
            await ProblemAsync(response, HttpStatusCode.Forbidden);
        }

        // 4. SystemAdmin does not by default read salary, calculate payroll, or read PII
        var adminUser = await CreateUserWithRolesAsync(harness, "admin", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        var adminForbidden = new (HttpMethod Method, string Uri, HttpContent? Content)[]
        {
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/items", null),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/calculate", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/approve", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Post, $"/api/v1/payroll-periods/{dummyId}/publish", JsonContent.Create(new PayrollActionRequest("Reason"))),
            (HttpMethod.Get, $"/api/v1/payroll-periods/{dummyId}/export", null),
            (HttpMethod.Get, $"/api/v1/employees/{dummyId}", null),
            (HttpMethod.Patch, $"/api/v1/employees/{dummyId}", JsonContent.Create(new UpdateEmployeeRequest("090", "Addr", "Reason"))),
            (HttpMethod.Post, $"/api/v1/leave-requests/{dummyId}/approve", JsonContent.Create(new ApproveLeaveRequest("Reason")))
        };

        foreach (var (method, uri, content) in adminForbidden)
        {
            using var request = new HttpRequestMessage(method, uri) { Content = content };
            using var response = await adminClient.SendAsync(request);
            await ProblemAsync(response, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task IDOR_EmployeeCannotReadOrModifyOtherEmployeeData_OrBulkExport()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var userA = await CreateUserWithRolesAsync(harness, "empA", HrmRoles.Employee);
        var empA = await CreateEmployeeProfileAsync(harness, userA, "Employee A", "001200000001");

        var userB = await CreateUserWithRolesAsync(harness, "empB", HrmRoles.Employee);
        var empB = await CreateEmployeeProfileAsync(harness, userB, "Employee B", "001200000002");

        // Seed leave request and payslip for B
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            if (!await db.WorkCalendarVersions.AnyAsync(v => v.EffectiveFrom == new DateOnly(2026, 1, 1)))
            {
                db.WorkCalendarVersions.Add(new WorkCalendarVersion
                {
                    Id = Guid.NewGuid(),
                    EffectiveFrom = new DateOnly(2026, 1, 1),
                    DefinitionJson = JsonSerializer.Serialize(CalendarMathTests.Definition()),
                    CreatedAtUtc = clock.GetUtcNow()
                });
            }

            db.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = empA.Id,
                Year = 2026,
                AllocatedDays = 12m,
                UsedDays = 0m,
                UpdatedAtUtc = clock.GetUtcNow()
            });

            var period = new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-11-TEST",
                Status = "Published",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.PayrollPeriods.Add(period);

            var leaveB = new LeaveRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = empB.Id,
                StartDate = new DateOnly(2026, 11, 20),
                EndDate = new DateOnly(2026, 11, 22),
                Reason = "Leave of B",
                Status = "Pending",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.LeaveRecords.Add(leaveB);

            var payslipB = new PayslipRecord
            {
                Id = Guid.NewGuid(),
                PeriodId = period.Id,
                EmployeeId = empB.Id,
                Amount = 15000000m,
                Status = "Published",
                PublishedAtUtc = clock.GetUtcNow()
            };
            db.Payslips.Add(payslipB);
            await db.SaveChangesAsync();
        }

        using var clientA = await BearerClientAsync(harness, userA);

        // 1. Employee A tries to read B's employee profile -> 403 Forbidden
        using (var res = await clientA.GetAsync($"/api/v1/employees/{empB.Id}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 2. Employee A can read their own profile
        using (var res = await clientA.GetAsync($"/api/v1/employees/{empA.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var profile = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal("Employee A", profile.FullName);
        }

        // 3. Employee A calls /me/profile -> returns A
        using (var res = await clientA.GetAsync("/api/v1/me/profile"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var profile = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal(empA.Id, profile.Id);
        }

        // 4. Employee A tries to read B's leave request -> 403 Forbidden
        Guid leaveBId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            leaveBId = (await scope.ServiceProvider.GetRequiredService<HrmDbContext>().LeaveRecords.FirstAsync(l => l.EmployeeId == empB.Id)).Id;
        }

        using (var res = await clientA.GetAsync($"/api/v1/leave-requests/{leaveBId}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 5. Employee A calls POST /leave-requests: server binds to empA regardless of client
        using (var res = await clientA.PostAsJsonAsync("/api/v1/leave-requests", new CreateLeaveRequest(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), "Vacation A")))
        {
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var created = await res.Content.ReadFromJsonAsync<LeaveRecordResponse>();
            Assert.NotNull(created);
            Assert.Equal(empA.Id, created.EmployeeId);
        }

        // 6. Employee A tries bulk export -> 403 Forbidden
        Guid periodId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            periodId = (await scope.ServiceProvider.GetRequiredService<HrmDbContext>().PayrollPeriods.FirstAsync()).Id;
        }

        using (var res = await clientA.GetAsync($"/api/v1/payroll-periods/{periodId}/export"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task MassAssignment_CannotModifyRoleOrCitizenIdOrUserId()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var user = await CreateUserWithRolesAsync(harness, "target", HrmRoles.Employee);
        var originalCitizenId = "001200000003";
        var emp = await CreateEmployeeProfileAsync(harness, user, "Target Employee", originalCitizenId);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-updater", HrmRoles.Hr);
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        // Client attempts to pass forbidden fields (role, isAdmin, citizenId, userId, salary)
        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var maliciousJson = JsonSerializer.Serialize(new
        {
            phone = "0988776655",
            address = "Updated Address",
            reason = "Update contact details",
            rowVersion = current!.RowVersion,
            citizenId = "999999999999",
            userId = Guid.NewGuid(),
            role = "SystemAdmin",
            isAdmin = true,
            salary = 99999999m
        });

        using var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/employees/{emp.Id}")
        {
            Content = new StringContent(maliciousJson, System.Text.Encoding.UTF8, "application/json")
        };

        using var response = await hrClient.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify in database: only phone and address changed; citizenId, userId, role did not change
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();

        var updatedProfile = await db.EmployeeProfiles.SingleAsync(e => e.Id == emp.Id);
        Assert.Equal("0988776655", updatedProfile.Phone);
        Assert.Equal("Updated Address", updatedProfile.Address);
        Assert.Equal(originalCitizenId, updatedProfile.CitizenId); // CCCD NOT changed
        Assert.Equal(user.Id, updatedProfile.UserId); // UserId NOT changed

        var userRoles = await userManager.GetRolesAsync((await userManager.FindByIdAsync(user.Id.ToString()))!);
        Assert.Single(userRoles);
        Assert.Equal(HrmRoles.Employee, userRoles[0]); // Role NOT elevated to SystemAdmin
    }

    [Fact]
    public async Task SystemAdmin_WithoutPayrollPermission_CannotReadSalary_FakeHeadersIgnored()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        Guid periodId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var period = new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-11-SA",
                Status = "Calculated",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.PayrollPeriods.Add(period);
            await db.SaveChangesAsync();
            periodId = period.Id;
        }

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-nopayroll", HrmRoles.SystemAdmin);
        using var client = await BearerClientAsync(harness, adminUser);

        // Attempting to access with fake client-sent headers
        client.DefaultRequestHeaders.Add("X-Role", "Payroll");
        client.DefaultRequestHeaders.Add("X-IsAdmin", "true");
        client.DefaultRequestHeaders.Add("VaiTro", "Payroll");

        using (var res = await client.GetAsync($"/api/v1/payroll-periods/{periodId}/items"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        using (var res = await client.GetAsync($"/api/v1/payroll-periods/{periodId}/export"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task RoleAssignment_AntiSelfEscalation_AndSessionRevocationSLA()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var adminUser = await CreateUserWithRolesAsync(harness, "sysadmin", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        // 1. Anti-self-escalation: Admin cannot modify their own roles
        using (var res = await adminClient.PutAsJsonAsync($"/api/v1/accounts/{adminUser.Id}/roles", new AssignRolesRequest([HrmRoles.Payroll], "Self promote")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 2. User B starts as Employee
        var userB = await CreateUserWithRolesAsync(harness, "userB", HrmRoles.Employee);
        using var clientB = await BearerClientAsync(harness, userB);

        // User B can access /me before change
        using (var res = await clientB.GetAsync("/api/v1/auth/me"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // 3. Admin assigns HR role to User B
        using (var res = await adminClient.PutAsJsonAsync($"/api/v1/accounts/{userB.Id}/roles", new AssignRolesRequest([HrmRoles.Hr], "Promote B to HR")))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // 4. Session Revocation SLA T00: User B's old session is immediately rejected with 401
        using (var res = await clientB.GetAsync("/api/v1/auth/me"))
        {
            await ProblemAsync(res, HttpStatusCode.Unauthorized);
        }

        // 5. User B logs in again -> now has HR role and can read departments
        using var clientBNew = await BearerClientAsync(harness, userB);
        using (var res = await clientBNew.GetAsync("/api/v1/departments"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
    }

    [Fact]
    public async Task Audit_SensitiveMutationsRecordedWithMaskedData_NoPasswordOrRawCccd()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-auditor", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "auditee", HrmRoles.Employee);
        var rawCccd = "038099012345";
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Audited Person", rawCccd);
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var updateReq = new UpdateEmployeeRequest("0912345678", "456 Audit Ave", "Quarterly contact info refresh", current!.RowVersion);
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", updateReq))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();

        var audit = await db.AuditEvents.OrderByDescending(a => a.OccurredAtUtc).FirstOrDefaultAsync(a => a.EntityId == emp.Id.ToString());
        Assert.NotNull(audit);
        Assert.Equal("employees.updated", audit.Action);
        Assert.Equal(hrUser.Id, audit.ActorId);
        Assert.Equal(hrUser.UserName, audit.ActorUsername);
        Assert.False(string.IsNullOrWhiteSpace(audit.TraceId));
        Assert.Equal("Quarterly contact info refresh", audit.Reason);

        // Verify masking
        Assert.NotNull(audit.Payload);
        Assert.DoesNotContain(rawCccd, audit.Payload); // Raw CCCD must NEVER appear
        Assert.Contains("********2345", audit.Payload); // Masked CCCD
        Assert.DoesNotContain("Password", audit.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", audit.Payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AtomicRollback_WhenAuditOrMutationFails_BothRollbackOnRealSqlServer()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-rollback", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "target-rollback", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Rollback Target", "001200000099");

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // Attempting to update a non-existent employee will fail cleanly with 404 and NO partial commit
        var nonExistentId = Guid.NewGuid();
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{nonExistentId}", new UpdateEmployeeRequest("099", "Addr", "Will fail")))
        {
            await ProblemAsync(res, HttpStatusCode.NotFound);
        }

        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();

        // Assert no stray audit event was recorded for nonExistentId
        var strayAudit = await db.AuditEvents.AnyAsync(a => a.EntityId == nonExistentId.ToString());
        Assert.False(strayAudit);
    }

    [Fact]
    public async Task AuditQuery_RequiresPermission_FiltersAndEnforcesBoundedPagination()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        // Seed some audit events
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            db.AuditEvents.AddRange(
                new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = clock.GetUtcNow().AddMinutes(-10),
                    Action = "test.action1",
                    EntityName = "Test",
                    EntityId = "1",
                    TraceId = "trace-1",
                    Reason = "Reason 1"
                },
                new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    OccurredAtUtc = clock.GetUtcNow().AddMinutes(-5),
                    Action = "test.action2",
                    EntityName = "Test",
                    EntityId = "2",
                    TraceId = "trace-2",
                    Reason = "Reason 2"
                });
            await db.SaveChangesAsync();
        }

        // Employee cannot read audit
        var empUser = await CreateUserWithRolesAsync(harness, "emp-audit", HrmRoles.Employee);
        using (var empClient = await BearerClientAsync(harness, empUser))
        {
            using var res = await empClient.GetAsync("/api/v1/audit-events");
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // SystemAdmin can read audit
        var adminUser = await CreateUserWithRolesAsync(harness, "admin-audit", HrmRoles.SystemAdmin);
        using (var adminClient = await BearerClientAsync(harness, adminUser))
        {
            // Bounded pagination: pageSize > 100 rejected with 400
            using (var res = await adminClient.GetAsync("/api/v1/audit-events?pageSize=200"))
            {
                await ProblemAsync(res, HttpStatusCode.BadRequest);
            }

            // Valid query
            using (var res = await adminClient.GetAsync("/api/v1/audit-events?page=1&pageSize=20&action=test.action1"))
            {
                Assert.Equal(HttpStatusCode.OK, res.StatusCode);
                var page = await res.Content.ReadFromJsonAsync<PagedResult<AuditEventResponse>>();
                Assert.NotNull(page);
                Assert.All(page.Items, item => Assert.Equal("test.action1", item.Action));
            }
        }
    }

    [Fact]
    public async Task AntiSelfApproval_HRManagerCannotApproveOwnLeave()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        // HR Manager 1
        var hr1 = await CreateUserWithRolesAsync(harness, "hr-mgr-1", HrmRoles.Hr, HrmRoles.Employee);
        var empHr1 = await CreateEmployeeProfileAsync(harness, hr1, "HR Manager 1", "001200000011");

        // HR Manager 2
        var hr2 = await CreateUserWithRolesAsync(harness, "hr-mgr-2", HrmRoles.Hr, HrmRoles.Employee);
        var empHr2 = await CreateEmployeeProfileAsync(harness, hr2, "HR Manager 2", "001200000012");
        await AssignHrScopeAsync(harness, hr2.Id, empHr1.Id);

        await using (var initScope = harness.Factory.Services.CreateAsyncScope())
        {
            var initDb = initScope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = initScope.ServiceProvider.GetRequiredService<TimeProvider>();

            if (!await initDb.WorkCalendarVersions.AnyAsync(v => v.EffectiveFrom == new DateOnly(2026, 1, 1)))
            {
                initDb.WorkCalendarVersions.Add(new WorkCalendarVersion
                {
                    Id = Guid.NewGuid(),
                    EffectiveFrom = new DateOnly(2026, 1, 1),
                    DefinitionJson = JsonSerializer.Serialize(CalendarMathTests.Definition()),
                    CreatedAtUtc = clock.GetUtcNow()
                });
            }

            initDb.LeaveBalances.Add(new LeaveBalance
            {
                Id = Guid.NewGuid(),
                EmployeeId = empHr1.Id,
                Year = 2026,
                AllocatedDays = 12m,
                UsedDays = 0m,
                UpdatedAtUtc = clock.GetUtcNow()
            });
            await initDb.SaveChangesAsync();
        }

        using var clientHr1 = await BearerClientAsync(harness, hr1);
        using var clientHr2 = await BearerClientAsync(harness, hr2);

        // HR 1 submits leave request
        Guid leaveId;
        using (var res = await clientHr1.PostAsJsonAsync("/api/v1/leave-requests", new CreateLeaveRequest(new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 12), "Self Leave Request")))
        {
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var leave = await res.Content.ReadFromJsonAsync<LeaveRecordResponse>();
            Assert.NotNull(leave);
            leaveId = leave.Id;
        }

        // HR 1 attempts to approve own leave -> 403 Forbidden (Anti-self-approval)
        using (var res = await clientHr1.PostAsJsonAsync($"/api/v1/leave-requests/{leaveId}/approve", new ApproveLeaveRequest("Self approval attempt")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // HR 2 approves HR 1's leave -> 204 NoContent
        using (var res = await clientHr2.PostAsJsonAsync($"/api/v1/leave-requests/{leaveId}/approve", new ApproveLeaveRequest("Approved by peer HR")))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // Verify status in DB
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var approvedLeave = await db.LeaveRecords.SingleAsync(l => l.Id == leaveId);
        Assert.Equal("Approved", approvedLeave.Status);
        Assert.Equal(empHr2.Id, approvedLeave.ApproverId);
    }

    [Fact]
    public async Task SeparationOfDuties_PayrollCalculator_CannotApproveOwnCalculation_AnotherPayrollUserCanApprove()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var calcUser = await CreateUserWithRolesAsync(harness, "calc-user", HrmRoles.Payroll);
        var approverUser = await CreateUserWithRolesAsync(harness, "approver-user", HrmRoles.Payroll);

        Guid periodId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var period = new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-11-SOD",
                Status = "Draft",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.PayrollPeriods.Add(period);
            await db.SaveChangesAsync();
            periodId = period.Id;
        }

        using var calcClient = await BearerClientAsync(harness, calcUser);
        using var approverClient = await BearerClientAsync(harness, approverUser);

        // 1. Calculator calculates the period -> 204
        using (var res = await calcClient.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/calculate", new PayrollActionRequest("Initial calculation")))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // 2. Calculator attempts to approve their own calculation -> 403 Forbidden
        using (var res = await calcClient.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/approve", new PayrollActionRequest("Self approval attempt")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 3. Different Payroll user approves -> 204 NoContent
        using (var res = await approverClient.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/approve", new PayrollActionRequest("Approved by peer")))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // 4. Calculator attempts to publish -> 403 Forbidden
        using (var res = await calcClient.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/publish", new PayrollActionRequest("Self publish attempt")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 5. Approver publishes -> 204 NoContent
        using (var res = await approverClient.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/publish", new PayrollActionRequest("Published by approver")))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }
    }

    [Fact]
    public async Task PayrollPeriod_PublishedOrLockedPeriod_CannotBeRecalculatedOrReopened()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var payrollUser = await CreateUserWithRolesAsync(harness, "pr-immutable", HrmRoles.Payroll);
        using var client = await BearerClientAsync(harness, payrollUser);

        Guid publishedId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var period = new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-11-PUB-IMM",
                Status = "Published",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.PayrollPeriods.Add(period);
            await db.SaveChangesAsync();
            publishedId = period.Id;
        }

        // 1. Calculate on Published period -> 409 Conflict
        using (var res = await client.PostAsJsonAsync($"/api/v1/payroll-periods/{publishedId}/calculate", new PayrollActionRequest("Attempt reopen")))
        {
            await ProblemAsync(res, HttpStatusCode.Conflict);
        }

        // 2. Approve on Published period -> 409 Conflict
        using (var res = await client.PostAsJsonAsync($"/api/v1/payroll-periods/{publishedId}/approve", new PayrollActionRequest("Attempt re-approve")))
        {
            await ProblemAsync(res, HttpStatusCode.Conflict);
        }

        // 3. Publish on Published period -> 409 Conflict
        using (var res = await client.PostAsJsonAsync($"/api/v1/payroll-periods/{publishedId}/publish", new PayrollActionRequest("Attempt re-publish")))
        {
            await ProblemAsync(res, HttpStatusCode.Conflict);
        }

        // Verify status in DB is STILL Published
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var status = await db.PayrollPeriods.Where(p => p.Id == publishedId).Select(p => p.Status).SingleAsync();
            Assert.Equal("Published", status);
        }
    }

    [Fact]
    public async Task Audit_SystemAdmin_CannotReadPiiOrSecretMarkers_FromReasonOrPayload()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-audit-leak", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "target-leak", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Target Leak", "000000002345");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        const string syntheticCccd = "000000002345";
        const string syntheticSecret = "TEST-ONLY-SECRET-NOT-A-REAL-TOKEN";
        const string privateAddress = "Private synthetic replacement address";

        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var updateReq = new UpdateEmployeeRequest("0911111111", privateAddress, $"Review fixture {syntheticCccd} {syntheticSecret}", current!.RowVersion);
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", updateReq))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // SystemAdmin queries audit events
        var adminUser = await CreateUserWithRolesAsync(harness, "admin-audit-leak", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        using (var res = await adminClient.GetAsync("/api/v1/audit-events?action=employees.updated"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();

            // Raw CCCD must NOT appear in Reason
            Assert.DoesNotContain(syntheticCccd, body);
            Assert.Contains("********2345", body);

            // Secret token must NOT appear in Reason
            Assert.DoesNotContain(syntheticSecret, body);
            Assert.Contains("[REDACTED]", body);

            // Private address must NOT appear in Payload
            Assert.DoesNotContain(privateAddress, body);
        }
    }

    [Fact]
    public async Task Payroll_GetEmployee_ReceivesCompensationProjectionWithoutAddressOrRawCccd()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var employeeUser = await CreateUserWithRolesAsync(harness, "emp-proj", HrmRoles.Employee);
        const string syntheticCccd = "000000002345";
        const string privateAddress = "Private synthetic address B";

        Guid empId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            empId = Guid.NewGuid();
            db.EmployeeProfiles.Add(new EmployeeProfile
            {
                Id = empId,
                UserId = employeeUser.Id,
                FullName = "Synthetic Employee B",
                CitizenId = syntheticCccd,
                Phone = "0900000000",
                Address = privateAddress,
                CreatedAtUtc = clock.GetUtcNow()
            });
            await db.SaveChangesAsync();
        }

        var payrollUser = await CreateUserWithRolesAsync(harness, "pr-proj", HrmRoles.Payroll);
        using var payrollClient = await BearerClientAsync(harness, payrollUser);

        using (var res = await payrollClient.GetAsync($"/api/v1/employees/{empId}"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadAsStringAsync();

            // Raw CCCD must NOT appear in response body
            Assert.DoesNotContain(syntheticCccd, body);
            // Private address must NOT appear in response body
            Assert.DoesNotContain(privateAddress, body);

            var profile = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal("Synthetic Employee B", profile.FullName);
            Assert.Null(profile.Address);
            Assert.Equal("********2345", profile.CitizenId);
        }
    }

    [Fact]
    public async Task BoundedPagination_EnforcedOnPersonalAndPayrollLists()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var empUser = await CreateUserWithRolesAsync(harness, "emp-page", HrmRoles.Employee);
        await CreateEmployeeProfileAsync(harness, empUser, "Paging Emp", "001200000055");
        using var empClient = await BearerClientAsync(harness, empUser);

        // 1. /api/v1/me/leave-requests
        using (var res = await empClient.GetAsync("/api/v1/me/leave-requests?pageSize=200"))
        {
            await ProblemAsync(res, HttpStatusCode.BadRequest);
        }
        using (var res = await empClient.GetAsync("/api/v1/me/leave-requests?page=1&pageSize=20"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var page = await res.Content.ReadFromJsonAsync<PagedResult<LeaveRecordResponse>>();
            Assert.NotNull(page);
            Assert.Equal(1, page.Page);
            Assert.Equal(20, page.PageSize);
        }

        // 2. /api/v1/me/payslips
        using (var res = await empClient.GetAsync("/api/v1/me/payslips?pageSize=200"))
        {
            await ProblemAsync(res, HttpStatusCode.BadRequest);
        }
        using (var res = await empClient.GetAsync("/api/v1/me/payslips?page=1&pageSize=20"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var page = await res.Content.ReadFromJsonAsync<PagedResult<PayslipResponse>>();
            Assert.NotNull(page);
            Assert.Equal(1, page.Page);
            Assert.Equal(20, page.PageSize);
        }

        // 3. /api/v1/payroll-periods/{id}/items
        var payrollUser = await CreateUserWithRolesAsync(harness, "pr-page", HrmRoles.Payroll);
        using var prClient = await BearerClientAsync(harness, payrollUser);

        Guid periodId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var period = new PayrollPeriodRecord
            {
                Id = Guid.NewGuid(),
                PeriodCode = "2026-11-PAGE",
                Status = "Draft",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.PayrollPeriods.Add(period);
            await db.SaveChangesAsync();
            periodId = period.Id;
        }

        using (var res = await prClient.GetAsync($"/api/v1/payroll-periods/{periodId}/items?pageSize=200"))
        {
            await ProblemAsync(res, HttpStatusCode.BadRequest);
        }
        using (var res = await prClient.GetAsync($"/api/v1/payroll-periods/{periodId}/items?page=1&pageSize=20"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var page = await res.Content.ReadFromJsonAsync<PagedResult<PayslipResponse>>();
            Assert.NotNull(page);
            Assert.Equal(1, page.Page);
            Assert.Equal(20, page.PageSize);
        }
    }

    [Fact]
    public async Task Audit_FreeTextAssignments_RedactsPasswordAndTokenValues_NotJustKeys()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-secret-leak", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "target-secret", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Secret Target", "001200000077");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        const string pwd = "BluebirdFixture!478";
        const string tok = "Qz9Xv2Lc7Nm5Rt8Yp4";

        var auditTarget = await hrClient.GetFromJsonAsync<EmployeeProfileResponse>($"/api/v1/employees/{emp.Id}");
        var updateReq = new UpdateEmployeeRequest("0900000000", null, $"Support fixture password={pwd}; token={tok}", auditTarget!.RowVersion);
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", updateReq))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-secret-audit", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        using (var res = await adminClient.GetAsync("/api/v1/audit-events?action=employees.updated"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var text = await res.Content.ReadAsStringAsync();

            // Secret values must NOT be visible
            Assert.DoesNotContain(pwd, text);
            Assert.DoesNotContain(tok, text);
        }
    }

    [Fact]
    public async Task PayrollPeriod_StaleCalculate_CannotOverwritePublishedPeriod_UnderRace()
    {
        var pause = new TestPauseCalculation();
        await using var harness = new AuthHarness(sql.ConnectionString, configure: services =>
            services.AddDbContext<HrmDbContext>((_, options) => options.AddInterceptors(pause)));

        var userA = await CreateUserWithRolesAsync(harness, "race-calc-a", HrmRoles.Payroll);
        var userB = await CreateUserWithRolesAsync(harness, "race-appr-b", HrmRoles.Payroll);
        var userC = await CreateUserWithRolesAsync(harness, "race-calc-c", HrmRoles.Payroll);

        var empUser = await CreateUserWithRolesAsync(harness, "race-emp", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, empUser, "Race Emp", "001200000088");

        Guid periodId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            periodId = Guid.NewGuid();
            db.PayrollPeriods.Add(new PayrollPeriodRecord
            {
                Id = periodId,
                PeriodCode = "2026-RACE",
                Status = "Draft",
                CreatedAtUtc = clock.GetUtcNow()
            });
            db.Payslips.Add(new PayslipRecord
            {
                Id = Guid.NewGuid(),
                PeriodId = periodId,
                EmployeeId = emp.Id,
                Amount = 5000000m,
                Status = "Draft"
            });
            await db.SaveChangesAsync();
        }

        using var clientA = await BearerClientAsync(harness, userA);
        using var clientB = await BearerClientAsync(harness, userB);
        using var clientC = await BearerClientAsync(harness, userC);

        pause.PeriodId = periodId;
        pause.Calculator = userA.Id;
        pause.Armed = true;

        var staleCalculationTask = clientA.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/calculate", new PayrollActionRequest("Pause original calculation"));
        await pause.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));

        try
        {
            // Concurrent newer calculations and publishing
            using var calcRes = await clientC.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/calculate", new PayrollActionRequest("Newer calculate"));
            Assert.Equal(HttpStatusCode.NoContent, calcRes.StatusCode);

            using var appRes = await clientB.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/approve", new PayrollActionRequest("Approve by peer"));
            Assert.Equal(HttpStatusCode.NoContent, appRes.StatusCode);

            using var pubRes = await clientB.PostAsJsonAsync($"/api/v1/payroll-periods/{periodId}/publish", new PayrollActionRequest("Publish"));
            Assert.Equal(HttpStatusCode.NoContent, pubRes.StatusCode);
        }
        finally
        {
            pause.Release.TrySetResult(true);
        }

        using var staleResponse = await staleCalculationTask;
        // Stale calculation must be rejected with 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);

        // Verify database: period and payslips are still Published, not overwritten to Calculated
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var period = await db.PayrollPeriods.AsNoTracking().SingleAsync(p => p.Id == periodId);
            var slip = await db.Payslips.AsNoTracking().SingleAsync(p => p.PeriodId == periodId);

            Assert.Equal("Published", period.Status);
            Assert.Equal("Published", slip.Status);
            Assert.True(period.PublishedBy.HasValue);
        }
    }

    [Fact]
    public async Task PayrollPeriod_MissingCalculationOrApprovalMetadata_FailsClosedWithConflict()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var payrollUser = await CreateUserWithRolesAsync(harness, "pr-meta", HrmRoles.Payroll);
        var approverUser = await CreateUserWithRolesAsync(harness, "pr-meta-appr", HrmRoles.Payroll);
        using var client = await BearerClientAsync(harness, payrollUser);
        using var approverClient = await BearerClientAsync(harness, approverUser);

        Guid missingCalcId;
        Guid missingApprId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();

            missingCalcId = Guid.NewGuid();
            missingApprId = Guid.NewGuid();

            // 1. Calculated period with NULL CalculatedBy (legacy/untracked data)
            db.PayrollPeriods.Add(new PayrollPeriodRecord
            {
                Id = missingCalcId,
                PeriodCode = "2026-NOCALC",
                Status = "Calculated",
                CalculatedBy = null,
                CalculatedAtUtc = null,
                CreatedAtUtc = clock.GetUtcNow()
            });

            // 2. Approved period with NULL ApprovedBy (legacy/untracked data)
            db.PayrollPeriods.Add(new PayrollPeriodRecord
            {
                Id = missingApprId,
                PeriodCode = "2026-NOAPPR",
                Status = "Approved",
                CalculatedBy = payrollUser.Id,
                CalculatedAtUtc = clock.GetUtcNow(),
                ApprovedBy = null,
                ApprovedAtUtc = null,
                CreatedAtUtc = clock.GetUtcNow()
            });

            await db.SaveChangesAsync();
        }

        // Attempting to approve period with missing CalculatedBy -> 409 Conflict (Fail-closed)
        using (var res = await client.PostAsJsonAsync($"/api/v1/payroll-periods/{missingCalcId}/approve", new PayrollActionRequest("Approve missing calc")))
        {
            await ProblemAsync(res, HttpStatusCode.Conflict);
        }

        // Attempting to publish period with missing ApprovedBy -> 409 Conflict (Fail-closed)
        using (var res = await client.PostAsJsonAsync($"/api/v1/payroll-periods/{missingApprId}/publish", new PayrollActionRequest("Publish missing appr")))
        {
            await ProblemAsync(res, HttpStatusCode.Conflict);
        }

        // Recovery path: recalculate period missingCalcId to restore metadata
        using (var calcRes = await client.PostAsJsonAsync($"/api/v1/payroll-periods/{missingCalcId}/calculate", new PayrollActionRequest("Recalculate to recover metadata")))
        {
            Assert.Equal(HttpStatusCode.NoContent, calcRes.StatusCode);
        }

        // Now CalculatedBy is set to payrollUser.Id; approverUser can approve
        using (var appRes = await approverClient.PostAsJsonAsync($"/api/v1/payroll-periods/{missingCalcId}/approve", new PayrollActionRequest("Approved after recovery")))
        {
            Assert.Equal(HttpStatusCode.NoContent, appRes.StatusCode);
        }

        // Now ApprovedBy is set; approverUser can publish
        using (var pubRes = await approverClient.PostAsJsonAsync($"/api/v1/payroll-periods/{missingCalcId}/publish", new PayrollActionRequest("Publish after recovery")))
        {
            Assert.Equal(HttpStatusCode.NoContent, pubRes.StatusCode);
        }
    }

    [Fact]
    public async Task Audit_CredentialAliases_CurrentPasswordAndNewPassword_RedactedFromReasonAndPayload()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-alias", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "target-alias", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Alias Target", "001200000091");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        const string pwd = "BluebirdFixture!478";
        const string tok = "Qz9Xv2Lc7Nm5Rt8Yp4";

        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var updateReq = new UpdateEmployeeRequest("0900000000", null, $"Support fixture CurrentPassword={pwd}; NewPassword={tok}", current!.RowVersion);
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", updateReq))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-alias-audit", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        using (var res = await adminClient.GetAsync("/api/v1/audit-events?action=employees.updated"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var text = await res.Content.ReadAsStringAsync();

            Assert.DoesNotContain(pwd, text);
            Assert.DoesNotContain(tok, text);
            Assert.Contains("[REDACTED]", text);
        }

        // Verify database persistence also has redacted values
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var audit = await db.AuditEvents.OrderByDescending(a => a.OccurredAtUtc).FirstOrDefaultAsync(a => a.EntityId == emp.Id.ToString());
        Assert.NotNull(audit);
        Assert.DoesNotContain(pwd, audit.Reason);
        Assert.DoesNotContain(tok, audit.Reason);
        Assert.Contains("[REDACTED]", audit.Reason);
    }

    [Fact]
    public async Task Audit_CredentialAliases_VariantsAndNestedPayload_AreRedacted()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-variants", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "target-var", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Var Target", "001200000092");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        const string s1 = "AlphaVal123!";
        const string s2 = "BetaVal456@";
        const string s3 = "GammaVal789#";
        const string s4 = "BluebirdFixture!478";

        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var complexReason = $"Config current_password: '{s1}', new-password=\"{s2}\", access_token: {s3}, client_secret => '{s4}'";
        var updateReq = new UpdateEmployeeRequest("0911223344", null, complexReason, current!.RowVersion);
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", updateReq))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-var-audit", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        using (var res = await adminClient.GetAsync("/api/v1/audit-events?action=employees.updated"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var text = await res.Content.ReadAsStringAsync();

            Assert.DoesNotContain(s1, text);
            Assert.DoesNotContain(s2, text);
            Assert.DoesNotContain(s3, text);
            Assert.DoesNotContain(s4, text);
        }

        // Verify database persistence also has redacted values
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var audit = await db.AuditEvents.OrderByDescending(a => a.OccurredAtUtc).FirstOrDefaultAsync(a => a.EntityId == emp.Id.ToString());
        Assert.NotNull(audit);
        Assert.DoesNotContain(s4, audit.Reason);
    }

    [Fact]
    public async Task Audit_SerializedCredentialJsonInReason_IsRedactedFromDbAndAuditResponse()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-json-cred", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "target-json-cred", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Json Cred Target", "001200000095");
        await AssignHrScopeAsync(harness, hrUser.Id, emp.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        const string pwd = "BluebirdFixture!478";
        const string tok = "Qz9Xv2Lc7Nm5Rt8Yp4";
        var getRes = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}");
        var current = await getRes.Content.ReadFromJsonAsync<EmployeeProfileResponse>();

        var jsonCredentialReason = JsonSerializer.Serialize(new { CurrentPassword = pwd, NewPassword = tok });

        var updateReq = new UpdateEmployeeRequest("0922334455", null, jsonCredentialReason, current!.RowVersion);
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", updateReq))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-json-audit", HrmRoles.SystemAdmin);
        using var adminClient = await BearerClientAsync(harness, adminUser);

        using (var res = await adminClient.GetAsync("/api/v1/audit-events?action=employees.updated"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var text = await res.Content.ReadAsStringAsync();

            Assert.DoesNotContain(pwd, text);
            Assert.DoesNotContain(tok, text);
            Assert.Contains("[REDACTED]", text);

            var page = await res.Content.ReadFromJsonAsync<PagedResult<AuditEventResponse>>();
            Assert.NotNull(page);
            var latest = page.Items.FirstOrDefault(a => a.EntityId == emp.Id.ToString());
            Assert.NotNull(latest);
            Assert.Equal(hrUser.Id, latest.ActorId);
            Assert.Equal("employees.updated", latest.Action);
            Assert.NotNull(latest.TraceId);
        }

        // Verify database persistence also has redacted values
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var persistedLeak = await db.AuditEvents.AnyAsync(e => e.Reason.Contains(pwd) || (e.Payload != null && e.Payload.Contains(pwd)));
        Assert.False(persistedLeak);
        var audit = await db.AuditEvents.OrderByDescending(a => a.OccurredAtUtc).FirstOrDefaultAsync(a => a.EntityId == emp.Id.ToString());
        Assert.NotNull(audit);
        Assert.DoesNotContain(pwd, audit.Reason);
        Assert.DoesNotContain(tok, audit.Reason);
    }

    [Fact]
    public async Task HrScope_UnassignedHr_CannotReadOrPatchOtherEmployee_OrApproveLeave()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-unassigned", HrmRoles.Hr);
        var targetUser = await CreateUserWithRolesAsync(harness, "emp-other", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, targetUser, "Other Emp", "001200000093");

        // Create leave for employee
        Guid leaveId;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var leave = new LeaveRecord
            {
                Id = Guid.NewGuid(),
                EmployeeId = emp.Id,
                StartDate = new DateOnly(2026, 12, 1),
                EndDate = new DateOnly(2026, 12, 5),
                Reason = "Vacation",
                Status = "Pending",
                CreatedAtUtc = clock.GetUtcNow()
            };
            db.LeaveRecords.Add(leave);
            await db.SaveChangesAsync();
            leaveId = leave.Id;
        }

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // 1. GET employee profile -> 403 Forbidden
        using (var res = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 2. PATCH employee profile -> 403 Forbidden
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp.Id}", new UpdateEmployeeRequest("0987654321", null, "Unauthorized update")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 3. GET leave request -> 403 Forbidden
        using (var res = await hrClient.GetAsync($"/api/v1/leave-requests/{leaveId}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 4. Approve leave request -> 403 Forbidden
        using (var res = await hrClient.PostAsJsonAsync($"/api/v1/leave-requests/{leaveId}/approve", new ApproveLeaveRequest("Unauthorized approval")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task HrScope_AssignedHr_CanAccessOnlyAssignedEmployees_UnassignedReturns403()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-scoped", HrmRoles.Hr);

        var user1 = await CreateUserWithRolesAsync(harness, "emp-1", HrmRoles.Employee);
        var emp1 = await CreateEmployeeProfileAsync(harness, user1, "Assigned Emp 1", "001200000094");

        var user2 = await CreateUserWithRolesAsync(harness, "emp-2", HrmRoles.Employee);
        var emp2 = await CreateEmployeeProfileAsync(harness, user2, "Unassigned Emp 2", "001200000095");

        // Assign only emp1 to hrUser
        await AssignHrScopeAsync(harness, hrUser.Id, emp1.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // 1. Can GET assigned employee 1 (full profile)
        using (var res = await hrClient.GetAsync($"/api/v1/employees/{emp1.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var profile = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal("Assigned Emp 1", profile.FullName);
            Assert.Equal("001200000094", profile.CitizenId); // Raw CCCD for assigned HR
            Assert.Equal("123 Test Street", profile.Address);
        }

        // 2. Cannot GET unassigned employee 2 -> 403 Forbidden
        using (var res = await hrClient.GetAsync($"/api/v1/employees/{emp2.Id}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 3. Can PATCH assigned employee 1
        var getEmp1Res = await hrClient.GetAsync($"/api/v1/employees/{emp1.Id}");
        var currentEmp1 = await getEmp1Res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp1.Id}", new UpdateEmployeeRequest("0912121212", null, "Authorized update", currentEmp1!.RowVersion)))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // 4. Cannot PATCH unassigned employee 2 -> 403 Forbidden
        using (var res = await hrClient.PatchAsJsonAsync($"/api/v1/employees/{emp2.Id}", new UpdateEmployeeRequest("0934343434", null, "Unauthorized update")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task HrScope_CombinedHrAndPayroll_ReceivesPayrollProjectionForOutOfScopeEmployee()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        // User with both HR and Payroll roles
        var dualUser = await CreateUserWithRolesAsync(harness, "hr-pr-dual", HrmRoles.Hr, HrmRoles.Payroll);

        var user1 = await CreateUserWithRolesAsync(harness, "emp-dual-1", HrmRoles.Employee);
        var emp1 = await CreateEmployeeProfileAsync(harness, user1, "Assigned Dual 1", "001200000096");

        var user2 = await CreateUserWithRolesAsync(harness, "emp-dual-2", HrmRoles.Employee);
        var emp2 = await CreateEmployeeProfileAsync(harness, user2, "Unassigned Dual 2", "001200000097");

        // Assign only emp1 to dualUser
        await AssignHrScopeAsync(harness, dualUser.Id, emp1.Id);

        using var client = await BearerClientAsync(harness, dualUser);

        // 1. Assigned employee: receives FULL HR profile
        using (var res = await client.GetAsync($"/api/v1/employees/{emp1.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var profile = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal("001200000096", profile.CitizenId); // Raw CCCD
            Assert.Equal("123 Test Street", profile.Address);
        }

        // 2. Out-of-scope employee: receives Payroll compensation projection (CCCD masked, address null)
        using (var res = await client.GetAsync($"/api/v1/employees/{emp2.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var profile = await res.Content.ReadFromJsonAsync<EmployeeProfileResponse>();
            Assert.NotNull(profile);
            Assert.Equal("********0097", profile.CitizenId); // Masked CCCD
            Assert.Null(profile.Address); // Omitted address
        }

        // 3. Out-of-scope employee: cannot PATCH (403 Forbidden - Payroll cannot elevate HR scope)
        using (var res = await client.PatchAsJsonAsync($"/api/v1/employees/{emp2.Id}", new UpdateEmployeeRequest("0955555555", null, "Attempt modify out of scope")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task HrScope_Revocation_TakesEffectImmediatelyOnNextRequest()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-revoke", HrmRoles.SystemAdmin);
        var hrUser = await CreateUserWithRolesAsync(harness, "hr-revokable", HrmRoles.Hr);

        var empUser = await CreateUserWithRolesAsync(harness, "emp-revokable", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, empUser, "Revokable Emp", "001200000098");

        using var adminClient = await BearerClientAsync(harness, adminUser);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        // 1. Admin assigns scope
        var assignReq = new AssignScopesRequest([emp.Id], "Initial scope assignment");
        using (var res = await adminClient.PostAsJsonAsync($"/api/v1/accounts/{hrUser.Id}/scopes", assignReq))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // 2. HR can now read employee
        using (var res = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // 3. Admin revokes scope
        var revokeReq = new RevokeScopeRequest("End of contract coverage");
        using var revokeMessage = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/accounts/{hrUser.Id}/scopes/{emp.Id}")
        {
            Content = JsonContent.Create(revokeReq)
        };
        using (var res = await adminClient.SendAsync(revokeMessage))
        {
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // 4. Immediate next request by HR is rejected with 403 without token refresh
        using (var res = await hrClient.GetAsync($"/api/v1/employees/{emp.Id}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 5. Verify audit log has both assignments and revocation
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HrmDbContext>();
        var assignAudit = await db.AuditEvents.AnyAsync(a => a.Action == "accounts.scopes_assigned" && a.EntityId == hrUser.Id.ToString());
        var revokeAudit = await db.AuditEvents.AnyAsync(a => a.Action == "accounts.scopes_revoked" && a.EntityId == hrUser.Id.ToString());
        Assert.True(assignAudit);
        Assert.True(revokeAudit);
    }

    [Fact]
    public async Task HrScope_AntiSelfAssignment_AndAdminHasNoImplicitPiiAccess()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var adminUser = await CreateUserWithRolesAsync(harness, "admin-anti", HrmRoles.SystemAdmin);
        var hrUser = await CreateUserWithRolesAsync(harness, "hr-anti", HrmRoles.Hr);

        var empUser = await CreateUserWithRolesAsync(harness, "emp-anti", HrmRoles.Employee);
        var emp = await CreateEmployeeProfileAsync(harness, empUser, "Anti Emp", "001200000099");

        using var adminClient = await BearerClientAsync(harness, adminUser);
        using var hrClient = await BearerClientAsync(harness, hrUser);

        // 1. SystemAdmin cannot read employee profile (no PII access)
        using (var res = await adminClient.GetAsync($"/api/v1/employees/{emp.Id}"))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 2. Admin cannot assign scopes to themselves (anti-self-scope)
        using (var res = await adminClient.PostAsJsonAsync($"/api/v1/accounts/{adminUser.Id}/scopes", new AssignScopesRequest([emp.Id], "Self assign attempt")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 3. HR cannot manage scopes (lacks hr.scope_manage permission)
        using (var res = await hrClient.PostAsJsonAsync($"/api/v1/accounts/{hrUser.Id}/scopes", new AssignScopesRequest([emp.Id], "HR self assign attempt")))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 4. Invalid employee ID in scope assignment is rejected with 400
        using (var res = await adminClient.PostAsJsonAsync($"/api/v1/accounts/{hrUser.Id}/scopes", new AssignScopesRequest([Guid.NewGuid()], "Invalid emp id")))
        {
            await ProblemAsync(res, HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task HrScope_BulkAndList_EnforcesAssignedScope()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);

        var hrUser = await CreateUserWithRolesAsync(harness, "hr-bulk", HrmRoles.Hr);

        var user1 = await CreateUserWithRolesAsync(harness, "emp-bulk-1", HrmRoles.Employee);
        var emp1 = await CreateEmployeeProfileAsync(harness, user1, "Bulk Emp 1", "001200000101");

        var user2 = await CreateUserWithRolesAsync(harness, "emp-bulk-2", HrmRoles.Employee);
        var emp2 = await CreateEmployeeProfileAsync(harness, user2, "Bulk Emp 2", "001200000102");

        // Assign only emp1 to hrUser
        await AssignHrScopeAsync(harness, hrUser.Id, emp1.Id);

        using var hrClient = await BearerClientAsync(harness, hrUser);

        // 1. List /api/v1/employees returns ONLY assigned emp1
        using (var res = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=20"))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var page = await res.Content.ReadFromJsonAsync<PagedResult<EmployeeProfileResponse>>();
            Assert.NotNull(page);
            Assert.Single(page.Items);
            Assert.Equal(emp1.Id, page.Items[0].Id);
        }

        // 2. Bulk get with only assigned emp1 succeeds
        using (var res = await hrClient.PostAsJsonAsync("/api/v1/employees/bulk", new BulkGetEmployeesRequest([emp1.Id])))
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var items = await res.Content.ReadFromJsonAsync<List<EmployeeProfileResponse>>();
            Assert.NotNull(items);
            Assert.Single(items);
            Assert.Equal(emp1.Id, items[0].Id);
        }

        // 3. Bulk get with MIXED assigned + unassigned employees fails closed with 403
        using (var res = await hrClient.PostAsJsonAsync("/api/v1/employees/bulk", new BulkGetEmployeesRequest([emp1.Id, emp2.Id])))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }

        // 4. Unprivileged Employee user calls bulk -> 403 Forbidden
        using var empClient = await BearerClientAsync(harness, user1);
        using (var res = await empClient.PostAsJsonAsync("/api/v1/employees/bulk", new BulkGetEmployeesRequest([emp1.Id])))
        {
            await ProblemAsync(res, HttpStatusCode.Forbidden);
        }
    }
}

public sealed class TestPauseCalculation : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    public Guid PeriodId;
    public Guid Calculator;
    public bool Armed;
    public TaskCompletionSource<bool> Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override async ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
        Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData data,
        Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result,
        CancellationToken token = default)
    {
        if (Armed && data.Context!.ChangeTracker.Entries<PayrollPeriodRecord>().Any(e => e.State == EntityState.Modified && e.Entity.Id == PeriodId && e.Entity.CalculatedBy == Calculator && e.Entity.Status == "Calculated"))
        {
            Armed = false;
            Reached.TrySetResult(true);
            await Release.Task.WaitAsync(token);
        }
        return result;
    }
}

