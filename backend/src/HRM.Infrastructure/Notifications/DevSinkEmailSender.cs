using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace HRM.Next.Infrastructure.Notifications;

public sealed record CapturedEmail(Guid Id, string To, string Subject, string Body, DateTimeOffset CapturedAtUtc);

public sealed class DevSinkInbox
{
    private readonly ConcurrentQueue<CapturedEmail> _messages = new();

    public void Add(CapturedEmail email) => _messages.Enqueue(email);

    public IReadOnlyCollection<CapturedEmail> GetAll() => _messages.ToArray();

    public void Clear() => _messages.Clear();
}

public sealed class DevSinkEmailSender(DevSinkInbox inbox, ILogger<DevSinkEmailSender> logger) : IEmailSender
{
    public const string DevSinkPrefix = "[DEV SINK]";
    public const string DevSinkNotice = "--- [DEV SINK - TEST NOTICE: THIS EMAIL WAS CAPTURED LOCALLY AND NOT SENT TO REAL USER] ---";

    public string Mode => "dev-sink";

    public Task<SendEmailResult> SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var labeledSubject = subject.StartsWith(DevSinkPrefix, StringComparison.OrdinalIgnoreCase)
            ? subject
            : $"{DevSinkPrefix} {subject}";

        var labeledBody = $"{DevSinkNotice}\r\n\r\n{body}\r\n\r\n{DevSinkNotice}";

        var item = new CapturedEmail(Guid.NewGuid(), to, labeledSubject, labeledBody, DateTimeOffset.UtcNow);
        inbox.Add(item);

        logger.LogInformation("DevSink intercepted email {MessageId} to {Recipient} with subject '{Subject}'",
            item.Id, EmailLogMasker.MaskEmail(to), labeledSubject);

        return Task.FromResult(new SendEmailResult(true));
    }
}
