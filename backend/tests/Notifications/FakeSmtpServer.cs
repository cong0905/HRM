using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HRM.Next.Tests.Integration.Notifications;

public sealed record FakeSmtpMessage(string? From, List<string> Recipients, string Data, DateTimeOffset ReceivedAtUtc)
{
    public string GetDecodedBody()
    {
        var parts = Data.Split("\r\n\r\n", 2);
        if (parts.Length > 1 && Data.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var clean = parts[1].Replace("\r", "").Replace("\n", "");
                return Encoding.UTF8.GetString(Convert.FromBase64String(clean));
            }
            catch { }
        }
        return Data;
    }
}

public enum FakeSmtpBehavior
{
    Normal,
    Timeout,
    TransientFailure,
    PermanentFailure,
    DropConnection
}

public sealed class FakeSmtpServer : IAsyncDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private readonly ConcurrentBag<FakeSmtpMessage> _messages = new();

    public int Port { get; private set; }
    public FakeSmtpBehavior Behavior { get; set; } = FakeSmtpBehavior.Normal;
    public int TimeoutDelayMs { get; set; } = 8000;
    public IReadOnlyCollection<FakeSmtpMessage> ReceivedMessages => _messages.ToArray();

    public void ClearMessages() => _messages.Clear();

    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Loopback, Port == 0 ? 0 : Port);
        _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => AcceptLoopAsync(_cts.Token));
        await Task.Yield();
    }

    public async Task StopAsync()
    {
        _cts?.Cancel();
        _listener?.Stop();
        if (_listenTask != null)
        {
            try { await _listenTask; } catch { }
        }
    }

    public async Task RestartAsync()
    {
        await StopAsync();
        await Task.Delay(100);
        await StartAsync();
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClientAsync(client, token), token);
            }
            catch (OperationCanceledException) { break; }
            catch { break; }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (client)
        await using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.ASCII))
        await using (var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true })
        {
            if (Behavior == FakeSmtpBehavior.DropConnection)
            {
                client.Close();
                return;
            }

            if (Behavior == FakeSmtpBehavior.Timeout)
            {
                await Task.Delay(TimeoutDelayMs, token);
            }

            await writer.WriteAsync("220 127.0.0.1 ESMTP FakeSmtpServer Ready\r\n");

            string? from = null;
            var recipients = new List<string>();
            var dataMode = false;
            var dataBuffer = new StringBuilder();

            while (!token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(token);
                if (line == null) break;

                if (dataMode)
                {
                    if (line == ".")
                    {
                        dataMode = false;
                        _messages.Add(new FakeSmtpMessage(from, new List<string>(recipients), dataBuffer.ToString(), DateTimeOffset.UtcNow));
                        await writer.WriteAsync("250 2.0.0 Ok: queued as fake-12345\r\n");
                        dataBuffer.Clear();
                    }
                    else
                    {
                        dataBuffer.Append(line).Append("\r\n");
                    }
                    continue;
                }

                var upper = line.ToUpperInvariant();

                if (upper.StartsWith("EHLO") || upper.StartsWith("HELO"))
                {
                    await writer.WriteAsync("250-127.0.0.1\r\n250-8BITMIME\r\n250 OK\r\n");
                }
                else if (upper.StartsWith("MAIL FROM:"))
                {
                    from = line["MAIL FROM:".Length..].Trim().Trim('<', '>');
                    await writer.WriteAsync("250 2.1.0 Ok\r\n");
                }
                else if (upper.StartsWith("RCPT TO:"))
                {
                    if (Behavior == FakeSmtpBehavior.TransientFailure)
                    {
                        await writer.WriteAsync("421 4.3.0 Service temporarily unavailable\r\n");
                        continue;
                    }

                    if (Behavior == FakeSmtpBehavior.PermanentFailure)
                    {
                        await writer.WriteAsync("550 5.1.1 User unknown\r\n");
                        continue;
                    }

                    var rcpt = line["RCPT TO:".Length..].Trim().Trim('<', '>');
                    recipients.Add(rcpt);
                    await writer.WriteAsync("250 2.1.5 Ok\r\n");
                }
                else if (upper.StartsWith("DATA"))
                {
                    if (Behavior == FakeSmtpBehavior.TransientFailure)
                    {
                        await writer.WriteAsync("421 4.3.0 Cannot accept mail at this time\r\n");
                        continue;
                    }

                    dataMode = true;
                    await writer.WriteAsync("354 End data with <CR><LF>.<CR><LF>\r\n");
                }
                else if (upper.StartsWith("RSET"))
                {
                    from = null;
                    recipients.Clear();
                    dataBuffer.Clear();
                    dataMode = false;
                    await writer.WriteAsync("250 2.0.0 Ok\r\n");
                }
                else if (upper.StartsWith("NOOP"))
                {
                    await writer.WriteAsync("250 2.0.0 Ok\r\n");
                }
                else if (upper.StartsWith("QUIT"))
                {
                    await writer.WriteAsync("221 2.0.0 Bye\r\n");
                    break;
                }
                else
                {
                    await writer.WriteAsync("502 5.5.2 Command not implemented\r\n");
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
