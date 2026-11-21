using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Claims;
using System.Text.Encodings.Web;
using HRM.Next.Api;
using HRM.Next.Api.Errors;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Next.Tests.Integration;

public sealed class FixedClock : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(2026, 11, 11, 3, 4, 5, TimeSpan.Zero);
}

public sealed class QueryObserver : DbCommandInterceptor, ISaveChangesInterceptor
{
    public ConcurrentQueue<string> Commands { get; } = new();
    public int SaveCalls;
    public void Reset() { Commands.Clear(); SaveCalls = 0; }
    public ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref SaveCalls);
        return ValueTask.FromResult(result);
    }
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        Commands.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Commands.Enqueue(command.CommandText);
        return ValueTask.FromResult(result);
    }
}

public sealed class ApiFactory(string connectionString, bool testIdentity = true, bool probes = true)
    : WebApplicationFactory<Program>
{
    public QueryObserver Observer { get; } = new();

    public static string RepoRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !File.Exists(Path.Combine(current.FullName, "global.json"))) current = current.Parent;
            return current?.FullName ?? throw new InvalidOperationException("Repository root not found.");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseContentRoot(Path.Combine(RepoRoot, "backend/src/HRM.Api"));
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Hrm"] = connectionString,
            ["Api:ExposeOpenApi"] = "true"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider, FixedClock>();
            services.AddDbContext<HrmDbContext>((_, options) => options.AddInterceptors(Observer));
            if (probes) services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
            if (testIdentity)
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "IntegrationOnly";
                    options.DefaultChallengeScheme = "IntegrationOnly";
                    options.DefaultForbidScheme = "IntegrationOnly";
                }).AddScheme<AuthenticationSchemeOptions, TestIdentityHandler>("IntegrationOnly", _ => { });
        });
    }
}

// Exists only in the test assembly, never referenced by the product API.
public sealed class TestIdentityHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers["X-Test-Identity"] == "anonymous") return Task.FromResult(AuthenticateResult.NoResult());
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "integration-user") };
        if (Request.Headers["X-Test-Permission"] != "none") claims.Add(new Claim("permission", "departments.read"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name)));
    }
    protected override Task HandleChallengeAsync(AuthenticationProperties properties) =>
        Results.Problem(ApiProblems.Create(Context, 401, "authentication.required")).ExecuteAsync(Context);
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties) =>
        Results.Problem(ApiProblems.Create(Context, 403, "authorization.forbidden")).ExecuteAsync(Context);
}
