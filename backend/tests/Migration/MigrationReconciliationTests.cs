using HRM.Next.Domain;
using HRM.Ops.Migration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration.Migration;

public sealed class MigrationReconciliationTests(MigrationDualDatabaseFixture fixture) : IClassFixture<MigrationDualDatabaseFixture>
{
    private static readonly string FixturesDir = MigrationDualDatabaseFixture.FixturesDir;

    private async Task SetupAndMigrateCleanDataAsync()
    {
        await fixture.ResetNextDatabaseAsync();

        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var cleanSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_clean_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(cleanSql);

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();
        var engine = new MigrationEngine(reader, nextDb);
        await engine.ExecuteMigrationAsync();
    }

    [Fact]
    public async Task Reconciliation_CleanRehearsal_MatchesRowCounts_AndFinancialSums_WithZeroTolerance()
    {
        await SetupAndMigrateCleanDataAsync();

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();

        var reconciler = new Reconciler(reader, nextDb);
        var report = await reconciler.ReconcileAsync(fixture.LegacyDatabaseName, fixture.NextDatabaseName);

        // Overall Passed
        Assert.True(report.Passed, "Reconciliation must pass for clean rehearsal migration.");

        // Table counts
        foreach (var table in report.Tables)
        {
            Assert.True(table.Matched, $"Table {table.TableName} should match (discrepancy: {table.Discrepancy}).");
            Assert.Equal(0, table.Discrepancy);
        }

        // Financial Payroll Reconciliation
        Assert.Single(report.PayrollPeriods);
        var periodRecon = report.PayrollPeriods[0];

        Assert.Equal("2026-04", periodRecon.PeriodCode);
        Assert.Equal(4, periodRecon.LegacyRecordCount);
        Assert.Equal(4, periodRecon.NextRecordCount);

        // Sum of LuongThucNhan in clean seed: 47,250,000 + 28,550,000 + 19,320,454.55 + 13,675,000 = 108,795,454.55
        Assert.Equal(108795454.55m, periodRecon.LegacyNetTotal);
        Assert.Equal(108795454.55m, periodRecon.NextNetTotal);
        Assert.Equal(0.00m, periodRecon.NetDiscrepancy);
        Assert.True(periodRecon.Matched);

        // Leave Balances
        Assert.NotNull(report.LeaveBalances);
        Assert.True(report.LeaveBalances.Matched);
        Assert.Equal(report.LeaveBalances.TotalAllocatedDaysLegacy, report.LeaveBalances.TotalAllocatedDaysNext);
        Assert.Equal(report.LeaveBalances.TotalUsedDaysLegacy, report.LeaveBalances.TotalUsedDaysNext);

        // Markdown report generation check
        var markdown = Reconciler.GenerateMarkdownReport(report);
        Assert.Contains("# Báo Cáo Đối Soát Dữ Liệu Di Chuyển", markdown);
        Assert.Contains("PASSED", markdown);
        Assert.Contains("2026-04", markdown);
    }

    [Fact]
    public async Task Reconciliation_PayrollSnapshots_AreSealedAndImmutable()
    {
        await SetupAndMigrateCleanDataAsync();

        await using var nextDb = fixture.CreateNextContext();

        var snapshots = await nextDb.PayrollCalculationSnapshots.ToListAsync();
        Assert.Equal(4, snapshots.Count);

        // Historical snapshots are append-only / immutable
        var snapshot = snapshots[0];
        snapshot.Net = 999999999m;

        nextDb.Attach(snapshot);
        nextDb.Entry(snapshot).State = EntityState.Modified;

        var ex = await Assert.ThrowsAsync<DomainRuleException>(() => nextDb.SaveChangesAsync());
        Assert.Equal("payroll.snapshot_immutable", ex.Code);
    }
}
