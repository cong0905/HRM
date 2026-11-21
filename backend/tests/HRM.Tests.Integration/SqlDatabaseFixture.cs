using System.Text.RegularExpressions;
using System.Text.Json;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class SqlDatabaseFixture : IAsyncLifetime
{
    private string _adminConnection = string.Empty;
    private bool _created;
    private string[] _migrations = [];
    public string DatabaseName { get; } = "HRM_Next_Test_T05_" + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; private set; } = string.Empty;
    public string ServerVersion { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("HRM_T05_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
            throw new InvalidOperationException("Set HRM_T05_SQLSERVER explicitly to a test SQL Server connection. Integration tests never fall back to InMemory.");
        var admin = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master", ConnectTimeout = 10 };
        _adminConnection = admin.ConnectionString;
        await using var connection = new SqlConnection(_adminConnection);
        await connection.OpenAsync();
        ServerVersion = connection.ServerVersion;
        await using var create = connection.CreateCommand();
        create.CommandText = $"CREATE DATABASE [{DatabaseName}]";
        await create.ExecuteNonQueryAsync();
        _created = true; // Only this successfully-created database may be dropped by cleanup.
        admin.InitialCatalog = DatabaseName;
        ConnectionString = admin.ConnectionString;
        try
        {
            await using var db = CreateContext();
            await db.Database.MigrateAsync();
            _migrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
            await WriteEvidenceAsync(false);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public HrmDbContext CreateContext() => new(new DbContextOptionsBuilder<HrmDbContext>()
        .UseSqlServer(ConnectionString, sql => sql.UseCompatibilityLevel(160))
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking).Options);

    public async Task ResetAsync()
    {
        await using var db = CreateContext();
        await db.Payslips.ExecuteDeleteAsync();
        await db.PayrollPeriods.ExecuteUpdateAsync(set=>set.SetProperty(x=>x.ActiveBatchId,(Guid?)null).SetProperty(x=>x.ApprovedBatchId,(Guid?)null).SetProperty(x=>x.PublishedBatchId,(Guid?)null));
        await db.PayrollCalculationSnapshots.ExecuteDeleteAsync();
        await db.PayrollCalculationBatches.ExecuteDeleteAsync();
        await db.PayrollInputReceipts.ExecuteDeleteAsync();
        await db.PayrollManualAdjustments.ExecuteDeleteAsync();
        await db.SalaryHistoryEntries.ExecuteDeleteAsync();
        await db.AttendanceIdempotencyRecords.ExecuteDeleteAsync();
        await db.AttendanceAuditLogs.ExecuteDeleteAsync();
        await db.AttendanceEvents.ExecuteDeleteAsync();
        await db.AttendanceOvertimeEntries.ExecuteDeleteAsync();
        await db.AttendanceSummaries.ExecuteDeleteAsync();
        await db.AuditEvents.ExecuteDeleteAsync();
        await db.ActivationOutboxMessages.ExecuteDeleteAsync();
        await db.EmailOutboxMessages.ExecuteDeleteAsync();
        await db.CalendarPeriodSnapshots.ExecuteDeleteAsync();
        await db.WorkCalendarVersions.ExecuteDeleteAsync();
        await db.Payslips.ExecuteDeleteAsync();
        await db.PayrollPeriods.ExecuteDeleteAsync();
        await db.HrEmployeeAssignments.ExecuteDeleteAsync();
        await db.LeaveIdempotencyRecords.ExecuteDeleteAsync();
        await db.LeaveLedgerEntries.ExecuteDeleteAsync();
        await db.LeaveRecords.ExecuteDeleteAsync();
        await db.LeaveBalances.ExecuteDeleteAsync();
        await db.EmployeeProfiles.ExecuteDeleteAsync();
        await db.Positions.ExecuteDeleteAsync();
        await db.Departments.ExecuteDeleteAsync();
    }

    public async Task DisposeAsync()
    {
        if (!_created) return;
        if (!Regex.IsMatch(DatabaseName, "^HRM_Next_Test_T05_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Refusing cleanup of an unowned database name.");
        using (var pooled = new SqlConnection(ConnectionString)) SqlConnection.ClearPool(pooled);
        await using var connection = new SqlConnection(_adminConnection);
        await connection.OpenAsync();
        await using var drop = connection.CreateCommand();
        drop.CommandText = $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}];";
        await drop.ExecuteNonQueryAsync();
        _created = false;
        await WriteEvidenceAsync(true);
    }

    private Task WriteEvidenceAsync(bool cleanupCompleted)
    {
        var path = Environment.GetEnvironmentVariable("HRM_T05_SQL_LEDGER");
        if (string.IsNullOrWhiteSpace(path)) return Task.CompletedTask;
        return File.WriteAllTextAsync(path, JsonSerializer.Serialize(new
        {
            serverVersion = ServerVersion, databaseName = DatabaseName, createdByFixture = true,
            appliedMigrations = _migrations, cleanupCompleted
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
