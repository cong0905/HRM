using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Next.Infrastructure.Notifications;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public string Mode => "smtp";

    public async Task<SendEmailResult> SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                Timeout = _options.SmtpTimeoutMilliseconds,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(_options.SmtpUsername, _options.SmtpPassword ?? string.Empty);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            // SmtpClient SendMailAsync with cancellation token
            await client.SendMailAsync(message, cancellationToken);

            logger.LogInformation("SMTP dispatch succeeded to {Recipient} with subject '{Subject}'",
                EmailLogMasker.MaskEmail(to), subject);

            return new SendEmailResult(true);
        }
        catch (SmtpException ex)
        {
            var isTransient = ex.StatusCode switch
            {
                SmtpStatusCode.MailboxBusy => true,
                SmtpStatusCode.ServiceNotAvailable => true,
                SmtpStatusCode.TransactionFailed => true,
                SmtpStatusCode.InsufficientStorage => true,
                _ => ex.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                     || ex.InnerException is SocketException or TimeoutException or IOException
            };

            var error = EmailLogMasker.Redact($"SMTP error ({ex.StatusCode}): {ex.Message}");
            logger.LogWarning("SMTP dispatch failed to {Recipient}: {Error} (Transient: {IsTransient})",
                EmailLogMasker.MaskEmail(to), error, isTransient);

            return new SendEmailResult(false, error, isTransient);
        }
        catch (SocketException ex)
        {
            var error = EmailLogMasker.Redact($"Socket connection error ({ex.SocketErrorCode}): {ex.Message}");
            logger.LogWarning("SMTP connection error to {Recipient}: {Error}", EmailLogMasker.MaskEmail(to), error);
            return new SendEmailResult(false, error, IsTransient: true);
        }
        catch (IOException ex)
        {
            var error = EmailLogMasker.Redact($"SMTP IO/network error: {ex.Message}");
            logger.LogWarning("SMTP network error to {Recipient}: {Error}", EmailLogMasker.MaskEmail(to), error);
            return new SendEmailResult(false, error, IsTransient: true);
        }
        catch (TimeoutException ex)
        {
            var error = EmailLogMasker.Redact($"SMTP timeout: {ex.Message}");
            logger.LogWarning("SMTP timeout to {Recipient}: {Error}", EmailLogMasker.MaskEmail(to), error);
            return new SendEmailResult(false, error, IsTransient: true);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            var error = EmailLogMasker.Redact($"Unexpected dispatch error: {ex.Message}");
            logger.LogError(ex, "Unexpected email error for {Recipient}: {Error}", EmailLogMasker.MaskEmail(to), error);
            return new SendEmailResult(false, error, IsTransient: false);
        }
    }
}
