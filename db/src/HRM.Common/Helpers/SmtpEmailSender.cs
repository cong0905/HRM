using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HRM.Common.Helpers;

public class SmtpEmailSender : IEmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string? _smtpUser;
    private readonly string? _smtpPass;
    private readonly bool _enableSsl;

    public SmtpEmailSender(string smtpHost, int smtpPort, string? smtpUser = null, string? smtpPass = null, bool enableSsl = true)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
        _enableSsl = enableSsl;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        using var msg = new MailMessage();
        msg.To.Add(toEmail);
        msg.Subject = subject;
        msg.Body = htmlBody;
        msg.IsBodyHtml = true;
        msg.From = new MailAddress(_smtpUser ?? "no-reply@hrm.local", "HRM System");

        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = _enableSsl
        };

        if (!string.IsNullOrEmpty(_smtpUser) && !string.IsNullOrEmpty(_smtpPass))
        {
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
        }

        await client.SendMailAsync(msg);
    }
}
