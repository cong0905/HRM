using System.Text.Encodings.Web;
using HRM.Next.Api.Errors;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace HRM.Next.Api.Security;

// Fail closed until the real Identity adapter is implemented in T06.
// This handler never issues identities/tokens or trusts caller-provided headers.
public sealed class UnconfiguredIdentityHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        Results.Problem(ApiProblems.Create(Context, 401, "authentication.required")).ExecuteAsync(Context);

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Results.Problem(ApiProblems.Create(Context, 403, "authorization.forbidden")).ExecuteAsync(Context);
}
