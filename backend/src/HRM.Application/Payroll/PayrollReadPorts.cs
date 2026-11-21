using HRM.Next.Domain.Payroll;

namespace HRM.Next.Application.Payroll;

public sealed record PayslipDetailResponse(Guid Id, Guid PeriodId, string PeriodCode, string PeriodStatus,
    Guid EmployeeId, string Status, DateTimeOffset? PublishedAtUtc, Guid SnapshotId, Guid BatchId,
    int RunVersion, string Currency, PayrollPolicy Policy, PayrollCalculationOutput Calculation,
    IReadOnlyList<PayrollDayInput> Days, IReadOnlyList<PayrollCalendarDay> Calendar, string Notice);
public sealed record PayrollReconciliationResponse(Guid PeriodId, string PeriodCode, string Status,
    Guid BatchId, int RunVersion, long PayslipCount, decimal PayslipNet, decimal SnapshotNet,
    decimal Difference, string Currency);
public sealed record PayrollCsvFile(byte[] Content, string FileName);
public interface IPayrollReadService
{
    Task<PayslipDetailResponse> DetailAsync(Guid id, bool ownOnly, CancellationToken token);
    Task<PayrollReconciliationResponse> ReconcileAsync(Guid periodId, CancellationToken token);
    Task<PayrollCsvFile> ExportAsync(Guid periodId, int maxRows, CancellationToken token);
}
