using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HRM.Next.Api.Errors;
using HRM.Next.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace HRM.Next.Api.Auth;

public sealed record LoginRequest([Required, StringLength(256)] string Username, [Required, StringLength(128)] string Password);
public sealed record ForgotRequest([Required, EmailAddress, StringLength(256)] string Email);
public sealed record ResetRequest(Guid UserId, [Required, StringLength(8192)] string Code, [Required, StringLength(128, MinimumLength = 12)] string NewPassword);
public sealed record ChangePasswordRequest([Required, StringLength(128)] string CurrentPassword, [Required, StringLength(128, MinimumLength = 12)] string NewPassword);
public sealed record RefreshRequest([Required, StringLength(8192)] string RefreshToken);
public sealed record TokenPair(string TokenType, string AccessToken, long ExpiresIn, string RefreshToken);
public sealed record DeliveryResponse(string DeliveryMode, string Message);
public sealed record CsrfResponse(string RequestToken);
public sealed record CurrentAccount(Guid Id, string? Username);

[ApiController]
[RequireHttps]
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(429)]
public sealed class AuthController(AccountService accounts, IAccountMessageSender sender, TimeProvider clock,
    IOptionsMonitor<BearerTokenOptions> options) : ControllerBase
{
    private ObjectResult Failure(int status = 401, string code = "auth.invalid") =>
        new(ApiProblems.Create(HttpContext, status, code)) { StatusCode = status, ContentTypes = { "application/problem+json" } };

    [HttpGet("csrf"), AllowAnonymous]
    public CsrfResponse Csrf([FromServices] IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-store";
        return new CsrfResponse(antiforgery.GetAndStoreTokens(HttpContext).RequestToken!);
    }

    [HttpPost("web/login"), AllowAnonymous, WebCsrf]
    [ProducesResponseType(204)]
    public async Task<IActionResult> WebLogin(LoginRequest request, CancellationToken token)
    {
        var grant = await accounts.LoginAsync(request.Username, request.Password, web: true, token);
        if (grant is null) return Failure();
        await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, grant.Principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = grant.ExpiresUtc, AllowRefresh = false });
        Response.Headers.CacheControl = "no-store";
        return NoContent();
    }

    [HttpPost("mobile/login"), AllowAnonymous]
    [ProducesResponseType<TokenPair>(200)]
    public async Task<ActionResult<TokenPair>> MobileLogin(LoginRequest request, CancellationToken token)
    {
        var grant = await accounts.LoginAsync(request.Username, request.Password, web: false, token);
        return grant is null ? Failure() : Tokens(grant);
    }

    private TokenPair Tokens(SessionGrant grant)
    {
        var settings = options.Get(IdentityConstants.BearerScheme);
        var now = clock.GetUtcNow();
        var expiry = now + settings.BearerTokenExpiration;
        if (expiry > grant.ExpiresUtc) expiry = grant.ExpiresUtc;
        // Use framework purpose-separated ticket protectors; no custom signing/encryption algorithm.
        var access = settings.BearerTokenProtector.Protect(new AuthenticationTicket(grant.Principal,
            new AuthenticationProperties { ExpiresUtc = expiry }, IdentityConstants.BearerScheme + ":AccessToken"));
        var refresh = settings.RefreshTokenProtector.Protect(new AuthenticationTicket(grant.Principal,
            new AuthenticationProperties { ExpiresUtc = grant.ExpiresUtc }, IdentityConstants.BearerScheme + ":RefreshToken"));
        Response.Headers.CacheControl = "no-store";
        return new TokenPair("Bearer", access, (long)(expiry - now).TotalSeconds, refresh);
    }

    [HttpPost("mobile/refresh"), AllowAnonymous]
    [ProducesResponseType<TokenPair>(200)]
    public async Task<ActionResult<TokenPair>> Refresh(RefreshRequest request, CancellationToken token)
    {
        var ticket = options.Get(IdentityConstants.BearerScheme).RefreshTokenProtector.Unprotect(request.RefreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expiry || expiry <= clock.GetUtcNow()) return Failure();
        var grant = await accounts.RefreshAsync(ticket.Principal, token);
        return grant is null ? Failure() : Tokens(grant);
    }

    [HttpPost("forgot-password"), AllowAnonymous]
    [ProducesResponseType<DeliveryResponse>(202)]
    public async Task<ActionResult<DeliveryResponse>> Forgot(ForgotRequest request, CancellationToken token)
    {
        await accounts.ForgotPasswordAsync(request.Email, token);
        return Accepted(new DeliveryResponse(sender.Mode, sender.Mode == "not-configured"
            ? "Chưa cấu hình email; không có email nào được gửi."
            : "Yêu cầu đã tiếp nhận. Kênh gửi thử không gửi email thật; chi tiết tài khoản không được công bố."));
    }

    [HttpPost("reset-password"), AllowAnonymous]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Reset(ResetRequest request, CancellationToken token) =>
        await accounts.ResetAsync(request.UserId, request.Code, request.NewPassword, activate: false, token) ? NoContent() : Failure(400, "auth.reset_invalid");

    [HttpPost("activate"), AllowAnonymous]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Activate(ResetRequest request, CancellationToken token) =>
        await accounts.ResetAsync(request.UserId, request.Code, request.NewPassword, activate: true, token) ? NoContent() : Failure(400, "auth.activation_invalid");

    [HttpGet("me"), Authorize]
    public CurrentAccount Me() => new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), User.Identity?.Name);

    [HttpPost("change-password"), Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken token)
    {
        AccountService.TryIds(User, out var userId, out _);
        if (!await accounts.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, token)) return Failure(400, "auth.password_invalid");
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return NoContent();
    }

    [HttpPost("logout"), Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout(CancellationToken token)
    {
        AccountService.TryIds(User, out var userId, out var sessionId);
        await accounts.LogoutAsync(userId, sessionId, token);
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return NoContent();
    }

    [HttpPost("logout-all"), Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> LogoutAll(CancellationToken token)
    {
        AccountService.TryIds(User, out var userId, out _);
        await accounts.RevokeAllAsync(userId, disable: false, token);
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return NoContent();
    }
}
