using HRM.Next.Domain;

namespace HRM.Next.Application.Personnel;

public static class EmployeeLifecycleGuard
{
    public static void EnsureEligibleForActiveOperation(string? status, DateOnly? terminationDate, DateOnly? targetDate = null)
    {
        if (string.Equals(status, "DaNghiViec", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainRuleException("employees.terminated", "Terminated employees cannot perform operational activities.");
        }

        if (terminationDate.HasValue && targetDate.HasValue && targetDate.Value > terminationDate.Value)
        {
            throw new DomainRuleException("employees.past_termination_date", "Operation date cannot be after employee termination date.");
        }
    }
}
