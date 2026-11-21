using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using HRM.Next.Domain.Payroll;

namespace HRM.Next.Application.Payroll;

public sealed class PayrollAccessDeniedException() : Exception("Payroll permission required.") { }
public sealed class PayrollNotFoundException(string code) : Exception("Payroll resource not found.")
{ public string Code { get; }=code; }

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AppendSalaryRequest(DateOnly EffectiveFrom,
    [Range(typeof(decimal),"0.01","1000000000000")] decimal MonthlyBase,
    [Required,StringLength(500,MinimumLength=10)] string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record AddPayrollAdjustmentRequest(Guid EmployeeId,[Required] string Kind,
    [Range(typeof(decimal),"0.01","1000000000000")] decimal Amount,
    [Required,StringLength(500,MinimumLength=10)] string Reason);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record VoidPayrollAdjustmentRequest([Required,StringLength(500,MinimumLength=10)] string Reason,
    [Required] string RowVersion);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CapturePayrollRequest([Required,StringLength(500,MinimumLength=10)] string Reason);
public sealed record SalaryHistoryResponse(Guid Id,Guid EmployeeId,DateOnly EffectiveFrom,DateOnly? EffectiveTo,
    decimal MonthlyBase,string Reason,DateTimeOffset CreatedAtUtc,string RowVersion);
public sealed record PayrollAdjustmentResponse(Guid Id,Guid PeriodId,Guid EmployeeId,string Kind,decimal Amount,
    string Reason,DateTimeOffset? VoidedAtUtc,string? VoidReason,string RowVersion);
public sealed record PayrollSnapshotResponse(Guid Id,Guid EmployeeId,PayrollCalculationInput Input,
    PayrollCalculationOutput Output,string InputHash);
public sealed record PayrollBatchResponse(Guid Id,Guid PeriodId,int Version,DateTimeOffset CapturedAtUtc,
    string InputHash,IReadOnlyList<PayrollSnapshotResponse> Snapshots);

public interface IPayrollCalculationService
{
    Task<SalaryHistoryResponse> AppendSalaryAsync(Guid employeeId,AppendSalaryRequest request,string key,CancellationToken token);
    Task<IReadOnlyList<SalaryHistoryResponse>> SalaryHistoryAsync(Guid employeeId,CancellationToken token);
    Task<PayrollAdjustmentResponse> AddAdjustmentAsync(Guid periodId,AddPayrollAdjustmentRequest request,string key,CancellationToken token);
    Task<PayrollAdjustmentResponse> VoidAdjustmentAsync(Guid id,VoidPayrollAdjustmentRequest request,string key,CancellationToken token);
    Task<IReadOnlyList<PayrollAdjustmentResponse>> AdjustmentsAsync(Guid periodId,CancellationToken token);
    Task<PayrollBatchResponse> CaptureAsync(Guid periodId,CapturePayrollRequest request,string? key,CancellationToken token);
    Task<PayrollBatchResponse?> ReadBatchAsync(Guid periodId,int? version,CancellationToken token);
    Task<bool> RequiresSnapshotsAsync(Guid periodId,CancellationToken token);
    Task EnsurePublishableAsync(Guid periodId,CancellationToken token);
}
