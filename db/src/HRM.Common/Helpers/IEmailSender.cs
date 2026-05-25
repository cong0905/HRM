using System.Threading.Tasks;

namespace HRM.Common.Helpers;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
