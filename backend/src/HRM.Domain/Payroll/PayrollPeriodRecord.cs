namespace HRM.Next.Domain.Payroll;

public sealed class PayrollPeriodRecord
{
    public Guid Id { get; set; }
    public string PeriodCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CalculatedBy { get; set; }
    public DateTimeOffset? CalculatedAtUtc { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public Guid? ActiveBatchId { get; set; }
    public Guid? ApprovedBatchId { get; set; }
    public Guid? PublishedBatchId { get; set; }
    public Guid? LockedBy { get; set; }
    public DateTimeOffset? LockedAtUtc { get; set; }
    public bool SnapshotRequired { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

