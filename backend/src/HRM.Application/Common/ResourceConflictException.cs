namespace HRM.Next.Application.Common;

// Callers supply a stable error code and a safe, public message (never SQL/input text).
public sealed class ResourceConflictException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
