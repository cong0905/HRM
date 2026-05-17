using System.Threading.Tasks;

namespace HRM.Common.Helpers;

public class NullEmailSender : IEmailSender
{
    public Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        // No-op: used when SMTP not configured in development
        return Task.CompletedTask;
    }
}
