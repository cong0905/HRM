namespace HRM.Next.Domain.Audit;

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorUsername { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Payload { get; set; }
}

