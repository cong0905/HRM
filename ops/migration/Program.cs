using System.Text.Json;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Ops.Migration;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
        {
            PrintUsage();
            return 0;
        }

        string command = args[0].ToLowerInvariant();
        string legacyConn = GetArg(args, "--legacy") ?? Environment.GetEnvironmentVariable("HRM_LEGACY_SQLSERVER") ?? "";
        string nextConn = GetArg(args, "--target") ?? Environment.GetEnvironmentVariable("HRM_T05_SQLSERVER") ?? "";
        string? reportPath = GetArg(args, "--report");

        if (string.IsNullOrWhiteSpace(legacyConn) || string.IsNullOrWhiteSpace(nextConn))
        {
            Console.Error.WriteLine("Error: Both --legacy and --target connection strings are required.");
            return 1;
        }

        try
        {
            var reader = new LegacyDataReader(legacyConn);
            var dbOptions = new DbContextOptionsBuilder<HrmDbContext>()
                .UseSqlServer(nextConn, opt => opt.UseCompatibilityLevel(160))
                .Options;

            await using var db = new HrmDbContext(dbOptions);

            switch (command)
            {
                case "dry-run":
                {
                    Console.WriteLine("=== EXECUTING DRY-RUN VALIDATION ===");
                    var validator = new MigrationValidator(reader);
                    var report = await validator.ValidateAsync("LegacyDB", "NextDB");

                    Console.WriteLine($"Total validation issues found: {report.Issues.Count}");
                    foreach (var issue in report.Issues)
                    {
                        var prefix = issue.Severity == IssueSeverity.Error ? "[ERROR]" : "[WARN]";
                        Console.WriteLine($"{prefix} [{issue.Table} #{issue.RecordId}] {issue.RuleCode}: {issue.Message}");
                    }

                    Console.WriteLine(report.CanProceed
                        ? " SUCCESS: Dữ liệu sạch, sẵn sàng di chuyển."
                        : " FAILED: Phát hiện lỗi vi phạm ràng buộc dữ liệu.");

                    if (!string.IsNullOrWhiteSpace(reportPath))
                    {
                        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
                        Console.WriteLine($"Dry-run report written to {reportPath}");
                    }
                    return report.CanProceed ? 0 : 2;
                }

                case "migrate":
                {
                    Console.WriteLine("=== EXECUTING MIGRATION ===");
                    var engine = new MigrationEngine(reader, db);
                    var ledger = await engine.ExecuteMigrationAsync();
                    Console.WriteLine($" Migration completed successfully.");
                    Console.WriteLine($"Migrated: {ledger.Departments.Count} departments, {ledger.Positions.Count} positions, {ledger.Employees.Count} employees, {ledger.Accounts.Count} users/accounts, {ledger.Attendance.Count} attendance, {ledger.LeaveRecords.Count} leave requests, {ledger.PayrollPeriods.Count} payroll periods ({ledger.Payslips.Count} payslips).");
                    return 0;
                }

                case "reconcile":
                {
                    Console.WriteLine("=== EXECUTING DATA RECONCILIATION ===");
                    var reconciler = new Reconciler(reader, db);
                    var report = await reconciler.ReconcileAsync("LegacyDB", "NextDB");

                    string markdown = Reconciler.GenerateMarkdownReport(report);
                    Console.WriteLine(markdown);

                    if (!string.IsNullOrWhiteSpace(reportPath))
                    {
                        await File.WriteAllTextAsync(reportPath, markdown);
                        Console.WriteLine($"Reconciliation report written to {reportPath}");
                    }

                    return report.Passed ? 0 : 3;
                }

                default:
                    Console.Error.WriteLine($"Unknown command: {command}");
                    PrintUsage();
                    return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FATAL: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 99;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(@"HRM Migration Tool (T23)
Usage:
  dotnet run --project ops/migration/HrmMigration.csproj -- <command> [options]

Commands:
  dry-run    Validate legacy data integrity (duplicates, orphans, invariants) without altering any DB.
  migrate    Execute idempotent migration into target DB.
  reconcile  Perform row-count and payroll financial reconciliation between legacy and target DBs.

Options:
  --legacy <conn>   Connection string to legacy database (or env HRM_LEGACY_SQLSERVER)
  --target <conn>   Connection string to target Next database (or env HRM_T05_SQLSERVER)
  --report <path>   Optional output path for report (JSON or Markdown)
");
    }

    private static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }
        return null;
    }
}
