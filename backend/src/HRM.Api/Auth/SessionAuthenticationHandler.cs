using System.Text.Encodings.Web;
using HRM.Next.Api.Errors;
using HRM.Next.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HRM.Next.Api.Auth;

// Framework handlers validate signatures/expiry. This adapter adds SQL-backed revocation on every request.
public sealed class SessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, AccountService accounts) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "HrmSession";
    public const string CookieName = "__Host-Hrm.Session";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var scheme = Request.Headers.ContainsKey("Authorization") ? IdentityConstants.BearerScheme : IdentityConstants.ApplicationScheme;
        var result = await Context.AuthenticateAsync(scheme);
        if (!result.Succeeded || result.Principal is null) return result;
        if (!await accounts.IsValidAsync(result.Principal, Context.RequestAborted))
            return AuthenticateResult.Fail("Session is no longer valid.");
        return AuthenticateResult.Success(new AuthenticationTicket(result.Principal, result.Properties, SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        Results.Problem(ApiProblems.Create(Context, 401, "authentication.required")).ExecuteAsync(Context);
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Results.Problem(ApiProblems.Create(Context, 403, "authorization.forbidden")).ExecuteAsync(Context);
}
