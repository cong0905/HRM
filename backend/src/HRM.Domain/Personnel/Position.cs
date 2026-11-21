namespace HRM.Next.Domain.Personnel;

public sealed class Position
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
