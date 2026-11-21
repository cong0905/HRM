namespace HRM.Next.Domain.Payroll;

public sealed class SalaryHistoryEntry
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public decimal MonthlyBase { get; set; }
    public string Reason { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class PayrollManualAdjustment
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public string Kind { get; set; } = "";
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public string? VoidReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class PayrollCalculationBatch
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public int Version { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string InputHash { get; set; } = "";
}
public sealed class PayrollCalculationSnapshot
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid EmployeeId { get; set; }
    public string InputJson { get; set; } = "";
    public string SourceJson { get; set; } = "";
    public string OutputJson { get; set; } = "";
    public string InputHash { get; set; } = "";
    public decimal Net { get; set; }
    public bool CanPublish { get; set; }
}
public sealed class PayrollInputReceipt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Operation { get; set; } = "";
    public Guid Key { get; set; }
    public string RequestHash { get; set; } = "";
    public string ResponseJson { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
}
