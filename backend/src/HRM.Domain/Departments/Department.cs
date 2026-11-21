namespace HRM.Next.Domain.Departments;

public sealed class Department
{
    private Department() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Department Create(string name, DateTimeOffset now)
    {
        var department = new Department { Id = Guid.NewGuid(), CreatedAtUtc = now.ToUniversalTime() };
        department.Rename(name);
        return department;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new DomainRuleException("department.invalid_name", "Tên phòng ban phải có từ 1 đến 100 ký tự.");
        Name = name.Trim();
    }
}
