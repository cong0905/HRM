using System.Security.Claims;
using System.Security.Cryptography;
using HRM.Next.Infrastructure.Notifications;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Identity;

public static class HrmClaims
{
    public const string SessionId = "hrm.session";
    public const string RefreshVersion = "hrm.refresh-version";
    public const string ActivationProvider = "HrmActivation";
    public const string ActivationPurpose = "HrmActivateAccount";
}

public sealed record SessionGrant(ClaimsPrincipal Principal, Guid SessionId, Guid RefreshVersion, DateTimeOffset ExpiresUtc);

// A random dummy password hash equalizes the missing-account password verification path.
public sealed class MissingAccountPassword
{
    private readonly IPasswordHasher<HrmUser> _hasher = new PasswordHasher<HrmUser>();
    private readonly HrmUser _user = new();
    private readonly string _hash;
    public MissingAccountPassword() => _hash = _hasher.HashPassword(_user, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
    public void Verify(string password) => _hasher.VerifyHashedPassword(_user, _hash, password);
}

public sealed class AccountService(HrmDbContext db, UserManager<HrmUser> users, SignInManager<HrmUser> signIn,
    IAccountMessageSender sender, EmailOutboxService emailOutbox, MissingAccountPassword missing, TimeProvider clock)
{
    // Caller is a trusted provisioning workflow (T08), never a public registration endpoint.
    public async Task<Guid> InviteAsync(string username, string email, CancellationToken token)
    {
        var user = new HrmUser { Id = Guid.NewGuid(), UserName = username.Trim(), Email = email.Trim(), IsActive = false };
        var result = await users.CreateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException("Account provisioning failed validation.");
        var code = await users.GenerateUserTokenAsync(user, HrmClaims.ActivationProvider, HrmClaims.ActivationPurpose);
        await DeliverAsync(new AccountMessage(user.Email!, user.Id, "activation", code), token);
        return user.Id;
    }

    private async Task<HrmUser?> LockUserAsync(Guid id, CancellationToken token) =>
        await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {id}")
            .AsTracking().SingleOrDefaultAsync(token);

    public async Task<SessionGrant?> LoginAsync(string username, string password, bool web, CancellationToken token)
    {
        var found = await users.FindByNameAsync(username.Trim());
        if (found is null) { missing.Verify(password); return null; }
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var user = await LockUserAsync(found.Id, token);
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user) ||
            await db.EmployeeProfiles.AnyAsync(e => e.UserId == user.Id && e.Status == "DaNghiViec", token))
        {
            missing.Verify(password);
            await transaction.CommitAsync(token);
            return null;
        }
        if (!(await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true)).Succeeded)
        {
            await transaction.CommitAsync(token); // Persist the failed-password lockout counter.
            return null;
        }
        var session = new AccountSession
        {
            Id = Guid.NewGuid(), UserId = user.Id, SecurityStamp = await users.GetSecurityStampAsync(user),
            RefreshVersion = Guid.NewGuid(), ExpiresUtc = clock.GetUtcNow() + (web ? TimeSpan.FromHours(8) : TimeSpan.FromDays(7))
        };
        db.AccountSessions.Add(session);
        await db.SaveChangesAsync(token);
        var grant = await GrantAsync(user, session);
        await transaction.CommitAsync(token);
        return grant;
    }

    private async Task<SessionGrant> GrantAsync(HrmUser user, AccountSession session)
    {
        var principal = await signIn.CreateUserPrincipalAsync(user);
        var identity = (ClaimsIdentity)principal.Identity!;
        identity.AddClaim(new Claim(HrmClaims.SessionId, session.Id.ToString()));
        identity.AddClaim(new Claim(HrmClaims.RefreshVersion, session.RefreshVersion.ToString()));
        return new SessionGrant(principal, session.Id, session.RefreshVersion, session.ExpiresUtc);
    }

    public static bool TryIds(ClaimsPrincipal principal, out Guid userId, out Guid sessionId)
    {
        var valid = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        return Guid.TryParse(principal.FindFirstValue(HrmClaims.SessionId), out sessionId) && valid;
    }

    private IQueryable<AccountSession> ValidSession(ClaimsPrincipal principal)
    {
        TryIds(principal, out var userId, out var sessionId);
        var stamp = principal.FindFirstValue("AspNet.Identity.SecurityStamp");
        var now = clock.GetUtcNow();
        return db.AccountSessions.Where(s => s.Id == sessionId && s.UserId == userId && !s.Revoked && s.ExpiresUtc > now
            && s.SecurityStamp == stamp && db.Users.Any(u => u.Id == s.UserId && u.IsActive && u.SecurityStamp == stamp
                && (u.LockoutEnd == null || u.LockoutEnd <= now)
                && !db.EmployeeProfiles.Any(e => e.UserId == u.Id && e.Status == "DaNghiViec")));
    }

    public Task<bool> IsValidAsync(ClaimsPrincipal principal, CancellationToken token) => ValidSession(principal).AnyAsync(token);

    public async Task<SessionGrant?> RefreshAsync(ClaimsPrincipal principal, CancellationToken token)
    {
        if (!TryIds(principal, out var userId, out var sessionId) ||
            !Guid.TryParse(principal.FindFirstValue(HrmClaims.RefreshVersion), out var previous)) return null;
        // Same lock order as login/reset/revoke: user first, then session.
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var user = await LockUserAsync(userId, token);
        if (user is null || !user.IsActive || user.SecurityStamp != principal.FindFirstValue("AspNet.Identity.SecurityStamp") ||
            await db.EmployeeProfiles.AnyAsync(e => e.UserId == user.Id && e.Status == "DaNghiViec", token)) return null;
        var next = Guid.NewGuid();
        // Atomic compare-and-swap: two concurrent refreshes cannot both consume the same version.
        var affected = await ValidSession(principal).Where(s => s.RefreshVersion == previous)
            .ExecuteUpdateAsync(update => update.SetProperty(s => s.RefreshVersion, next), token);
        if (affected != 1) return null;
        var session = await db.AccountSessions.AsNoTracking().SingleAsync(s => s.Id == sessionId, token);
        var grant = await GrantAsync(user, session);
        await transaction.CommitAsync(token);
        return grant;
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken token)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is not null && user.IsActive && !await db.EmployeeProfiles.AnyAsync(e => e.UserId == user.Id && e.Status == "DaNghiViec", token))
        {
            // Queue into transactional outbox; dispatch by EmailOutboxWorker.
            // The token is embedded in the reset URL inside the email body — NEVER logged here.
            var resetToken = await users.GeneratePasswordResetTokenAsync(user);
            await DeliverAsync(new AccountMessage(user.Email!, user.Id, "reset-password", resetToken), token);
            try
            {
                await emailOutbox.QueuePasswordResetAsync(user.Email!, user.Id, resetToken, token);
            }
            catch (Exception) when (!token.IsCancellationRequested) { /* Outbox failure must not reveal account existence. */ }
        }
    }

    private async Task DeliverAsync(AccountMessage message, CancellationToken token)
    {
        // Delivery failures must not reveal whether the requested account exists.
        try { await sender.SendAsync(message, token); }
        catch (Exception) when (!token.IsCancellationRequested) { /* No credentials or message tokens in logs. */ }
    }

    public async Task<bool> ResetAsync(Guid id, string code, string password, bool activate, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var user = await LockUserAsync(id, token);
        if (user is null) return false;
        if (await db.EmployeeProfiles.AnyAsync(e => e.UserId == id && e.Status == "DaNghiViec", token)) return false;
        IdentityResult result;
        if (activate)
        {
            if (user.IsActive || user.PasswordHash is not null ||
                !await users.VerifyUserTokenAsync(user, HrmClaims.ActivationProvider, HrmClaims.ActivationPurpose, code)) return false;
            result = await users.AddPasswordAsync(user, password);
            if (result.Succeeded) { user.IsActive = true; user.EmailConfirmed = true; result = await users.UpdateAsync(user); }
        }
        else
        {
            if (!user.IsActive) return false;
            result = await users.ResetPasswordAsync(user, code, password);
        }
        if (!result.Succeeded) return false;
        await RevokeRowsAsync(id, token);
        await transaction.CommitAsync(token);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var user = await LockUserAsync(id, token);
        if (user is null || !user.IsActive || !(await users.ChangePasswordAsync(user, oldPassword, newPassword)).Succeeded) return false;
        await RevokeRowsAsync(id, token);
        await transaction.CommitAsync(token);
        return true;
    }

    private Task<int> RevokeRowsAsync(Guid id, CancellationToken token) => db.AccountSessions.Where(s => s.UserId == id && !s.Revoked)
        .ExecuteUpdateAsync(update => update.SetProperty(s => s.Revoked, true), token);

    public Task<int> LogoutAsync(Guid userId, Guid sessionId, CancellationToken token) => db.AccountSessions
        .Where(s => s.UserId == userId && s.Id == sessionId).ExecuteUpdateAsync(update => update.SetProperty(s => s.Revoked, true), token);

    // Server-side ports for offboarding/account administration in T07/T08; no public escalation endpoint.
    public async Task<bool> RevokeAllAsync(Guid id, bool disable, CancellationToken token)
    {
        // T08 offboarding may compose this into its existing personnel transaction.
        await using var transaction = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(token) : null;
        var user = await LockUserAsync(id, token);
        if (user is null) return false;
        if (disable) user.IsActive = false;
        if (!(await users.UpdateSecurityStampAsync(user)).Succeeded) return false;
        await RevokeRowsAsync(id, token);
        if (transaction is not null) await transaction.CommitAsync(token);
        return true;
    }
}
