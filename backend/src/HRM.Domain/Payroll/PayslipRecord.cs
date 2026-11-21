namespace HRM.Next.Domain.Payroll;

public sealed class PayslipRecord
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? SnapshotId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTimeOffset? PublishedAtUtc { get; set; }
}

