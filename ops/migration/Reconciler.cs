using System.Text;
using System.Text.Json;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Ops.Migration;

public sealed class Reconciler(LegacyDataReader reader, HrmDbContext db)
{
    public async Task<ReconciliationReport> ReconcileAsync(
        string sourceDbName,
        string targetDbName,
        CancellationToken token = default)
    {
        var report = new ReconciliationReport
        {
            SourceDatabase = sourceDbName,
            TargetDatabase = targetDbName
        };

        // 1. Read legacy data
        var legDepts = await reader.ReadDepartmentsAsync(token);
        var legPositions = await reader.ReadPositionsAsync(token);
        var legEmployees = await reader.ReadEmployeesAsync(token);
        var legAccounts = await reader.ReadAccountsAsync(token);
        var legAttendances = await reader.ReadAttendanceAsync(token);
        var legLeaves = await reader.ReadLeaveRequestsAsync(token);
        var legBalances = await reader.ReadLeaveBalancesAsync(token);
        var legPayrolls = await reader.ReadPayrollAsync(token);

        // Pre-validate to know expected excluded counts
        var validator = new MigrationValidator(reader);
        var dryRun = await validator.ValidateAsync(sourceDbName, targetDbName, token);

        // 2. Query target counts
        int nextDeptCount = await db.Departments.CountAsync(token);
        int nextPosCount = await db.Positions.CountAsync(token);
        int nextEmpCount = await db.EmployeeProfiles.CountAsync(token);
        int nextUserCount = await db.Users.CountAsync(token);
        int nextAttCount = await db.AttendanceSummaries.CountAsync(token);
        int nextLeaveCount = await db.LeaveRecords.CountAsync(token);
        int nextPayslipCount = await db.Payslips.CountAsync(token);

        AddTableComparison(report, "Departments", legDepts.Count, nextDeptCount, dryRun.ExcludedTableCounts.GetValueOrDefault("PhongBan", 0));
        AddTableComparison(report, "Positions", legPositions.Count, nextPosCount, dryRun.ExcludedTableCounts.GetValueOrDefault("ChucVu", 0));
        AddTableComparison(report, "Employees", legEmployees.Count, nextEmpCount, dryRun.ExcludedTableCounts.GetValueOrDefault("NhanVien", 0));
        AddTableComparison(report, "Accounts", legAccounts.Count, nextUserCount, dryRun.ExcludedTableCounts.GetValueOrDefault("TaiKhoan", 0));
        AddTableComparison(report, "Attendance", legAttendances.Count, nextAttCount, dryRun.ExcludedTableCounts.GetValueOrDefault("ChamCong", 0));
        AddTableComparison(report, "LeaveRecords", legLeaves.Count, nextLeaveCount, dryRun.ExcludedTableCounts.GetValueOrDefault("DonNghiPhep", 0));
        AddTableComparison(report, "Payslips", legPayrolls.Count, nextPayslipCount, dryRun.ExcludedTableCounts.GetValueOrDefault("BangLuong", 0));

        // 3. Financial Reconciliation per Period
        var legacyPeriods = legPayrolls.GroupBy(p => $"{p.Nam:D4}-{p.Thang:D2}");
        foreach (var group in legacyPeriods.OrderBy(g => g.Key))
        {
            var periodCode = group.Key;
            decimal legacyNetTotal = group.Sum(p => p.LuongThucNhan ?? 0m);
            int legacyPeriodCount = group.Count();

            var nextPeriod = await db.PayrollPeriods.FirstOrDefaultAsync(p => p.PeriodCode == periodCode, token);
            decimal nextNetTotal = 0m;
            int nextPeriodCount = 0;

            if (nextPeriod is not null)
            {
                var payslips = await db.Payslips.Where(p => p.PeriodId == nextPeriod.Id).ToListAsync(token);
                nextPeriodCount = payslips.Count;
                nextNetTotal = payslips.Sum(p => p.Amount);
            }

            decimal discrepancy = Math.Abs(legacyNetTotal - nextNetTotal);
            const decimal tolerance = 0.00m; // Strict 0-tolerance for rehearsal financial matching
            bool matched = (discrepancy <= tolerance) && (legacyPeriodCount == nextPeriodCount);

            report.PayrollPeriods.Add(new PayrollPeriodReconciliation(
                PeriodCode: periodCode,
                LegacyRecordCount: legacyPeriodCount,
                NextRecordCount: nextPeriodCount,
                LegacyNetTotal: legacyNetTotal,
                NextNetTotal: nextNetTotal,
                NetDiscrepancy: discrepancy,
                Tolerance: tolerance,
                Matched: matched
            ));
        }

        // 4. Leave Balance Reconciliation
        decimal totalAllocatedLeg = legBalances.Sum(b => (decimal)b.TongSoNgayPhep);
        decimal totalUsedLeg = legBalances.Sum(b => (decimal)b.SoNgayDaSuDung);

        var nextBalances = await db.LeaveBalances.ToListAsync(token);
        decimal totalAllocatedNext = nextBalances.Sum(b => b.AllocatedDays);
        decimal totalUsedNext = nextBalances.Sum(b => b.UsedDays);

        report.LeaveBalances = new LeaveBalanceReconciliation(
            TotalEmployeesWithBalance: nextBalances.Count,
            TotalAllocatedDaysLegacy: totalAllocatedLeg,
            TotalAllocatedDaysNext: totalAllocatedNext,
            TotalUsedDaysLegacy: totalUsedLeg,
            TotalUsedDaysNext: totalUsedNext,
            Matched: (totalAllocatedLeg == totalAllocatedNext) && (totalUsedLeg == totalUsedNext)
        );

        foreach (var issue in dryRun.Issues.Where(i => i.Severity == IssueSeverity.Error))
        {
            report.ExcludedLegacyIdsWithReason.Add($"[{issue.Table}] Record #{issue.RecordId}: {issue.RuleCode} - {issue.Message}");
        }

        return report;
    }

    private static void AddTableComparison(ReconciliationReport report, string tableName, int legacyCount, int nextCount, int excludedCount)
    {
        int expectedNext = legacyCount - excludedCount;
        int discrepancy = Math.Abs(expectedNext - nextCount);
        bool matched = discrepancy == 0;

        report.Tables.Add(new TableReconciliation(
            TableName: tableName,
            LegacyCount: legacyCount,
            NextCount: nextCount,
            ExcludedCount: excludedCount,
            Discrepancy: discrepancy,
            Matched: matched
        ));
    }

    public static string GenerateMarkdownReport(ReconciliationReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Báo Cáo Đối Soát Dữ Liệu Di Chuyển (Migration Reconciliation Report)");
        sb.AppendLine();
        sb.AppendLine($"- **Thời gian thực hiện**: `{report.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss} UTC`");
        sb.AppendLine($"- **Cơ sở dữ liệu nguồn (Legacy)**: `{report.SourceDatabase}`");
        sb.AppendLine($"- **Cơ sở dữ liệu đích (Next)**: `{report.TargetDatabase}`");
        sb.AppendLine($"- **Kết quả tổng thể**: {(report.Passed ? "**PASSED (100% Khớp)**" : "**FAILED (Có chênh lệch)**")}");
        sb.AppendLine();

        sb.AppendLine("## 1. Đối Soát Số Lượng Bản Ghi Theo Bảng (Row Counts)");
        sb.AppendLine();
        sb.AppendLine("| Bảng / Thực thể | Nguồn (Legacy) | Đã loại bỏ (Excluded) | Kỳ vọng đích | Thực tế đích (Next) | Chênh lệch | Trạng thái |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var t in report.Tables)
        {
            int expected = t.LegacyCount - t.ExcludedCount;
            string status = t.Matched ? " khớp" : " LỆCH";
            sb.AppendLine($"| {t.TableName} | {t.LegacyCount:N0} | {t.ExcludedCount:N0} | {expected:N0} | {t.NextCount:N0} | {t.Discrepancy:N0} | {status} |");
        }
        sb.AppendLine();

        sb.AppendLine("## 2. Đối Soát Tài Chính Kỳ Lương (Payroll Financial Reconciliation)");
        sb.AppendLine();
        sb.AppendLine("| Kỳ lương | Số phiếu Legacy | Số phiếu Next | Tổng thực nhận Legacy (VND) | Tổng thực nhận Next (VND) | Chênh lệch | Dung sai cho phép | Kết quả |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var p in report.PayrollPeriods)
        {
            string status = p.Matched ? " Khớp" : " LỆCH";
            sb.AppendLine($"| {p.PeriodCode} | {p.LegacyRecordCount} | {p.NextRecordCount} | {p.LegacyNetTotal:N2} | {p.NextNetTotal:N2} | {p.NetDiscrepancy:N2} | {p.Tolerance:N2} | {status} |");
        }
        sb.AppendLine();

        if (report.LeaveBalances is not null)
        {
            var lb = report.LeaveBalances;
            sb.AppendLine("## 3. Đối Soát Quỹ Phép (Leave Balances)");
            sb.AppendLine();
            sb.AppendLine($"- **Tổng nhân viên có số dư**: `{lb.TotalEmployeesWithBalance}`");
            sb.AppendLine($"- **Tổng ngày phép cấp (Allocated)**: Legacy = `{lb.TotalAllocatedDaysLegacy:N1}`, Next = `{lb.TotalAllocatedDaysNext:N1}` (Khớp: {lb.TotalAllocatedDaysLegacy == lb.TotalAllocatedDaysNext})");
            sb.AppendLine($"- **Tổng ngày phép đã dùng (Used)**: Legacy = `{lb.TotalUsedDaysLegacy:N1}`, Next = `{lb.TotalUsedDaysNext:N1}` (Khớp: {lb.TotalUsedDaysLegacy == lb.TotalUsedDaysNext})");
            sb.AppendLine();
        }

        if (report.ExcludedLegacyIdsWithReason.Count > 0)
        {
            sb.AppendLine("## 4. Danh Sách Bản Ghi Legacy Bị Loại Bỏ (Exclusion Audit)");
            sb.AppendLine();
            foreach (var exc in report.ExcludedLegacyIdsWithReason)
            {
                sb.AppendLine($"- {exc}");
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("## 4. Danh Sách Bản Ghi Legacy Bị Loại Bỏ (Exclusion Audit)");
            sb.AppendLine();
            sb.AppendLine("*(Không có bản ghi nào bị loại bỏ. Toàn bộ dữ liệu sạch 100% đã được chuyển đổi thành công)*");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
