using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HRM.Next.Infrastructure.Notifications;

public sealed class EmailOptionsValidator(IHostEnvironment environment) : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        var failures = new List<string>();

        if (environment.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(options.DeliveryMode) ||
                options.DeliveryMode.Equals("dev-sink", StringComparison.OrdinalIgnoreCase) ||
                options.DeliveryMode.Equals("not-configured", StringComparison.OrdinalIgnoreCase) ||
                options.DeliveryMode.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                failures.Add("Production environment requires an active email delivery configuration (e.g. 'smtp'). Silently falling back to dev-sink or NullEmailSender is prohibited.");
            }

            if (options.DeliveryMode.Equals("smtp", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(options.SmtpHost))
                    failures.Add("SmtpHost is required in production.");

                if (string.IsNullOrWhiteSpace(options.FromAddress) || !options.FromAddress.Contains('@'))
                    failures.Add("FromAddress must be a valid email address in production.");

                if (string.IsNullOrWhiteSpace(options.ResetUrlOrigin) || !Uri.TryCreate(options.ResetUrlOrigin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    failures.Add("ResetUrlOrigin must be an absolute HTTPS URL in production.");
            }
        }

        if (options.ResetTokenLifespanMinutes <= 0)
            failures.Add("ResetTokenLifespanMinutes must be greater than 0.");

        if (options.MaxRetryAttempts <= 0)
            failures.Add("MaxRetryAttempts must be greater than 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
