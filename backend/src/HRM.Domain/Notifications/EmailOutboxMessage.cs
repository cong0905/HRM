namespace HRM.Next.Domain.Notifications;

/// <summary>
/// Transactional outbox record for outbound emails.
/// Written inside the business transaction; dispatched asynchronously by EmailOutboxWorker.
/// Status lifecycle: Queued -> Sent | Failed.
/// </summary>
public sealed class EmailOutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Destination email address.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Subject line.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Email body text / html.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Purpose tag: e.g. "reset-password", "activation", "notification".</summary>
    public string Purpose { get; set; } = "general";

    /// <summary>Delivery status: Queued, Sent, Failed.</summary>
    public string Status { get; set; } = "Queued";

    /// <summary>Number of delivery attempts made so far.</summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>Maximum delivery attempts allowed before marking as Failed.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Timestamp when message was queued.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Timestamp for next scheduled dispatch attempt (for exponential backoff).</summary>
    public DateTimeOffset? NextAttemptAtUtc { get; set; }

    /// <summary>Timestamp when email was confirmed sent by SMTP/sink.</summary>
    public DateTimeOffset? SentAtUtc { get; set; }

    /// <summary>Expiration timestamp for time-sensitive emails (e.g. password reset token lifetime). Expired emails are aborted.</summary>
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    /// <summary>Last delivery failure error message (sanitized and redacted of sensitive tokens).</summary>
    public string? LastError { get; set; }

    /// <summary>Unique idempotency key to prevent duplicate email outbox entries.</summary>
    public string? IdempotencyKey { get; set; }
}
