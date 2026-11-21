namespace HRM.Next.Infrastructure.Identity;

// T22 replaces this port with an outbox. Never log message tokens.
public sealed record AccountMessage(string Email, Guid UserId, string Purpose, string Token);
public interface IAccountMessageSender
{
    string Mode { get; }
    Task SendAsync(AccountMessage message, CancellationToken cancellationToken);
}

public sealed class UnconfiguredAccountMessageSender : IAccountMessageSender
{
    public string Mode => "not-configured";
    public Task SendAsync(AccountMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}
