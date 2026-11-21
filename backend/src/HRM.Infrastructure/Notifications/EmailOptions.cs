namespace HRM.Next.Infrastructure.Notifications;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Delivery mode: "smtp", "dev-sink", "test-sink", "not-configured"
    /// </summary>
    public string DeliveryMode { get; set; } = "dev-sink";

    /// <summary>
    /// Base URL origin for reset/activation links (e.g. "https://hrm.example.com")
    /// </summary>
    public string ResetUrlOrigin { get; set; } = "https://localhost:5001";

    /// <summary>
    /// Reset token lifespan in minutes. Aligns with DataProtectionTokenProviderOptions (default: 15).
    /// </summary>
    public int ResetTokenLifespanMinutes { get; set; } = 15;

    public string SmtpHost { get; set; } = "127.0.0.1";
    public int SmtpPort { get; set; } = 25;
    public bool EnableSsl { get; set; } = false;
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string FromAddress { get; set; } = "noreply@hrm.local";
    public string FromName { get; set; } = "HRM Notification";

    public int SmtpTimeoutMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Max delivery attempts before marking outbox record as Failed.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Initial exponential backoff in seconds.
    /// </summary>
    public int BackoffInitialSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum backoff delay cap in seconds.
    /// </summary>
    public int BackoffMaxSeconds { get; set; } = 3600;

    /// <summary>
    /// Background worker poll interval in seconds.
    /// </summary>
    public int WorkerPollIntervalSeconds { get; set; } = 5;
}
