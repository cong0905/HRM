using System.Globalization;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Personnel;

namespace HRM.Next.Domain.Leave;

public sealed class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int Year { get; set; }
    public decimal AllocatedDays { get; set; }
    public decimal UsedDays { get; set; }
    public decimal RemainingDays => AllocatedDays - UsedDays;
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class LeaveLedgerEntry
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid? LeaveRequestId { get; set; }
    public int Year { get; set; }
    public string Action { get; set; } = string.Empty; // "Grant", "Debit", "Reversal"
    public decimal Days { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class LeaveIdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Operation { get; set; } = string.Empty; // "leave.create", "leave.approve", etc.
    public Guid Key { get; set; }
    public string RequestHash { get; set; } = string.Empty;
    public string ResponseJson { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public static class LeaveMath
{
    public static void ValidateDates(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new DomainRuleException("leave.invalid_date_range", "End date cannot precede start date.");

        if (start.Year != end.Year)
            throw new DomainRuleException("leave.cross_year_unsupported", "Cross-year leave requests are not supported in MVP. Please submit separate requests for each calendar year.");
    }

    public static decimal CalculateWorkingDays(WorkCalendarDefinition definition, DateOnly start, DateOnly end)
    {
        ValidateDates(start, end);
        CalendarMath.Validate(definition);

        decimal workingDays = 0m;
        for (var current = start; current <= end; current = current.AddDays(1))
        {
            if (CalendarMath.StandardMinutes(definition, current) > 0m)
            {
                workingDays += 1.0m;
            }
        }

        return workingDays;
    }

    public static bool IsOverlapping(DateOnly startA, DateOnly endA, DateOnly startB, DateOnly endB)
    {
        return startA <= endB && endA >= startB;
    }
}
