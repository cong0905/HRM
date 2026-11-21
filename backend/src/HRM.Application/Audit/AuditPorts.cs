using HRM.Next.Application.Common;
using HRM.Next.Domain.Audit;

namespace HRM.Next.Application.Audit;

public sealed record AuditEventResponse(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    Guid? ActorId,
    string? ActorUsername,
    string TraceId,
    string Action,
    string EntityName,
    string EntityId,
    string Reason,
    string? Payload);

public sealed record AuditQueryParameters(
    int Page = 1,
    int PageSize = 20,
    string? Action = null,
    Guid? ActorId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    string Sort = "-occurredAt");

public interface IAuditRepository
{
    Task RecordAsync(AuditEvent entry, CancellationToken token);
    Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters parameters, CancellationToken token);
}

