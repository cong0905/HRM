using HRM.Ops.Migration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration.Migration;

public sealed class MigrationCutoverRollbackTests(MigrationDualDatabaseFixture fixture) : IClassFixture<MigrationDualDatabaseFixture>
{
    private static readonly string FixturesDir = MigrationDualDatabaseFixture.FixturesDir;

    [Fact]
    public async Task Cutover_RollbackDrill_RestoresCleanTargetStateOnAbortedTransaction()
    {
        await fixture.ResetNextDatabaseAsync();

        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var cleanSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_clean_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(cleanSql);

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();

        // Start transaction
        await using var tx = await nextDb.Database.BeginTransactionAsync();

        var engine = new MigrationEngine(reader, nextDb);
        await engine.ExecuteMigrationAsync();

        // Verify records exist inside transaction
        var inFlightEmployees = await nextDb.EmployeeProfiles.CountAsync();
        Assert.Equal(5, inFlightEmployees);

        // Abort / Rollback drill
        await tx.RollbackAsync();

        // Create fresh context to verify post-rollback state
        await using var verifyDb = fixture.CreateNextContext();
        var rolledBackEmployees = await verifyDb.EmployeeProfiles.CountAsync();
        Assert.Equal(0, rolledBackEmployees);
        var rolledBackDepartments = await verifyDb.Departments.CountAsync();
        Assert.Equal(0, rolledBackDepartments);
    }
}
