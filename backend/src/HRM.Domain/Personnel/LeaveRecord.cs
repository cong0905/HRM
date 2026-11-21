namespace HRM.Next.Domain.Personnel;

public sealed class LeaveRecord
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal WorkingDays { get; set; } = 0m;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public Guid? ApproverId { get; set; }
    public string? DecisionReason { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
