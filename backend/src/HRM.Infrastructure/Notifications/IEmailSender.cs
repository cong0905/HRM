namespace HRM.Next.Infrastructure.Notifications;

public sealed record SendEmailResult(bool Success, string? ErrorMessage = null, bool IsTransient = false);

public interface IEmailSender
{
    string Mode { get; }
    Task<SendEmailResult> SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}
