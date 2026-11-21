using System.ComponentModel.DataAnnotations;

namespace HRM.Next.Application.Payroll;

public sealed record PayslipResponse(
    Guid Id,
    Guid PeriodId,
    Guid EmployeeId,
    decimal Amount,
    string Status,
    DateTimeOffset? PublishedAtUtc);

public sealed record PayrollPeriodResponse(
    Guid Id,
    string PeriodCode,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record PayrollActionRequest(
    [Required, StringLength(500)] string Reason, string? RowVersion=null, int? RunVersion=null);

