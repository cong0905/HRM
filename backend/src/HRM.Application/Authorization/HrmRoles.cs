namespace HRM.Next.Application.Authorization;

public static class HrmRoles
{
    public const string Employee = "Employee";
    public const string Hr = "HR";
    public const string Payroll = "Payroll";
    public const string SystemAdmin = "SystemAdmin";

    public static readonly IReadOnlyList<string> All = [Employee, Hr, Payroll, SystemAdmin];

    public static bool IsValid(string role) => All.Contains(role);
}

