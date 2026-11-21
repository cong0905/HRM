using System.Net;
using System.Net.Http.Json;
using HRM.Next.Api.Auth;
using HRM.Next.Api.Workers;
using HRM.Next.Domain.Notifications;
using HRM.Next.Infrastructure.Identity;
using HRM.Next.Infrastructure.Notifications;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HRM.Next.Tests.Integration.Notifications;

public sealed class EmailOutboxTests : IClassFixture<SqlDatabaseFixture>, IAsyncLifetime
{
    private readonly SqlDatabaseFixture _sql;

    public EmailOutboxTests(SqlDatabaseFixture sql)
    {
        _sql = sql;
    }

    public Task InitializeAsync() => _sql.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Outbox_Delivery_Success_MarksSent_RecordsTimestamp_And_FakeSmtpReceivesEmail()
    {
        await using var fakeSmtp = new FakeSmtpServer();
        await fakeSmtp.StartAsync();

        await using var harness = new AuthHarness(_sql.ConnectionString, configure: services =>
        {
            services.Configure<EmailOptions>(options =>
            {
                options.DeliveryMode = "smtp";
                options.SmtpHost = "127.0.0.1";
                options.SmtpPort = fakeSmtp.Port;
                options.EnableSsl = false;
                options.ResetUrlOrigin = "https://hrm.example.com";
                options.ResetTokenLifespanMinutes = 15;
            });
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        });

        // 1. Create active user
        await using var scope = harness.Factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<HrmUser>>();
        var user = new HrmUser { Id = Guid.NewGuid(), UserName = "reset-test-user", Email = "reset.user@example.com", IsActive = true };
        Assert.True((await manager.CreateAsync(user, "Test-only!Passphrase2026")).Succeeded);

        // 2. Request forgot password via HTTP endpoint
        using var client = harness.Client();
        using var forgotResponse = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotRequest(user.Email));
        Assert.Equal(HttpStatusCode.Accepted, forgotResponse.StatusCode);

        // 3. Verify message is Queued in transactional outbox
        await using var db = _sql.CreateContext();
        var outboxMessage = await db.EmailOutboxMessages.FirstOrDefaultAsync(m => m.To == user.Email);
        Assert.NotNull(outboxMessage);
        Assert.Equal("Queued", outboxMessage.Status);
        Assert.Equal(0, outboxMessage.AttemptCount);
        Assert.Null(outboxMessage.SentAtUtc);
        Assert.Contains("https://hrm.example.com/auth/reset-password", outboxMessage.Body);
        Assert.Contains("15 phút", outboxMessage.Body);

        // 4. Run background worker batch
        var worker = harness.Factory.Services.GetRequiredService<EmailOutboxWorker>();
        var processedCount = await worker.ProcessBatchAsync(CancellationToken.None);
        Assert.True(processedCount >= 1);

        // 5. Verify outbox message updated to Sent
        await using var dbVerify = _sql.CreateContext();
        var sentMessage = await dbVerify.EmailOutboxMessages.SingleAsync(m => m.Id == outboxMessage.Id);
        Assert.Equal("Sent", sentMessage.Status);
        Assert.NotNull(sentMessage.SentAtUtc);
        Assert.Equal(1, sentMessage.AttemptCount);
        Assert.Null(sentMessage.LastError);

        // 6. Evidence: Fake SMTP actually received the email
        Assert.Single(fakeSmtp.ReceivedMessages);
        var received = fakeSmtp.ReceivedMessages.First();
        Assert.Contains(user.Email, received.Recipients);
        Assert.Contains("Subject:", received.Data);
        Assert.Contains("https://hrm.example.com/auth/reset-password", received.GetDecodedBody());
    }

    [Fact]
    public async Task Outbox_Delivery_TransientFailure_StaysQueued_CalculatesBackoff_DoesNotMarkSent()
    {
        await using var fakeSmtp = new FakeSmtpServer { Behavior = FakeSmtpBehavior.TransientFailure };
        await fakeSmtp.StartAsync();

        await using var harness = new AuthHarness(_sql.ConnectionString, configure: services =>
        {
            services.Configure<EmailOptions>(options =>
            {
                options.DeliveryMode = "smtp";
                options.SmtpHost = "127.0.0.1";
                options.SmtpPort = fakeSmtp.Port;
                options.EnableSsl = false;
                options.BackoffInitialSeconds = 20;
            });
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        });

        // Insert queued outbox message
        var msgId = Guid.NewGuid();
        await using (var db = _sql.CreateContext())
        {
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                Id = msgId,
                To = "transient.fail@example.com",
                Subject = "Test Transient",
                Body = "Body with reset token code=SecretToken12345",
                Purpose = "reset-password",
                Status = "Queued",
                AttemptCount = 0,
                MaxAttempts = 5,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await db.SaveChangesAsync();
        }

        // Process batch
        var worker = harness.Factory.Services.GetRequiredService<EmailOutboxWorker>();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Verify record in DB: Still Queued, NOT Sent, attempt incremented, next attempt set
        await using (var dbVerify = _sql.CreateContext())
        {
            var msg = await dbVerify.EmailOutboxMessages.SingleAsync(m => m.Id == msgId);
            Assert.Equal("Queued", msg.Status);
            Assert.Null(msg.SentAtUtc);
            Assert.Equal(1, msg.AttemptCount);
            Assert.NotNull(msg.NextAttemptAtUtc);
            Assert.True(msg.NextAttemptAtUtc > DateTimeOffset.UtcNow);
            Assert.NotNull(msg.LastError);
            // Safe redaction: error detail must not contain token
            Assert.DoesNotContain("SecretToken12345", msg.LastError);
        }
    }

    [Fact]
    public async Task Outbox_Delivery_PermanentFailure_MarksFailed_StopsRetrying()
    {
        await using var fakeSmtp = new FakeSmtpServer { Behavior = FakeSmtpBehavior.PermanentFailure };
        await fakeSmtp.StartAsync();

        await using var harness = new AuthHarness(_sql.ConnectionString, configure: services =>
        {
            services.Configure<EmailOptions>(options =>
            {
                options.DeliveryMode = "smtp";
                options.SmtpHost = "127.0.0.1";
                options.SmtpPort = fakeSmtp.Port;
                options.EnableSsl = false;
            });
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        });

        var msgId = Guid.NewGuid();
        await using (var db = _sql.CreateContext())
        {
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                Id = msgId,
                To = "permanent.fail@example.com",
                Subject = "Test Permanent",
                Body = "Body",
                Purpose = "reset-password",
                Status = "Queued",
                AttemptCount = 0,
                MaxAttempts = 5,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await db.SaveChangesAsync();
        }

        var worker = harness.Factory.Services.GetRequiredService<EmailOutboxWorker>();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await using (var dbVerify = _sql.CreateContext())
        {
            var msg = await dbVerify.EmailOutboxMessages.SingleAsync(m => m.Id == msgId);
            Assert.Equal("Failed", msg.Status);
            Assert.Null(msg.SentAtUtc);
            Assert.Null(msg.NextAttemptAtUtc);
            Assert.NotNull(msg.LastError);
        }
    }

    [Fact]
    public async Task Outbox_MaxAttemptsExceeded_StopsRetrying_MarksFailed()
    {
        await using var fakeSmtp = new FakeSmtpServer { Behavior = FakeSmtpBehavior.TransientFailure };
        await fakeSmtp.StartAsync();

        await using var harness = new AuthHarness(_sql.ConnectionString, configure: services =>
        {
            services.Configure<EmailOptions>(options =>
            {
                options.DeliveryMode = "smtp";
                options.SmtpHost = "127.0.0.1";
                options.SmtpPort = fakeSmtp.Port;
                options.EnableSsl = false;
            });
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        });

        var msgId = Guid.NewGuid();
        await using (var db = _sql.CreateContext())
        {
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                Id = msgId,
                To = "cap.exceeded@example.com",
                Subject = "Attempt Cap Test",
                Body = "Body",
                Purpose = "reset-password",
                Status = "Queued",
                AttemptCount = 4, // Next attempt will be #5 == MaxAttempts
                MaxAttempts = 5,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await db.SaveChangesAsync();
        }

        var worker = harness.Factory.Services.GetRequiredService<EmailOutboxWorker>();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await using (var dbVerify = _sql.CreateContext())
        {
            var msg = await dbVerify.EmailOutboxMessages.SingleAsync(m => m.Id == msgId);
            Assert.Equal("Failed", msg.Status);
            Assert.Equal(5, msg.AttemptCount);
            Assert.Null(msg.SentAtUtc);
            Assert.Null(msg.NextAttemptAtUtc);
        }
    }

    [Fact]
    public async Task Outbox_ExpiredToken_IsNotDispatched_MarkedFailedBeforeSend()
    {
        await using var fakeSmtp = new FakeSmtpServer();
        await fakeSmtp.StartAsync();

        await using var harness = new AuthHarness(_sql.ConnectionString, configure: services =>
        {
            services.Configure<EmailOptions>(options =>
            {
                options.DeliveryMode = "smtp";
                options.SmtpHost = "127.0.0.1";
                options.SmtpPort = fakeSmtp.Port;
                options.EnableSsl = false;
            });
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        });

        var msgId = Guid.NewGuid();
        await using (var db = _sql.CreateContext())
        {
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                Id = msgId,
                To = "expired.token@example.com",
                Subject = "Expired Token Test",
                Body = "Body with expired token",
                Purpose = "reset-password",
                Status = "Queued",
                AttemptCount = 0,
                MaxAttempts = 5,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddHours(-1),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5) // Expired 5 minutes ago
            });
            await db.SaveChangesAsync();
        }

        var worker = harness.Factory.Services.GetRequiredService<EmailOutboxWorker>();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Verify Fake SMTP received 0 emails
        Assert.Empty(fakeSmtp.ReceivedMessages);

        // Verify outbox status is Failed with expired reason
        await using (var dbVerify = _sql.CreateContext())
        {
            var msg = await dbVerify.EmailOutboxMessages.SingleAsync(m => m.Id == msgId);
            Assert.Equal("Failed", msg.Status);
            Assert.Equal(0, msg.AttemptCount); // Never attempted delivery
            Assert.Contains("expired", msg.LastError, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Outbox_DevSink_LabelsEmailExplicitly_CapturesInDevInbox()
    {
        var inbox = new DevSinkInbox();
        var sender = new DevSinkEmailSender(inbox, NullLogger<DevSinkEmailSender>.Instance);

        var result = await sender.SendAsync("dev.user@example.local", "Test Subject", "Hello Developer", CancellationToken.None);
        Assert.True(result.Success);

        var captured = Assert.Single(inbox.GetAll());
        Assert.StartsWith("[DEV SINK]", captured.Subject);
        Assert.Contains(DevSinkEmailSender.DevSinkNotice, captured.Body);
        Assert.Equal("dev.user@example.local", captured.To);
    }

    [Fact]
    public void ConfigValidation_InProduction_FailsSilentlyWhenNotConfigured()
    {
        var prodEnv = new TestHostEnvironment { EnvironmentName = "Production" };
        var validator = new EmailOptionsValidator(prodEnv);

        // 1. Dev-sink in production -> FAIL
        var devSinkOptions = new EmailOptions { DeliveryMode = "dev-sink" };
        var result1 = validator.Validate(null, devSinkOptions);
        Assert.True(result1.Failed);
        Assert.Contains("silently falling back", result1.FailureMessage, StringComparison.OrdinalIgnoreCase);

        // 2. Not-configured in production -> FAIL
        var unconfOptions = new EmailOptions { DeliveryMode = "not-configured" };
        var result2 = validator.Validate(null, unconfOptions);
        Assert.True(result2.Failed);

        // 3. Smtp mode without host -> FAIL
        var smtpMissingHost = new EmailOptions { DeliveryMode = "smtp", SmtpHost = "" };
        var result3 = validator.Validate(null, smtpMissingHost);
        Assert.True(result3.Failed);
        Assert.Contains("SmtpHost is required", result3.FailureMessage);

        // 4. Valid production SMTP config -> SUCCESS
        var validProdOptions = new EmailOptions
        {
            DeliveryMode = "smtp",
            SmtpHost = "smtp.sendgrid.net",
            SmtpPort = 587,
            FromAddress = "noreply@hrm.company.com",
            ResetUrlOrigin = "https://hrm.company.com",
            ResetTokenLifespanMinutes = 15
        };
        var result4 = validator.Validate(null, validProdOptions);
        Assert.True(result4.Succeeded);
    }

    [Fact]
    public async Task Outbox_FakeSmtp_Timeout_And_Restart_Recovery()
    {
        await using var fakeSmtp = new FakeSmtpServer { Behavior = FakeSmtpBehavior.Timeout, TimeoutDelayMs = 4000 };
        await fakeSmtp.StartAsync();

        await using var harness = new AuthHarness(_sql.ConnectionString, configure: services =>
        {
            services.Configure<EmailOptions>(options =>
            {
                options.DeliveryMode = "smtp";
                options.SmtpHost = "127.0.0.1";
                options.SmtpPort = fakeSmtp.Port;
                options.EnableSsl = false;
                options.SmtpTimeoutMilliseconds = 1000; // Timeout quickly
                options.BackoffInitialSeconds = 1;
            });
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        });

        var msgId = Guid.NewGuid();
        await using (var db = _sql.CreateContext())
        {
            db.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                Id = msgId,
                To = "timeout.recovery@example.com",
                Subject = "Timeout Recovery Test",
                Body = "Body",
                Purpose = "reset-password",
                Status = "Queued",
                AttemptCount = 0,
                MaxAttempts = 5,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(15)
            });
            await db.SaveChangesAsync();
        }

        var worker = harness.Factory.Services.GetRequiredService<EmailOutboxWorker>();

        // 1. First batch run: Smtp client times out
        await worker.ProcessBatchAsync(CancellationToken.None);

        await using (var dbCheck1 = _sql.CreateContext())
        {
            var msg = await dbCheck1.EmailOutboxMessages.SingleAsync(m => m.Id == msgId);
            Assert.Equal("Queued", msg.Status);
            Assert.Equal(1, msg.AttemptCount);
            Assert.NotNull(msg.LastError);
        }

        // 2. Restart Fake SMTP in normal behavior
        fakeSmtp.Behavior = FakeSmtpBehavior.Normal;
        await fakeSmtp.RestartAsync();

        // Fast-forward next attempt timestamp to null (ready immediately)
        await using (var dbUpdate = _sql.CreateContext())
        {
            var msg = await dbUpdate.EmailOutboxMessages.AsTracking().SingleAsync(m => m.Id == msgId);
            msg.NextAttemptAtUtc = null;
            await dbUpdate.SaveChangesAsync();
        }

        // 3. Second batch run: Succeeds!
        await worker.ProcessBatchAsync(CancellationToken.None);

        await using (var dbCheck2 = _sql.CreateContext())
        {
            var msg = await dbCheck2.EmailOutboxMessages.SingleAsync(m => m.Id == msgId);
            Assert.True(msg.Status == "Sent", $"Status={msg.Status}, Attempt={msg.AttemptCount}, LastError={msg.LastError}");
            Assert.NotNull(msg.SentAtUtc);
            Assert.Equal(2, msg.AttemptCount);
            Assert.Null(msg.LastError);
        }

        Assert.Single(fakeSmtp.ReceivedMessages);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "HRM.Next.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
