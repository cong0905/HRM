using System.Text.RegularExpressions;

namespace HRM.Next.Infrastructure.Notifications;

public static class EmailLogMasker
{
    private static readonly Regex TokenPattern = new(
        @"(?i)(?:code|token|key|password|secret|pwd|credential)=([^&\s""']+)",
        RegexOptions.Compiled);

    private static readonly Regex BearerPattern = new(
        @"(?i)bearer\s+([a-zA-Z0-9_\-\.]+)",
        RegexOptions.Compiled);

    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "[EMPTY]";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "[MASKED_EMAIL]";
        var name = email[..atIndex];
        var domain = email[atIndex..];
        var maskedName = name.Length switch
        {
            1 => "*",
            2 => name[0] + "*",
            _ => name[0] + new string('*', Math.Min(name.Length - 2, 4)) + name[^1]
        };
        return maskedName + domain;
    }

    public static string Redact(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var redacted = TokenPattern.Replace(input, match =>
        {
            var prefix = match.Value[..match.Value.IndexOf('=')];
            return $"{prefix}=[REDACTED]";
        });
        redacted = BearerPattern.Replace(redacted, "Bearer [REDACTED]");
        return redacted;
    }
}
