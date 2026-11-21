using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Common;
using HRM.Next.Application.Payroll;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Payroll;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Payroll;

public sealed class PayrollReadService(HrmDbContext db, ISecurityContext security,
    IAuditRepository audit, TimeProvider clock) : IPayrollReadService
{
    private void Require(string permission)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue || !security.HasPermission(permission))
            throw new PayrollAccessDeniedException();
    }

    private async Task<PayrollPeriodRecord> PeriodAsync(Guid id, CancellationToken token) =>
        await db.PayrollPeriods.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token)
        ?? throw new PayrollNotFoundException("payroll.period_not_found");

    private async Task<PayrollCalculationBatch> RunAsync(PayrollPeriodRecord period, CancellationToken token)
    {
        var closed = period.Status is "Published" or "Locked";
        var id = closed ? period.PublishedBatchId : period.ActiveBatchId;
        if (!id.HasValue || (closed && id != period.ActiveBatchId))
            throw new ResourceConflictException("payroll.snapshot_missing", "A versioned payroll run is required.");
        return await db.PayrollCalculationBatches.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.PeriodId == period.Id, token)
            ?? throw new ResourceConflictException("payroll.snapshot_missing", "Payroll run unavailable.");
    }

    // The shared source lock serializes these multi-query projections with calculate/publish.
    // It changes lock metadata only: detail and reconciliation never SaveChanges or write rows.
    public async Task<PayslipDetailResponse> DetailAsync(Guid id, bool ownOnly, CancellationToken token)
    {
        Require(ownOnly ? HrmPermissions.PayrollReadOwn : HrmPermissions.PayrollReadAll);
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await PayrollDataLocks.SourceAsync(db, false, token);
        var query = db.Payslips.AsNoTracking().Where(x => x.Id == id);
        if (ownOnly)
            query = query.Where(x => x.Status == "Published" && db.EmployeeProfiles.Any(e =>
                e.Id == x.EmployeeId && e.UserId == security.CurrentUserId));
        var slip = await query.SingleOrDefaultAsync(token)
            ?? throw new PayrollNotFoundException("payroll.payslip_not_found");
        var period = await PeriodAsync(slip.PeriodId, token);
        if (ownOnly && period.Status is not ("Published" or "Locked"))
            throw new PayrollNotFoundException("payroll.payslip_not_found");
        var run = await RunAsync(period, token);
        var snapshot = await db.PayrollCalculationSnapshots.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == slip.SnapshotId && x.BatchId == run.Id && x.EmployeeId == slip.EmployeeId, token);
        if (snapshot is null || snapshot.Net != slip.Amount)
            throw new ResourceConflictException("payroll.run_items_mismatch", "Payslip and snapshot differ.");
        var input = JsonSerializer.Deserialize<PayrollCalculationInput>(snapshot.InputJson)!;
        var output = JsonSerializer.Deserialize<PayrollCalculationOutput>(snapshot.OutputJson)!;
        if (output.Net != snapshot.Net || output.Lines.Sum(x => x.Amount) != output.Net)
            throw new ResourceConflictException("payroll.run_items_mismatch", "Snapshot totals differ.");
        var result = new PayslipDetailResponse(slip.Id, period.Id, period.PeriodCode, period.Status,
            slip.EmployeeId, slip.Status, slip.PublishedAtUtc, snapshot.Id, run.Id, run.Version,
            "VND", input.Policy, output, input.Days, input.Calendar,
            "Chính sách demo; thuế và bảo hiểm là khoản nhập thủ công, không tự tính theo luật.");
        await tx.CommitAsync(token);
        return result;
    }

    private async Task<PayrollReconciliationResponse> ReconcileCoreAsync(PayrollPeriodRecord period,
        PayrollCalculationBatch run, CancellationToken token)
    {
        var slips = db.Payslips.AsNoTracking().Where(x => x.PeriodId == period.Id);
        var snapshots = db.PayrollCalculationSnapshots.AsNoTracking().Where(x => x.BatchId == run.Id);
        var count = await slips.LongCountAsync(token);
        var snapshotCount = await snapshots.LongCountAsync(token);
        var wrong = await slips.AnyAsync(s => !snapshots.Any(x => x.Id == s.SnapshotId &&
            x.EmployeeId == s.EmployeeId && x.Net == s.Amount), token);
        if (count == 0 || count != snapshotCount || wrong ||
            (period.Status is "Published" or "Locked" && await slips.AnyAsync(x => x.Status != "Published", token)))
            throw new ResourceConflictException("payroll.run_items_mismatch", "Cannot reconcile this run.");
        var total = await slips.SumAsync(x => x.Amount, token);
        var snapshotTotal = await snapshots.SumAsync(x => x.Net, token);
        return new(period.Id, period.PeriodCode, period.Status, run.Id, run.Version, count,
            total, snapshotTotal, total - snapshotTotal, "VND");
    }

    public async Task<PayrollReconciliationResponse> ReconcileAsync(Guid periodId, CancellationToken token)
    {
        Require(HrmPermissions.PayrollReadAll);
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await PayrollDataLocks.SourceAsync(db, false, token);
        var period = await PeriodAsync(periodId, token);
        var result = await ReconcileCoreAsync(period, await RunAsync(period, token), token);
        await tx.CommitAsync(token);
        return result;
    }

    public async Task<PayrollCsvFile> ExportAsync(Guid periodId, int maxRows, CancellationToken token)
    {
        Require(HrmPermissions.PayrollExport);
        if (maxRows is < 1 or > 1000)
            throw new RequestValidationException("payroll.invalid_export_limit", "maxRows must be 1..1000.");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        await PayrollDataLocks.SourceAsync(db, false, token);
        var period = await PeriodAsync(periodId, token);
        if (period.Status is not ("Published" or "Locked"))
            throw new ResourceConflictException("payroll.period_not_published", "Only published payroll may be exported.");
        var run = await RunAsync(period, token);
        var summary = await ReconcileCoreAsync(period, run, token);
        if (summary.PayslipCount > maxRows)
            throw new ResourceConflictException("payroll.export_limit_exceeded", "Use a smaller period/export workflow; no partial file is returned.");
        var rows = await (from slip in db.Payslips.AsNoTracking()
                          join employee in db.EmployeeProfiles.AsNoTracking() on slip.EmployeeId equals employee.Id
                          join snapshot in db.PayrollCalculationSnapshots.AsNoTracking() on slip.SnapshotId equals (Guid?)snapshot.Id
                          where slip.PeriodId == periodId && snapshot.BatchId == run.Id
                          orderby slip.EmployeeId
                          select new { slip.Id, employee.EmployeeCode, employee.FullName, slip.Status,
                              SnapshotId = snapshot.Id, snapshot.OutputJson, slip.Amount }).Take(maxRows).ToListAsync(token);
        if (rows.Count != summary.PayslipCount)
            throw new ResourceConflictException("payroll.run_items_mismatch", "Missing export identities.");
        var csv = new StringBuilder("PayslipId,EmployeeCode,FullName,PeriodCode,RunVersion,StandardPay,OvertimePay,Allowance,Bonus,OtherDeduction,DemoInsurance,DemoTax,RoundingAdjustment,Gross,Net,Status,SnapshotId\r\n");
        foreach (var row in rows)
        {
            var output = JsonSerializer.Deserialize<PayrollCalculationOutput>(row.OutputJson)!;
            if (output.Net != row.Amount || output.Lines.Sum(x => x.Amount) != output.Net)
                throw new ResourceConflictException("payroll.run_items_mismatch", "Snapshot output does not reconcile.");
            decimal Line(string code) => output.Lines.Where(x => x.Code == code).Sum(x => x.Amount);
            csv.AppendJoin(',', CsvText(row.Id.ToString()), CsvText(row.EmployeeCode), CsvText(row.FullName),
                CsvText(period.PeriodCode), run.Version.ToString(CultureInfo.InvariantCulture),
                Money(output.StandardPay), Money(output.OvertimePay), Money(Line("Allowance")), Money(Line("Bonus")),
                Money(Line("OtherDeduction")), Money(Line("DemoInsurance")), Money(Line("DemoTax")),
                Money(output.RoundingAdjustment), Money(output.Gross), Money(output.Net),
                CsvText(row.Status), CsvText(row.SnapshotId.ToString())).Append("\r\n");
        }
        var encoding = new UTF8Encoding(true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(csv.ToString())).ToArray();
        await audit.RecordAsync(new AuditEvent
        {
            Id = Guid.NewGuid(), OccurredAtUtc = clock.GetUtcNow(), ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername, TraceId = Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Action = "payroll.exported", EntityName = "PayrollPeriod", EntityId = periodId.ToString(),
            Reason = "Xuất CSV bảng lương đã công bố",
            Payload = AuditMasker.MaskObject(new { periodId, batchId = run.Id, runVersion = run.Version,
                exportedRows = rows.Count, format = "csv-utf8-bom" })
        }, token);
        await tx.CommitAsync(token);
        return new(bytes, $"payroll-{periodId:N}-v{run.Version}.csv");
    }

    private static string Money(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    public static string CsvText(string value)
    {
        var i = 0;
        while (i < value.Length && (char.IsWhiteSpace(value[i]) || char.IsControl(value[i]) ||
            char.GetUnicodeCategory(value[i]) == UnicodeCategory.Format)) i++;
        if ((i < value.Length && "=+-@".Contains(value[i])) ||
            (value.Length > 0 && "\t\r\n".Contains(value[0]))) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
