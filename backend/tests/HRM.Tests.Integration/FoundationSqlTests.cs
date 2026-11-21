using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Common;
using HRM.Next.Application.Departments;
using HRM.Next.Domain.Departments;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace HRM.Next.Tests.Integration;

public sealed partial class FoundationSqlTests(SqlDatabaseFixture sql, ITestOutputHelper output) : IClassFixture<SqlDatabaseFixture>
{
    private async Task SeedAsync(int count)
    {
        await sql.ResetAsync();
        await using var db = sql.CreateContext();
        db.Departments.AddRange(Enumerable.Range(0, count)
            .Select(i => Department.Create($"Department {i:D3}", new FixedClock().GetUtcNow())));
        await db.SaveChangesAsync();
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("DO_NOT_EXPOSE", text);
        Assert.DoesNotContain("private_table", text);
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);
        var body = JsonDocument.Parse(text).RootElement.Clone();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("errorCode").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
        return body;
    }

    [Fact]
    public async Task UsesRealSqlServerAndAppliedMigrations()
    {
        await using var db = sql.CreateContext();
        Assert.True(db.Database.IsSqlServer());
        Assert.StartsWith("HRM_Next_Test_T05_", sql.DatabaseName);
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        output.WriteLine($"SQL Server {sql.ServerVersion}; owned database {sql.DatabaseName}; provider {db.Database.ProviderName}");
    }

    [Fact]
    public async Task ConcurrentRequests_GetDistinctContextsAndReuseContextWithinEachRequest()
    {
        await SeedAsync(2);
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var results = await Task.WhenAll(Enumerable.Range(0, 24)
            .Select(_ => client.GetFromJsonAsync<ScopeResult>("/_tests/foundation/scope")));
        Assert.All(results, result => { Assert.NotNull(result); Assert.True(result.SameWithinRequest); Assert.Equal(0, result.TrackedEntries); });
        Assert.Equal(24, results.Select(x => x!.ContextId).Distinct().Count());
    }

    [Fact]
    public async Task RepositoryAdd_DoesNotCommitBeforeUseCaseBoundary()
    {
        await SeedAsync(0);
        await using var factory = new ApiFactory(sql.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();
        repo.Add(Department.Create("Not committed yet", new FixedClock().GetUtcNow()));
        await using (var independent = sql.CreateContext()) Assert.Equal(0, await independent.Departments.CountAsync());
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(_ => Task.FromResult(true), CancellationToken.None);
        await using var after = sql.CreateContext();
        Assert.Equal(1, await after.Departments.CountAsync());
    }

    [Fact]
    public async Task FailureAfterTwoSqlWrites_RollsBackBothAndRedactsProblemDetails()
    {
        await SeedAsync(0);
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        factory.Observer.Reset();
        using var response = await client.PostAsJsonAsync("/_tests/foundation/transaction", new TransactionRequest(true));
        await ProblemAsync(response, HttpStatusCode.InternalServerError);
        Assert.True(factory.Observer.SaveCalls >= 2); // The failed use case actually flushed both steps.
        Assert.True(factory.Observer.Commands.Count(command => command.Contains("INSERT INTO", StringComparison.OrdinalIgnoreCase)) >= 2);
        await using var db = sql.CreateContext();
        Assert.Equal(0, await db.Departments.CountAsync());
    }

    [Fact]
    public async Task SuccessfulTwoStepUseCase_CommitsBothWrites()
    {
        await SeedAsync(0);
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/_tests/foundation/transaction", new TransactionRequest(false));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var db = sql.CreateContext();
        Assert.Equal(2, await db.Departments.CountAsync());
    }

    [Fact]
    public async Task ExplicitTrackedWrite_PersistsThroughUnitOfWork()
    {
        await SeedAsync(1);
        await using var factory = new ApiFactory(sql.ConnectionString);
        Guid id;
        await using (var before = sql.CreateContext()) id = await before.Departments.Select(x => x.Id).SingleAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();
            var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unit.ExecuteAsync(async token =>
            {
                var department = await repo.FindTrackedAsync(id, token);
                Assert.NotNull(department);
                department.Rename("Renamed through tracked command");
                return true;
            }, CancellationToken.None);
        }
        await using var after = sql.CreateContext();
        Assert.Equal("Renamed through tracked command", await after.Departments.Select(x => x.Name).SingleAsync());
    }

    [Fact]
    public async Task CancellationAfterSqlWrite_RollsBackAndClearsPendingState()
    {
        await SeedAsync(0);
        await using var factory = new ApiFactory(sql.ConnectionString);
        await using var scope = factory.Services.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();
        var repo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => unit.ExecuteAsync(async token =>
        {
            repo.Add(Department.Create("Cancelled write", new FixedClock().GetUtcNow()));
            await unit.SaveChangesAsync(token);
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return true;
        }, cancellation.Token));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<HrmDbContext>().ChangeTracker.Entries());
        await using var db = sql.CreateContext();
        Assert.Equal(0, await db.Departments.CountAsync());
    }

    [Fact]
    public async Task ReadEndpoint_ProjectsWithoutTrackingOrWritesAndPaginatesDeterministically()
    {
        await SeedAsync(22);
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        factory.Observer.Reset();
        var first = await client.GetFromJsonAsync<PagedResult<DepartmentResponse>>("/api/v1/departments");
        var second = await client.GetFromJsonAsync<PagedResult<DepartmentResponse>>("/api/v1/departments?page=2");
        var tracking = await client.GetFromJsonAsync<ScopeResult>("/_tests/foundation/read-tracking");
        Assert.NotNull(first); Assert.NotNull(second); Assert.NotNull(tracking);
        Assert.Equal(20, first.Items.Count); Assert.Equal(2, second.Items.Count);
        Assert.Equal(22, first.TotalCount); Assert.Equal(2, first.TotalPages);
        Assert.Equal("Department 000", first.Items[0].Name);
        Assert.Equal("Department 020", second.Items[0].Name);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)));
        Assert.Equal(0, tracking.TrackedEntries);
        Assert.Equal(0, factory.Observer.SaveCalls);
        Assert.NotEmpty(factory.Observer.Commands);
        Assert.All(factory.Observer.Commands, command => Assert.StartsWith("SELECT", command.TrimStart(), StringComparison.OrdinalIgnoreCase));
        await using var db = sql.CreateContext();
        Assert.Equal(22, await db.Departments.CountAsync());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("sort=untrustedColumn")]
    [InlineData("page=DO_NOT_EXPOSE")]
    public async Task InvalidPagination_ReturnsSafe400(string query)
    {
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/v1/departments?" + query);
        await ProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MaximumPageSizeAndWhitelistedDescendingSort_Work()
    {
        await SeedAsync(3);
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var page = await client.GetFromJsonAsync<PagedResult<DepartmentResponse>>("/api/v1/departments?pageSize=100&sort=-name");
        Assert.NotNull(page);
        Assert.Equal(100, page.PageSize);
        Assert.Equal("Department 002", page.Items[0].Name);
    }

    [Fact]
    public async Task UnconfiguredIdentity_FailsClosedEvenWithForgedTestHeaders()
    {
        await using var factory = new ApiFactory(sql.ConnectionString, testIdentity: false, probes: false);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer forged");
        client.DefaultRequestHeaders.Add("X-Test-Permission", "reader");
        using var response = await client.GetAsync("/api/v1/departments");
        await ProblemAsync(response, HttpStatusCode.Unauthorized);
        var info = await client.GetFromJsonAsync<JsonElement>("/api/v1/system/info");
        Assert.Equal("2026-11-11T03:04:05.0000000Z", info.GetProperty("serverTimeUtc").GetString());
    }

    [Fact]
    public async Task MissingPermission_Returns403()
    {
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Permission", "none");
        using var response = await client.GetAsync("/api/v1/departments");
        await ProblemAsync(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Health_ReportsRealDatabaseReadiness()
    {
        await using var factory = new ApiFactory(sql.ConnectionString, testIdentity: false);
        using var client = factory.CreateClient();
        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }

    [Fact]
    public async Task MigrationDownAndUpgrade_KeepReadinessClosedUntilSchemaIsCurrent()
    {
        await using var db = sql.CreateContext();
        await using var factory = new ApiFactory(sql.ConnectionString, testIdentity: false, probes: false);
        using var client = factory.CreateClient();
        try
        {
            await db.GetService<IMigrator>().MigrateAsync("0");
            Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
            using var live = await client.GetAsync("/health/live");
            using var ready = await client.GetAsync("/health/ready");
            Assert.Equal(HttpStatusCode.OK, live.StatusCode);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        }
        finally
        {
            await db.Database.MigrateAsync();
        }
        using var upgraded = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, upgraded.StatusCode);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await db.Database.GetAppliedMigrationsAsync());
    }

    [Theory]
    [InlineData("conflict", HttpStatusCode.Conflict, "resource.conflict")]
    [InlineData("domain", HttpStatusCode.UnprocessableEntity, "domain.rule")]
    public async Task KnownErrors_UseDeclared409And422ProblemContracts(string kind, HttpStatusCode status, string code)
    {
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/_tests/foundation/errors/" + kind);
        var problem = await ProblemAsync(response, status);
        Assert.Equal(code, problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task UnavailableDatabase_DoesNotBreakLivenessOrLeakDiagnostics()
    {
        await using var factory = new ApiFactory("Server=127.0.0.1,1;Database=HRM_Next_Test_T05_Unavailable;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=1;ConnectRetryCount=0", testIdentity: false);
        using var client = factory.CreateClient();
        using var live = await client.GetAsync("/health/live");
        using var ready = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqlException", body);
        Assert.DoesNotContain("127.0.0.1", body);
    }

    [Fact]
    public async Task JsonContracts_PreserveDecimalPrecisionAndNormalizeUtc()
    {
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        const string input = """{"date":"2026-11-11","amount":"9007199254740993.12","timestamp":"2026-11-11T10:04:05+07:00"}""";
        using var response = await client.PostAsync("/_tests/foundation/conventions", new StringContent(input, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("9007199254740993.12", body.GetProperty("amount").GetString());
        Assert.Equal("2026-11-11", body.GetProperty("date").GetString());
        Assert.Equal("2026-11-11T03:04:05.0000000Z", body.GetProperty("timestamp").GetString());
    }

    [Theory]
    [InlineData("2026-11-11", "1.23", "2026-11-11T03:04:05Z")]
    [InlineData("2026-1-1", "\"1.23\"", "2026-11-11T03:04:05Z")]
    [InlineData("2026-11-11", "\"0.12345678901234567890123456789123\"", "2026-11-11T03:04:05Z")]
    [InlineData("2026-11-11", "\"1.23\"", "2026-11-11T03:04:05")]
    public async Task InvalidJsonConventions_AreRejected(string date, string amountJson, string timestamp)
    {
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        var input = $$"""{"date":"{{date}}","amount":{{amountJson}},"timestamp":"{{timestamp}}"}""";
        using var response = await client.PostAsync("/_tests/foundation/conventions", new StringContent(input, Encoding.UTF8, "application/json"));
        await ProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MissingRequiredDecimal_DoesNotSilentlyBecomeZero()
    {
        await using var factory = new ApiFactory(sql.ConnectionString);
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/_tests/foundation/conventions", new StringContent(
            """{"date":"2026-11-11","timestamp":"2026-11-11T03:04:05Z"}""", Encoding.UTF8, "application/json"));
        await ProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProductOpenApi_ContainsSharedConventionsAndNoTestRoutes()
    {
        await using var factory = new ApiFactory(sql.ConnectionString, testIdentity: false, probes: false);
        using var client = factory.CreateClient();
        var document = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/v1/departments", out _));
        Assert.All(document.GetProperty("paths").EnumerateObject(), path => Assert.DoesNotContain("_tests", path.Name));
        var schemas = document.GetProperty("components").GetProperty("schemas");
        Assert.Equal("string", schemas.GetProperty("DecimalString").GetProperty("type").GetString());
        Assert.Equal("date", schemas.GetProperty("LocalDate").GetProperty("format").GetString());
        Assert.Equal("date-time", schemas.GetProperty("UtcTimestamp").GetProperty("format").GetString());
        Assert.Equal("string", schemas.GetProperty("SystemInfo").GetProperty("properties").GetProperty("serverTimeUtc").GetProperty("type").GetString());
    }
}
