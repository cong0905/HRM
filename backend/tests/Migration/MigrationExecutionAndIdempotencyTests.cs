using HRM.Ops.Migration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration.Migration;

public sealed class MigrationExecutionAndIdempotencyTests(MigrationDualDatabaseFixture fixture) : IClassFixture<MigrationDualDatabaseFixture>
{
    private static readonly string FixturesDir = MigrationDualDatabaseFixture.FixturesDir;

    private async Task SetupCleanLegacyAsync()
    {
        await fixture.ResetNextDatabaseAsync();

        var schemaSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_schema.sql"));
        var cleanSql = await File.ReadAllTextAsync(Path.Combine(FixturesDir, "legacy_clean_seed.sql"));

        await fixture.ExecuteSqlScriptOnLegacyAsync(schemaSql);
        await fixture.ExecuteSqlScriptOnLegacyAsync(cleanSql);
    }

    [Fact]
    public async Task Migration_CleanData_ExecutesSuccessfully_PopulatesAllTables()
    {
        await SetupCleanLegacyAsync();

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();
        var engine = new MigrationEngine(reader, nextDb);

        var ledger = await engine.ExecuteMigrationAsync();

        Assert.Equal(3, ledger.Departments.Count);
        Assert.Equal(3, ledger.Positions.Count);
        Assert.Equal(5, ledger.Employees.Count);
        Assert.Equal(5, ledger.Accounts.Count);
        Assert.Equal(6, ledger.Attendance.Count);
        Assert.Equal(3, ledger.LeaveRecords.Count);
        Assert.Single(ledger.PayrollPeriods);
        Assert.Equal(4, ledger.Payslips.Count);

        // Verify DB counts
        Assert.Equal(3, await nextDb.Departments.CountAsync());
        Assert.Equal(3, await nextDb.Positions.CountAsync());
        Assert.Equal(5, await nextDb.EmployeeProfiles.CountAsync());
        Assert.Equal(5, await nextDb.Users.CountAsync());
        Assert.Equal(6, await nextDb.AttendanceSummaries.CountAsync());
        Assert.Equal(3, await nextDb.LeaveRecords.CountAsync());
        Assert.Equal(5, await nextDb.LeaveBalances.CountAsync());
        Assert.Equal(1, await nextDb.PayrollPeriods.CountAsync());
        Assert.Equal(4, await nextDb.Payslips.CountAsync());
    }

    [Fact]
    public async Task Migration_Rerun_IsIdempotent_CreatesZeroDuplicates()
    {
        await SetupCleanLegacyAsync();

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();
        var engine = new MigrationEngine(reader, nextDb);

        // Run 1
        await engine.ExecuteMigrationAsync();

        // Capture counts after run 1
        var deptCount1 = await nextDb.Departments.CountAsync();
        var posCount1 = await nextDb.Positions.CountAsync();
        var empCount1 = await nextDb.EmployeeProfiles.CountAsync();
        var userCount1 = await nextDb.Users.CountAsync();
        var attCount1 = await nextDb.AttendanceSummaries.CountAsync();
        var leaveCount1 = await nextDb.LeaveRecords.CountAsync();
        var payPeriodCount1 = await nextDb.PayrollPeriods.CountAsync();
        var payslipCount1 = await nextDb.Payslips.CountAsync();

        // Run 2 (Immediately rerun migration)
        var exception = await Record.ExceptionAsync(() => engine.ExecuteMigrationAsync());
        Assert.Null(exception); // Must not throw unique constraint violations or errors

        // Assert counts after run 2 are identical (zero duplicates)
        Assert.Equal(deptCount1, await nextDb.Departments.CountAsync());
        Assert.Equal(posCount1, await nextDb.Positions.CountAsync());
        Assert.Equal(empCount1, await nextDb.EmployeeProfiles.CountAsync());
        Assert.Equal(userCount1, await nextDb.Users.CountAsync());
        Assert.Equal(attCount1, await nextDb.AttendanceSummaries.CountAsync());
        Assert.Equal(leaveCount1, await nextDb.LeaveRecords.CountAsync());
        Assert.Equal(payPeriodCount1, await nextDb.PayrollPeriods.CountAsync());
        Assert.Equal(payslipCount1, await nextDb.Payslips.CountAsync());
    }

    [Fact]
    public async Task Migration_Interrupted_ResumesCleanlyWithoutDuplicates()
    {
        await SetupCleanLegacyAsync();

        var reader = fixture.CreateLegacyReader();
        await using var nextDb = fixture.CreateNextContext();

        // Simulate interrupted migration: insert departments and employees, then simulate crash
        var depts = await reader.ReadDepartmentsAsync();
        foreach (var d in depts)
        {
            var deptId = DeterministicGuid.Create(DeterministicGuid.DepartmentNamespace, $"Dept_{d.MaPhongBan}");
            var dept = HRM.Next.Domain.Departments.Department.Create(d.TenPhongBan, DateTimeOffset.UtcNow);
            typeof(HRM.Next.Domain.Departments.Department).GetProperty("Id",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!
                .SetValue(dept, deptId);
            nextDb.Departments.Add(dept);
        }
        await nextDb.SaveChangesAsync();

        // Now run full migration engine
        var engine = new MigrationEngine(reader, nextDb);
        var ledger = await engine.ExecuteMigrationAsync();

        // Verify all entities completed without duplicate departments or errors
        Assert.Equal(3, await nextDb.Departments.CountAsync());
        Assert.Equal(5, await nextDb.EmployeeProfiles.CountAsync());
        Assert.Equal(4, await nextDb.Payslips.CountAsync());
    }
}
