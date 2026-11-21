using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HRM.Next.Api.Auth;
using HRM.Next.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using HRM.Next.Infrastructure.Persistence;
using HRM.Next.Domain.Departments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests
{
    private const string AuthPassword = "Test-only!Passphrase2026";
    private const string NewAuthPassword = "Test-only!Replacement2026";

    private static async Task<HrmUser> AuthUserAsync(AuthHarness harness, bool active = true)
    {
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        var suffix = Guid.NewGuid().ToString("N");
        var user = new HrmUser { Id = Guid.NewGuid(), UserName = "test-" + suffix, Email = suffix + "@example.invalid", IsActive = active };
        Assert.True((await manager.CreateAsync(user, AuthPassword)).Succeeded);
        return user;
    }

    private static async Task<TokenPair> MobileAsync(HttpClient client, HrmUser user)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, AuthPassword));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pair = await response.Content.ReadFromJsonAsync<TokenPair>();
        Assert.NotNull(pair);
        Assert.Equal("Bearer", pair.TokenType);
        Assert.Equal(900, pair.ExpiresIn);
        Assert.DoesNotContain(".", pair.AccessToken); // Opaque framework token, not JWT.
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        return pair;
    }

    private static void UseBearer(HttpClient client, string token) => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    private static async Task CsrfAsync(HttpClient client)
    {
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        Assert.NotNull(csrf);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", csrf.RequestToken);
    }

    private static async Task<string> WebAsync(HttpClient client, HrmUser user)
    {
        await CsrfAsync(client);
        using var response = await client.PostAsJsonAsync("/api/v1/auth/web/login", new LoginRequest(user.UserName!, AuthPassword));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), x => x.StartsWith(SessionAuthenticationHandler.CookieName + "="));
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        await CsrfAsync(client); // Tokens are identity-bound; renew after sign-in.
        return cookie.Split(';')[0];
    }

    private static async Task<string> ResetCodeAsync(AuthHarness harness, HrmUser user)
    {
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        return await manager.GeneratePasswordResetTokenAsync((await manager.FindByIdAsync(user.Id.ToString()))!);
    }

    [Fact]
    public async Task ApprovedAuthLifetimesAndSevenDayRefreshExpiry_AreEnforced()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        var pair = await MobileAsync(client, user);
        Assert.Equal(TimeSpan.FromMinutes(15), harness.Factory.Services.GetRequiredService<IOptions<DataProtectionTokenProviderOptions>>().Value.TokenLifespan);
        harness.Clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromSeconds(1));
        using var response = await client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(pair.RefreshToken));
        await ProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invitation_RequiresActivationAndNormalizedUsernameIsUnique()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        Guid id;
        var name = "invite-" + Guid.NewGuid().ToString("N");
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
            id = await accounts.InviteAsync(name, name + "@example.invalid", CancellationToken.None);
            await Assert.ThrowsAsync<InvalidOperationException>(() => accounts.InviteAsync(name.ToUpperInvariant(), "different-" + name + "@example.invalid", CancellationToken.None));
            var user = await scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>().FindByIdAsync(id.ToString());
            Assert.NotNull(user); Assert.Null(user.PasswordHash); Assert.False(user.IsActive);
        }
        var message = Assert.Single(harness.Sender.Messages);
        Assert.Equal("activation", message.Purpose);
        using var client = harness.Client();
        using var inactive = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(name, AuthPassword));
        await ProblemAsync(inactive, HttpStatusCode.Unauthorized);
        using var wrongPurpose = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(id, message.Token, AuthPassword));
        await ProblemAsync(wrongPurpose, HttpStatusCode.BadRequest);
        using var activated = await client.PostAsJsonAsync("/api/v1/auth/activate", new ResetRequest(id, message.Token, AuthPassword));
        Assert.Equal(HttpStatusCode.NoContent, activated.StatusCode);
        using var replay = await client.PostAsJsonAsync("/api/v1/auth/activate", new ResetRequest(id, message.Token, NewAuthPassword));
        await ProblemAsync(replay, HttpStatusCode.BadRequest);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(name.ToUpperInvariant(), AuthPassword));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task WebCookie_CsrfAndLogout_AreEnforced()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        using var loginWithout = await client.PostAsJsonAsync("/api/v1/auth/web/login", new LoginRequest(user.UserName!, AuthPassword));
        await ProblemAsync(loginWithout, HttpStatusCode.BadRequest);
        var savedCookie = await WebAsync(client, user);
        var me = await client.GetFromJsonAsync<CurrentAccount>("/api/v1/auth/me");
        Assert.Equal(user.Id, me?.Id);
        client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        using var without = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        await ProblemAsync(without, HttpStatusCode.BadRequest);
        client.DefaultRequestHeaders.Add("X-XSRF-TOKEN", "invalid");
        using var invalid = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        await ProblemAsync(invalid, HttpStatusCode.BadRequest);
        await CsrfAsync(client);
        using var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var after = await client.GetAsync("/api/v1/auth/me");
        await ProblemAsync(after, HttpStatusCode.Unauthorized);
        using var replay = harness.Client();
        replay.DefaultRequestHeaders.Add("Cookie", savedCookie);
        using var reused = await replay.GetAsync("/api/v1/auth/me");
        await ProblemAsync(reused, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("reset")]
    [InlineData("disable")]
    [InlineData("revoke-all")]
    [InlineData("stamp-only")]
    [InlineData("inactive-only")]
    public async Task CommittedRevocation_RejectsExistingCookieBearerAndRefresh(string action)
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var mobile = harness.Client();
        using var web = harness.Client();
        var pair = await MobileAsync(mobile, user);
        UseBearer(mobile, pair.AccessToken);
        await WebAsync(web, user);
        using (var before = await mobile.GetAsync("/api/v1/auth/me")) Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        if (action == "password")
        {
            using var changed = await mobile.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequest(AuthPassword, NewAuthPassword));
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        }
        else if (action == "reset")
        {
            using var reset = await mobile.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(user.Id, await ResetCodeAsync(harness, user), NewAuthPassword));
            Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        }
        else if (action == "revoke-all")
        {
            using var revoked = await mobile.PostAsJsonAsync("/api/v1/auth/logout-all", new { });
            Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        }
        else
        {
            await using var scope = harness.Factory.Services.CreateAsyncScope();
            if (action == "disable") Assert.True(await scope.ServiceProvider.GetRequiredService<AccountService>().RevokeAllAsync(user.Id, true, CancellationToken.None));
            else
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
                var account = (await manager.FindByIdAsync(user.Id.ToString()))!;
                if (action == "stamp-only") Assert.True((await manager.UpdateSecurityStampAsync(account)).Succeeded);
                else { account.IsActive = false; Assert.True((await manager.UpdateAsync(account)).Succeeded); }
            }
        }
        using var bearer = await mobile.GetAsync("/api/v1/auth/me");
        using var cookie = await web.GetAsync("/api/v1/auth/me");
        using var refresh = await mobile.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(pair.RefreshToken));
        await ProblemAsync(bearer, HttpStatusCode.Unauthorized);
        await ProblemAsync(cookie, HttpStatusCode.Unauthorized);
        await ProblemAsync(refresh, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RotatesAtomicallyAndRejectsReplayAndWrongPurpose()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        var original = await MobileAsync(client, user);
        using var wrongPurpose = await client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(original.AccessToken));
        await ProblemAsync(wrongPurpose, HttpStatusCode.Unauthorized);
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(original.RefreshToken))));
        try
        {
            Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.OK);
            Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.Unauthorized);
            var fresh = await concurrent.Single(r => r.IsSuccessStatusCode).Content.ReadFromJsonAsync<TokenPair>();
            Assert.NotNull(fresh);
            Assert.NotEqual(original.RefreshToken, fresh.RefreshToken);
            UseBearer(client, fresh.AccessToken);
            using var me = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            using var replay = await client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(original.RefreshToken));
            await ProblemAsync(replay, HttpStatusCode.Unauthorized);
            using var stillValid = await client.GetAsync("/api/v1/auth/me");
            Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
            UseBearer(client, fresh.RefreshToken);
            using var invalidAccess = await client.GetAsync("/api/v1/auth/me");
            await ProblemAsync(invalidAccess, HttpStatusCode.Unauthorized);
        }
        finally { foreach (var response in concurrent) response.Dispose(); }
    }

    [Fact]
    public async Task Expiry_AccessCookieAndRefreshHaveIndependentLifetimes()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var mobile = harness.Client();
        using var web = harness.Client();
        var pair = await MobileAsync(mobile, user);
        await WebAsync(web, user);
        UseBearer(mobile, pair.AccessToken);
        harness.Clock.Advance(TimeSpan.FromMinutes(16));
        using var expired = await mobile.GetAsync("/api/v1/auth/me");
        await ProblemAsync(expired, HttpStatusCode.Unauthorized);
        using var refreshed = await mobile.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(pair.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        harness.Clock.Advance(TimeSpan.FromHours(8));
        using var cookie = await web.GetAsync("/api/v1/auth/me");
        await ProblemAsync(cookie, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResetTokens_RejectEmailPurposeWrongAccountUsedAndConcurrentReplay()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        var other = await AuthUserAsync(harness);
        using var client = harness.Client();
        string emailCode;
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
            emailCode = await manager.GenerateEmailConfirmationTokenAsync((await manager.FindByIdAsync(user.Id.ToString()))!);
        }
        using var wrongPurpose = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(user.Id, emailCode, NewAuthPassword));
        await ProblemAsync(wrongPurpose, HttpStatusCode.BadRequest);
        using var unchanged = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, AuthPassword));
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
        var resetCode = await ResetCodeAsync(harness, user);
        using var wrongAccount = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(other.Id, resetCode, NewAuthPassword));
        await ProblemAsync(wrongAccount, HttpStatusCode.BadRequest);
        using var otherUnchanged = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(other.UserName!, AuthPassword));
        Assert.Equal(HttpStatusCode.OK, otherUnchanged.StatusCode);
        var concurrent = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(user.Id, resetCode, NewAuthPassword))));
        try
        {
            Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.NoContent);
            Assert.Single(concurrent, r => r.StatusCode == HttpStatusCode.BadRequest);
        }
        finally { foreach (var response in concurrent) response.Dispose(); }
        using var replay = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(user.Id, resetCode, AuthPassword));
        await ProblemAsync(replay, HttpStatusCode.BadRequest);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, NewAuthPassword));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var logs = string.Join('\n', harness.Logs.Messages);
        Assert.DoesNotContain(AuthPassword, logs); Assert.DoesNotContain(NewAuthPassword, logs); Assert.DoesNotContain(resetCode, logs);
    }

    [Fact]
    public async Task ExpiredResetToken_IsRejected()
    {
        await using var harness = new AuthHarness(sql.ConnectionString, configure: services =>
            services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromTicks(-1)));
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetRequest(user.Id, await ResetCodeAsync(harness, user), NewAuthPassword));
        await ProblemAsync(response, HttpStatusCode.BadRequest);
        await MobileAsync(client, user);
    }

    [Fact]
    public async Task ForgotPassword_IsHonestAboutMissingEmailAndDoesNotEnumerate()
    {
        await using var harness = new AuthHarness(sql.ConnectionString, testSender: false);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        using var known = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotRequest(user.Email!));
        using var unknown = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotRequest("missing@example.invalid"));
        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode); Assert.Equal(known.StatusCode, unknown.StatusCode);
        var result = await known.Content.ReadFromJsonAsync<DeliveryResponse>();
        Assert.Equal("not-configured", result?.DeliveryMode);
        Assert.Equal(await known.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task TestSender_IsExplicitlyLabelledAndNeverReturnsResetTokenInResponse()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotRequest(user.Email!));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var message = Assert.Single(harness.Sender.Messages);
        Assert.Equal("reset-password", message.Purpose);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("test-only", text); Assert.DoesNotContain(message.Token, text);
    }

    [Fact]
    public async Task LockoutAndIpLimit_BlockPasswordAbuseWithoutAccountEnumeration()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        for (var i = 0; i < 5; i++)
        {
            using var invalid = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, "Incorrect!Password2026"));
            var problem = await ProblemAsync(invalid, HttpStatusCode.Unauthorized);
            Assert.Equal("auth.invalid", problem.GetProperty("errorCode").GetString());
        }
        using var locked = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, AuthPassword));
        await ProblemAsync(locked, HttpStatusCode.Unauthorized);
        for (var i = 6; i < 20; i++)
        {
            using var unknown = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest("missing", "Incorrect!Password2026"));
            await ProblemAsync(unknown, HttpStatusCode.Unauthorized);
        }
        using var limited = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest("missing", AuthPassword));
        await ProblemAsync(limited, HttpStatusCode.TooManyRequests);
        Assert.NotNull(limited.Headers.RetryAfter);
    }

    [Fact]
    public async Task LogoutCurrentSession_RejectsItsTokensAndPreservesOtherSessions()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var first = harness.Client();
        using var second = harness.Client();
        var a = await MobileAsync(first, user); var b = await MobileAsync(second, user);
        UseBearer(first, a.AccessToken); UseBearer(second, b.AccessToken);
        using var logout = await first.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using var old = await first.GetAsync("/api/v1/auth/me"); await ProblemAsync(old, HttpStatusCode.Unauthorized);
        using var oldRefresh = await first.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(a.RefreshToken));
        await ProblemAsync(oldRefresh, HttpStatusCode.Unauthorized);
        using var other = await second.GetAsync("/api/v1/auth/me"); Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    [Fact]
    public async Task PublicRegistrationCannotCreateRolesAndPasswordsHaveNoDefaultBypass()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        using var client = harness.Client();
        using var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { username = "attacker", password = AuthPassword, role = "SystemAdmin" });
        Assert.False(register.IsSuccessStatusCode);
        var user = await AuthUserAsync(harness, active: false);
        using var inactive = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, AuthPassword));
        await ProblemAsync(inactive, HttpStatusCode.Unauthorized);
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        Assert.Null(await manager.FindByNameAsync("attacker"));
        var duplicate = new HrmUser { Id = Guid.NewGuid(), UserName = user.UserName!.ToUpperInvariant(), Email = Guid.NewGuid() + "@example.invalid" };
        Assert.False((await manager.CreateAsync(duplicate, "123456")).Succeeded);
    }

    [Fact]
    public async Task AuthOverPlainHttp_DoesNotIssueCredentials()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        client.BaseAddress = new Uri("http://localhost");
        using var response = await client.PostAsJsonAsync("/api/v1/auth/mobile/login", new LoginRequest(user.UserName!, AuthPassword));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("accessToken", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RefreshHasAbsoluteExpiryAndAuthenticatedAccountHasNoImplicitBusinessPermission()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        var pair = await MobileAsync(client, user);
        UseBearer(client, pair.AccessToken);
        using var denied = await client.GetAsync("/api/v1/departments");
        await ProblemAsync(denied, HttpStatusCode.Forbidden);
        harness.Clock.Advance(TimeSpan.FromDays(6));
        using var renewed = await client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(pair.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        var next = await renewed.Content.ReadFromJsonAsync<TokenPair>();
        Assert.NotNull(next);
        harness.Clock.Advance(TimeSpan.FromDays(2));
        using var expired = await client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(next.RefreshToken));
        await ProblemAsync(expired, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IdentityMigration_UpgradeAndDownPreserveFoundationData()
    {
        await using var db = sql.CreateContext();
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261111094103_InitialFoundation");
            var department = Department.Create("T05 migration preservation", DateTimeOffset.UtcNow);
            db.Departments.Add(department);
            await db.SaveChangesAsync();
            await migrator.MigrateAsync();
            Assert.Equal("T05 migration preservation", await db.Departments.Where(d => d.Id == department.Id).Select(d => d.Name).SingleAsync());
            await migrator.MigrateAsync("20261111094103_InitialFoundation");
            Assert.True(await db.Departments.AnyAsync(d => d.Id == department.Id));
        }
        finally { await migrator.MigrateAsync(); }
    }

    [Fact]
    public async Task ConcurrentRefreshAndDisable_NeverLeaveUsableTokensAfterCommit()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        var pair = await MobileAsync(client, user);
        var refreshing = client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(pair.RefreshToken));
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
            Assert.True(await scope.ServiceProvider.GetRequiredService<AccountService>().RevokeAllAsync(user.Id, true, CancellationToken.None));
        using var result = await refreshing;
        Assert.Contains(result.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        if (result.IsSuccessStatusCode)
        {
            var issued = await result.Content.ReadFromJsonAsync<TokenPair>();
            Assert.NotNull(issued);
            UseBearer(client, issued.AccessToken);
            using var me = await client.GetAsync("/api/v1/auth/me");
            await ProblemAsync(me, HttpStatusCode.Unauthorized);
            using var next = await client.PostAsJsonAsync("/api/v1/auth/mobile/refresh", new RefreshRequest(issued.RefreshToken));
            await ProblemAsync(next, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task OffboardingPort_ParticipatesInCallerTransactionAndRollsBackWithIt()
    {
        await using var harness = new AuthHarness(sql.ConnectionString);
        var user = await AuthUserAsync(harness);
        using var client = harness.Client();
        var pair = await MobileAsync(client, user);
        await using (var scope = harness.Factory.Services.CreateAsyncScope())
        {
            var unit = scope.ServiceProvider.GetRequiredService<HRM.Next.Application.Common.IUnitOfWork>();
            var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => unit.ExecuteAsync<bool>(async token =>
            {
                Assert.True(await accounts.RevokeAllAsync(user.Id, true, token));
                throw new InvalidOperationException("Injected personnel workflow failure");
            }, CancellationToken.None));
        }
        UseBearer(client, pair.AccessToken);
        using var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }
}
