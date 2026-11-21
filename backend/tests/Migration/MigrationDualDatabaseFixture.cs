using System.Text.RegularExpressions;
using HRM.Next.Infrastructure.Persistence;
using HRM.Ops.Migration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration.Migration;

public sealed class MigrationDualDatabaseFixture : IAsyncLifetime
{
    private string _adminConnection = string.Empty;
    private bool _legacyCreated;
    private bool _nextCreated;

    public string LegacyDatabaseName { get; } = "HRM_Legacy_Rehearsal_" + Guid.NewGuid().ToString("N");
    public string NextDatabaseName { get; } = "HRM_Next_Rehearsal_" + Guid.NewGuid().ToString("N");

    public string LegacyConnectionString { get; private set; } = string.Empty;
    public string NextConnectionString { get; private set; } = string.Empty;

    public static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }

    public static string FixturesDir => Path.Combine(FindRepoRoot(), "ops", "migration", "fixtures");

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("HRM_T05_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set HRM_T05_SQLSERVER explicitly to a test SQL Server connection.");

        var admin = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master", ConnectTimeout = 10 };
        _adminConnection = admin.ConnectionString;

        await using var connection = new SqlConnection(_adminConnection);
        await connection.OpenAsync();

        // 1. Create Legacy Database
        await using var cmdLegacy = connection.CreateCommand();
        cmdLegacy.CommandText = $"CREATE DATABASE [{LegacyDatabaseName}]";
        await cmdLegacy.ExecuteNonQueryAsync();
        _legacyCreated = true;

        admin.InitialCatalog = LegacyDatabaseName;
        LegacyConnectionString = admin.ConnectionString;

        // 2. Create Next Database
        await using var cmdNext = connection.CreateCommand();
        cmdNext.CommandText = $"CREATE DATABASE [{NextDatabaseName}]";
        await cmdNext.ExecuteNonQueryAsync();
        _nextCreated = true;

        admin.InitialCatalog = NextDatabaseName;
        NextConnectionString = admin.ConnectionString;

        // Apply Next EF Core Migrations
        await using var nextDb = CreateNextContext();
        await nextDb.Database.MigrateAsync();
    }

    public HrmDbContext CreateNextContext() => new(new DbContextOptionsBuilder<HrmDbContext>()
        .UseSqlServer(NextConnectionString, sql => sql.UseCompatibilityLevel(160))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);

    public LegacyDataReader CreateLegacyReader() => new(LegacyConnectionString);

    public async Task ExecuteSqlScriptOnLegacyAsync(string scriptContent)
    {
        await using var conn = new SqlConnection(LegacyConnectionString);
        await conn.OpenAsync();

        // Split by GO statements
        var batches = Regex.Split(scriptContent, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch)) continue;
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = batch;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async Task ResetNextDatabaseAsync()
    {
        await using var db = CreateNextContext();
        await db.Payslips.ExecuteDeleteAsync();
        await db.PayrollPeriods.ExecuteUpdateAsync(s => s.SetProperty(x => x.ActiveBatchId, (Guid?)null)
            .SetProperty(x => x.ApprovedBatchId, (Guid?)null).SetProperty(x => x.PublishedBatchId, (Guid?)null));
        await db.PayrollCalculationSnapshots.ExecuteDeleteAsync();
        await db.PayrollCalculationBatches.ExecuteDeleteAsync();
        await db.PayrollPeriods.ExecuteDeleteAsync();
        await db.PayrollInputReceipts.ExecuteDeleteAsync();
        await db.PayrollManualAdjustments.ExecuteDeleteAsync();
        await db.SalaryHistoryEntries.ExecuteDeleteAsync();
        await db.AttendanceEvents.ExecuteDeleteAsync();
        await db.AttendanceAuditLogs.ExecuteDeleteAsync();
        await db.AttendanceOvertimeEntries.ExecuteDeleteAsync();
        await db.AttendanceSummaries.ExecuteDeleteAsync();
        await db.LeaveLedgerEntries.ExecuteDeleteAsync();
        await db.LeaveRecords.ExecuteDeleteAsync();
        await db.LeaveBalances.ExecuteDeleteAsync();
        await db.ActivationOutboxMessages.ExecuteDeleteAsync();
        await db.EmailOutboxMessages.ExecuteDeleteAsync();
        await db.EmployeeProfiles.ExecuteDeleteAsync();
        await db.Positions.ExecuteDeleteAsync();
        await db.Departments.ExecuteDeleteAsync();
        await db.UserRoles.ExecuteDeleteAsync();
        await db.Users.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        await using var connection = new SqlConnection(_adminConnection);
        await connection.OpenAsync();

        if (_legacyCreated)
        {
            using (var pooled = new SqlConnection(LegacyConnectionString)) SqlConnection.ClearPool(pooled);
            await using var drop = connection.CreateCommand();
            drop.CommandText = $"ALTER DATABASE [{LegacyDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{LegacyDatabaseName}];";
            await drop.ExecuteNonQueryAsync();
            _legacyCreated = false;
        }

        if (_nextCreated)
        {
            using (var pooled = new SqlConnection(NextConnectionString)) SqlConnection.ClearPool(pooled);
            await using var drop = connection.CreateCommand();
            drop.CommandText = $"ALTER DATABASE [{NextDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{NextDatabaseName}];";
            await drop.ExecuteNonQueryAsync();
            _nextCreated = false;
        }
    }
}
