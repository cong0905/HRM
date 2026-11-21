using HRM.Next.Domain.Notifications;
using HRM.Next.Infrastructure.Notifications;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Next.Api.Workers;

/// <summary>
/// Background service that polls the EmailOutboxMessages table and dispatches pending emails.
/// Implements: exponential backoff, retry cap, idempotency, expiry check, and no-log-sent marking.
/// </summary>
public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IEmailSender emailSender,
    IOptions<EmailOptions> options,
    ILogger<EmailOutboxWorker> logger,
    TimeProvider clock) : BackgroundService
{
    private readonly EmailOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailOutboxWorker started. Mode={Mode}, PollInterval={PollInterval}s",
            emailSender.Mode, _options.WorkerPollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in EmailOutboxWorker batch processing");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.WorkerPollIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("EmailOutboxWorker stopping.");
    }

    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HRM.Next.Infrastructure.Persistence.HrmDbContext>();

        var now = clock.GetUtcNow();

        // Pick up Queued messages that are ready for a retry attempt, in creation order.
        // Skip messages where NextAttemptAtUtc is in the future.
        var messages = await db.EmailOutboxMessages
            .AsTracking()
            .Where(m => m.Status == "Queued" && (m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return 0;

        logger.LogDebug("EmailOutboxWorker picked up {Count} messages to dispatch", messages.Count);

        foreach (var message in messages)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await DispatchMessageAsync(db, message, now, cancellationToken);
        }

        return messages.Count;
    }

    private async Task DispatchMessageAsync(
        HrmDbContext db,
        EmailOutboxMessage message,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // Abort expired messages without attempting delivery.
        if (message.ExpiresAtUtc.HasValue && message.ExpiresAtUtc.Value < now)
        {
            message.Status = "Failed";
            message.LastError = "Message expired before delivery.";
            logger.LogWarning("OutboxMessage {Id} purpose={Purpose} expired; aborting without delivery",
                message.Id, message.Purpose);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        message.AttemptCount++;
        logger.LogDebug("Attempting dispatch of OutboxMessage {Id}, attempt #{Attempt}/{Max}",
            message.Id, message.AttemptCount, message.MaxAttempts);

        SendEmailResult result;
        try
        {
            result = await emailSender.SendAsync(message.To, message.Subject, message.Body, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Don't mark as failed; will retry next worker cycle.
            message.AttemptCount--;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }
        catch (Exception ex)
        {
            var errorText = EmailLogMasker.Redact($"Unhandled error: {ex.GetType().Name}: {ex.Message}");
            result = new SendEmailResult(false, errorText, IsTransient: true);
        }

        if (result.Success)
        {
            // NEVER log body or recipient in full — only masked recipient and id.
            message.Status = "Sent";
            message.SentAtUtc = now;
            message.LastError = null;
            logger.LogInformation("OutboxMessage {Id} purpose={Purpose} dispatched to {Recipient}",
                message.Id, message.Purpose, EmailLogMasker.MaskEmail(message.To));
        }
        else
        {
            message.LastError = EmailLogMasker.Redact(result.ErrorMessage ?? "Unknown error");

            if (message.AttemptCount >= message.MaxAttempts)
            {
                // Non-retryable: exceeded attempt cap.
                message.Status = "Failed";
                message.NextAttemptAtUtc = null;
                logger.LogError("OutboxMessage {Id} purpose={Purpose} permanently failed after {Attempts} attempts: {Error}",
                    message.Id, message.Purpose, message.AttemptCount, message.LastError);
            }
            else if (!result.IsTransient)
            {
                // Non-transient permanent delivery failure (e.g. invalid address).
                message.Status = "Failed";
                message.NextAttemptAtUtc = null;
                logger.LogError("OutboxMessage {Id} purpose={Purpose} permanent (non-transient) failure: {Error}",
                    message.Id, message.Purpose, message.LastError);
            }
            else
            {
                // Transient failure — exponential backoff, stays Queued.
                var backoffSeconds = Math.Min(
                    _options.BackoffInitialSeconds * (int)Math.Pow(2, message.AttemptCount - 1),
                    _options.BackoffMaxSeconds);
                message.NextAttemptAtUtc = now.AddSeconds(backoffSeconds);
                logger.LogWarning("OutboxMessage {Id} purpose={Purpose} attempt #{Attempt} failed (transient). Retry after {Backoff}s. Error: {Error}",
                    message.Id, message.Purpose, message.AttemptCount, backoffSeconds, message.LastError);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
