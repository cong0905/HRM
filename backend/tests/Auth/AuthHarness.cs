using System.Collections.Concurrent;
using HRM.Next.Api;
using HRM.Next.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace HRM.Next.Tests.Integration;

public sealed class AuthClock : TimeProvider
{
    private long _ticks = DateTimeOffset.UtcNow.UtcTicks;
    public override DateTimeOffset GetUtcNow() => new(Interlocked.Read(ref _ticks), TimeSpan.Zero);
    public void Advance(TimeSpan value) => Interlocked.Add(ref _ticks, value.Ticks);
}

// Captures token-bearing messages only in this isolated test assembly; never sends real email.
public sealed class TestAccountSender : IAccountMessageSender
{
    public string Mode => "test-only";
    public ConcurrentQueue<AccountMessage> Messages { get; } = new();
    public Task SendAsync(AccountMessage message, CancellationToken cancellationToken)
    {
        Messages.Enqueue(message);
        return Task.CompletedTask;
    }
}

public sealed class CaptureAuthLogs : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Messages);
    public void Dispose() { }
    private sealed class CaptureLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter) =>
            messages.Enqueue(formatter(state, error));
    }
}

public sealed class AuthHarness : IAsyncDisposable
{
    private readonly ApiFactory _base;
    public WebApplicationFactory<Program> Factory { get; }
    public AuthClock Clock { get; } = new();
    public TestAccountSender Sender { get; } = new();
    public CaptureAuthLogs Logs { get; } = new();

    public AuthHarness(string connection, bool testSender = true, Action<IServiceCollection>? configure = null)
    {
        _base = new ApiFactory(connection, testIdentity: false, probes: false);
        Factory = _base.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Clock);
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                if (testSender)
                {
                    services.RemoveAll<IAccountMessageSender>();
                    services.AddSingleton<IAccountMessageSender>(Sender);
                }
                configure?.Invoke(services);
            });
            builder.ConfigureLogging(logging => logging.ClearProviders().AddProvider(Logs));
        });
    }

    public HttpClient Client() => Factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _base.DisposeAsync();
    }
}
