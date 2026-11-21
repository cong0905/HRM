using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HRM.Next.Application.Payroll;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record PayrollWorkflowRequest([Required,StringLength(500,MinimumLength=10)]string Reason,
    [Required]string RowVersion,[Range(0,int.MaxValue)]int RunVersion);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreatePayrollPeriodRequest([Range(2000,2100)]int Year,[Range(1,12)]int Month,
    [Required,StringLength(500,MinimumLength=10)]string Reason);
public sealed record PayrollWorkflowState(Guid Id,string PeriodCode,string Status,string RowVersion,int RunVersion,
    Guid? ActiveBatchId,Guid? ApprovedBatchId,Guid? PublishedBatchId,Guid? CalculatedBy,Guid? ApprovedBy,
    Guid? PublishedBy,DateTimeOffset? LockedAtUtc);
public interface IPayrollWorkflowService
{
    Task<PayrollWorkflowState> CreateAsync(CreatePayrollPeriodRequest request,string key,CancellationToken token);
    Task<PayrollWorkflowState> StateAsync(Guid id,CancellationToken token);
    Task ExecuteAsync(Guid id,string action,PayrollWorkflowRequest request,string key,CancellationToken token);
}
