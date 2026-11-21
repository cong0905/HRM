using System.Threading.RateLimiting;
using HRM.Next.Api.Errors;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace HRM.Next.Api.Auth;

public static class AuthConfiguration
{
    public static IServiceCollection AddHrmIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var protection = services.AddDataProtection().SetApplicationName("HRM.Next");
        if (configuration["DataProtection:KeyDirectory"] is { Length: > 0 } directory)
            protection.PersistKeysToFileSystem(new DirectoryInfo(directory));
        services.AddHttpContextAccessor();
        services.AddScoped<HRM.Next.Application.Authorization.ISecurityContext, SecurityContext>();
        services.AddIdentityCore<HrmUser>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireDigit = options.Password.RequireLowercase = options.Password.RequireUppercase = options.Password.RequireNonAlphanumeric = true;
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<IdentityRole<Guid>>()
            .AddClaimsPrincipalFactory<HrmUserClaimsPrincipalFactory>()
            .AddEntityFrameworkStores<HrmDbContext>()
            .AddSignInManager().AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromMinutes(15));
        services.AddScoped<AccountService>();
        services.AddSingleton<MissingAccountPassword>();
        services.AddSingleton<IAccountMessageSender, UnconfiguredAccountMessageSender>();
        services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, _ => { })
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = SessionAuthenticationHandler.CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;
            })
            .AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
                options.RefreshTokenExpiration = TimeSpan.FromDays(7);
            });
        // Activation has a separate provider/purpose from email verification/password reset.
        services.AddTransient<ActivationTokenProvider>();
        services.Configure<IdentityOptions>(options => options.Tokens.ProviderMap[HrmClaims.ActivationProvider] =
            new TokenProviderDescriptor(typeof(ActivationTokenProvider)));
        services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme)
            .Configure<TimeProvider>((options, clock) => options.TimeProvider = clock);
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<TimeProvider>((options, clock) => options.TimeProvider = clock);
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-XSRF-TOKEN";
            options.Cookie.Name = "__Host-Hrm.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
                { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await Results.Problem(ApiProblems.Create(context.HttpContext, 429, "auth.rate_limited")).ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }
}

public sealed class ActivationTokenProvider(IDataProtectionProvider provider, ILogger<DataProtectorTokenProvider<HrmUser>> logger)
    : DataProtectorTokenProvider<HrmUser>(provider, Microsoft.Extensions.Options.Options.Create(new DataProtectionTokenProviderOptions
        { Name = HrmClaims.ActivationProvider, TokenLifespan = TimeSpan.FromHours(24) }), logger);

[AttributeUsage(AttributeTargets.Method)]
public sealed class WebCsrfAttribute : Attribute;

public sealed class CookieCsrfMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAntiforgery antiforgery)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1/auth")) context.Response.Headers.CacheControl = "no-store";
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method))
        {
            var needsToken = context.Request.Cookies.ContainsKey(SessionAuthenticationHandler.CookieName) ||
                context.GetEndpoint()?.Metadata.GetMetadata<WebCsrfAttribute>() is not null;
            if (needsToken)
            {
                try { await antiforgery.ValidateRequestAsync(context); }
                catch (AntiforgeryValidationException)
                {
                    await Results.Problem(ApiProblems.Create(context, 400, "csrf.invalid")).ExecuteAsync(context);
                    return;
                }
            }
        }
        await next(context);
    }
}
