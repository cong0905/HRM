using HRM.Next.Application.Audit;
using HRM.Next.Application.Common;
using HRM.Next.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Persistence;

public sealed class AuditRepository(HrmDbContext db) : IAuditRepository
{
    public async Task RecordAsync(AuditEvent entry, CancellationToken token)
    {
        entry.Reason = AuditMasker.SanitizeFreeText(entry.Reason);
        entry.Payload = AuditMasker.MaskJson(entry.Payload);
        db.AuditEvents.Add(entry);
        await db.SaveChangesAsync(token);
    }

    public async Task<PagedResult<AuditEventResponse>> ListAsync(AuditQueryParameters parameters, CancellationToken token)
    {
        var page = parameters.Page < 1 ? 1 : parameters.Page;
        var pageSize = parameters.PageSize is < 1 or > 100 ? 20 : parameters.PageSize;

        IQueryable<AuditEvent> query = db.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Action))
            query = query.Where(a => a.Action == parameters.Action.Trim());

        if (parameters.ActorId.HasValue)
            query = query.Where(a => a.ActorId == parameters.ActorId.Value);

        if (parameters.FromUtc.HasValue)
            query = query.Where(a => a.OccurredAtUtc >= parameters.FromUtc.Value);

        if (parameters.ToUtc.HasValue)
            query = query.Where(a => a.OccurredAtUtc <= parameters.ToUtc.Value);

        query = parameters.Sort switch
        {
            "occurredAt" => query.OrderBy(a => a.OccurredAtUtc).ThenBy(a => a.Id),
            _ => query.OrderByDescending(a => a.OccurredAtUtc).ThenByDescending(a => a.Id)
        };

        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;
        var rawItems = await query.Skip(offset).Take(pageSize).ToListAsync(token);
        var items = rawItems.Select(a => new AuditEventResponse(
            a.Id,
            a.OccurredAtUtc,
            a.ActorId,
            a.ActorUsername,
            a.TraceId,
            a.Action,
            a.EntityName,
            a.EntityId,
            AuditMasker.SanitizeFreeText(a.Reason),
            AuditMasker.MaskJson(a.Payload)))
            .ToList();

        return new PagedResult<AuditEventResponse>(items, page, pageSize, total);
    }
}

