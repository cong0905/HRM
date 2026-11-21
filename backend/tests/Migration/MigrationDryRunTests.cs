using HRM.Ops.Migration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration.Migration;

public sealed class MigrationDryRunTests(MigrationDualDatabaseFixture fixture) : IClassFixture<MigrationDualDatabaseFixture>
{
    private static readonly string FixturesDir = MigrationDualDatabaseFixture.FixturesDir;

    [Fact]
    public async Task DryRun_OnDirtyLegacyData_DetectsDuplicates_Orphans_AndInvalidValues()
    {
        // Setup dirty schema and data on legacy DB
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var dirtySql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_dirty_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(dirtySql);

        var reader = fixture.CreateLegacyReader();
        var validator = new MigrationValidator(reader);

        var report = await validator.ValidateAsync(fixture.LegacyDatabaseName, fixture.NextDatabaseName);

        Assert.False(report.CanProceed, "Dry run must fail when errors are present in dirty data.");
        Assert.NotEmpty(report.Issues);

        var ruleCodes = report.Issues.Select(i => i.RuleCode).ToHashSet();
        Assert.Contains("DUPLICATE_USERNAME", ruleCodes);
        Assert.Contains("DUPLICATE_CCCD", ruleCodes);
        Assert.Contains("DUPLICATE_EMAIL", ruleCodes);
        Assert.Contains("ORPHAN_DEPARTMENT", ruleCodes);
        Assert.Contains("ORPHAN_POSITION", ruleCodes);
        Assert.Contains("ORPHAN_EMPLOYEE_ACCOUNT", ruleCodes);
        Assert.Contains("ORPHAN_ATTENDANCE", ruleCodes);
        Assert.Contains("INVALID_SALARY", ruleCodes);
        Assert.Contains("INVALID_LEAVE_DATES", ruleCodes);
        Assert.Contains("CROSS_YEAR_LEAVE", ruleCodes);
        Assert.Contains("LEAVE_OVERDRAFT", ruleCodes);
    }

    [Fact]
    public async Task DryRun_DoesNotMutate_SourceOrTargetDatabases()
    {
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var dirtySql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_dirty_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(dirtySql);

        var reader = fixture.CreateLegacyReader();

        // Baseline legacy counts
        var empBefore = (await reader.ReadEmployeesAsync()).Count;
        var accBefore = (await reader.ReadAccountsAsync()).Count;

        // Baseline next counts
        await using var nextDb = fixture.CreateNextContext();
        var nextEmpBefore = await nextDb.EmployeeProfiles.CountAsync();
        var nextUserBefore = await nextDb.Users.CountAsync();

        var validator = new MigrationValidator(reader);
        var report = await validator.ValidateAsync(fixture.LegacyDatabaseName, fixture.NextDatabaseName);

        // Verify legacy unchanged
        var empAfter = (await reader.ReadEmployeesAsync()).Count;
        var accAfter = (await reader.ReadAccountsAsync()).Count;
        Assert.Equal(empBefore, empAfter);
        Assert.Equal(accBefore, accAfter);

        // Verify next unchanged
        var nextEmpAfter = await nextDb.EmployeeProfiles.CountAsync();
        var nextUserAfter = await nextDb.Users.CountAsync();
        Assert.Equal(nextEmpBefore, nextEmpAfter);
        Assert.Equal(nextUserBefore, nextUserAfter);
    }

    [Fact]
    public async Task DryRun_OnCleanLegacyData_ReportsZeroErrors_CanProceed()
    {
        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var cleanSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_clean_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(cleanSql);

        var reader = fixture.CreateLegacyReader();
        var validator = new MigrationValidator(reader);

        var report = await validator.ValidateAsync(fixture.LegacyDatabaseName, fixture.NextDatabaseName);

        Assert.True(report.CanProceed, "Clean data must pass dry-run validation.");
        Assert.DoesNotContain(report.Issues, i => i.Severity == IssueSeverity.Error);
        Assert.Equal(5, report.ValidTableCounts["NhanVien"]);
        Assert.Equal(5, report.ValidTableCounts["TaiKhoan"]);
        Assert.Equal(3, report.ValidTableCounts["PhongBan"]);
    }
}
