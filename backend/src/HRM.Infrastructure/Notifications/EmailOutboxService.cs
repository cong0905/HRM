using HRM.Next.Domain.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Next.Infrastructure.Notifications;

public sealed class EmailOutboxService(
    HRM.Next.Infrastructure.Persistence.HrmDbContext db,
    IOptions<EmailOptions> options,
    TimeProvider clock,
    ILogger<EmailOutboxService> logger)
{
    private readonly EmailOptions _options = options.Value;

    /// <summary>
    /// Enqueues a password reset email into the transactional outbox.
    /// Must be called inside the same DB transaction as the user record change.
    /// Never logs the token value itself.
    /// </summary>
    public async Task<EmailOutboxMessage> QueuePasswordResetAsync(
        string toEmail,
        Guid userId,
        string resetToken,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = $"reset:{userId}:{resetToken.GetHashCode():X8}";

        // Idempotency: check if already queued.
        var existing = db.EmailOutboxMessages.Local
            .FirstOrDefault(m => m.IdempotencyKey == idempotencyKey && m.Status == "Queued");
        if (existing is not null) return existing;

        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.ResetTokenLifespanMinutes);
        var resetUrl = BuildResetUrl(userId, resetToken);

        var subject = "Yêu cầu đặt lại mật khẩu HRM";
        var body = BuildPasswordResetBody(resetUrl, expiresAt);

        var message = new EmailOutboxMessage
        {
            Id = Guid.NewGuid(),
            To = toEmail,
            Subject = subject,
            Body = body,
            Purpose = "reset-password",
            Status = "Queued",
            AttemptCount = 0,
            MaxAttempts = _options.MaxRetryAttempts,
            CreatedAtUtc = now,
            NextAttemptAtUtc = null,
            ExpiresAtUtc = expiresAt,
            IdempotencyKey = idempotencyKey
        };

        db.EmailOutboxMessages.Add(message);

        // Intentionally NOT logging token value.
        logger.LogDebug("Queued reset email outbox message {MessageId} for user {UserId}, expires {ExpiresAt}",
            message.Id, userId, expiresAt);

        await db.SaveChangesAsync(cancellationToken);
        return message;
    }

    private string BuildResetUrl(Guid userId, string token)
    {
        // Token embedded in URL, not in log. URL is passed to email body only.
        var origin = _options.ResetUrlOrigin.TrimEnd('/');
        return $"{origin}/auth/reset-password?userId={userId}&code={Uri.EscapeDataString(token)}";
    }

    private string BuildPasswordResetBody(string resetUrl, DateTimeOffset expiresAt)
    {
        var expiresLocalVn = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(expiresAt.UtcDateTime, "SE Asia Standard Time");
        var expiresStr = expiresLocalVn.ToString("HH:mm 'ngày' dd/MM/yyyy");
        return $"""
            Xin chào,

            Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn trong hệ thống HRM.

            Nhấn vào liên kết sau để đặt lại mật khẩu (có hiệu lực trong {_options.ResetTokenLifespanMinutes} phút, đến {expiresStr} ICT):

            {resetUrl}

            Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này. Mật khẩu hiện tại vẫn còn hiệu lực.

            Lưu ý: Liên kết này chỉ sử dụng được một lần và hết hạn sau {_options.ResetTokenLifespanMinutes} phút (lúc {expiresStr} ICT).

            Trân trọng,
            HRM System
            """;
    }
}
