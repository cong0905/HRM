namespace HRM.Next.Domain.Personnel;

public sealed class HrEmployeeAssignment
{
    public Guid Id { get; set; }
    public Guid HrUserId { get; set; }
    public Guid EmployeeId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; }
    public Guid? AssignedBy { get; set; }
}

