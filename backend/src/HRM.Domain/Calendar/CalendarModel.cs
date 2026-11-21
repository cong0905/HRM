namespace HRM.Next.Domain.Calendar;

public sealed record WorkBreak(TimeOnly Start, TimeOnly End);
public sealed record WorkCalendarDefinition(string FixtureId, string TimeZone,
    TimeOnly ShiftStart, TimeOnly ShiftEnd, IReadOnlyList<WorkBreak> Breaks,
    IReadOnlyList<DayOfWeek> WorkingDays, IReadOnlyList<DateOnly> Holidays);

public sealed class WorkCalendarVersion
{
    public Guid Id { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string DefinitionJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
}

public sealed class CalendarPeriodSnapshot
{
    public Guid Id { get; set; }
    public Guid PayrollPeriodId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string ContentJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public static class CalendarMath
{
    public const string TimeZoneId = "Asia/Ho_Chi_Minh";
    public static DateOnly BusinessDate(DateTimeOffset instant) =>
        DateOnly.FromDateTime(instant.ToOffset(TimeSpan.FromHours(7)).DateTime);

    public static void Validate(WorkCalendarDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.FixtureId) || definition.FixtureId.Length > 100 ||
            definition.TimeZone != TimeZoneId || definition.Breaks is null || definition.WorkingDays is null ||
            definition.Holidays is null || definition.Breaks.Count > 8 || definition.Holidays.Count > 366 ||
            definition.Breaks.Any(b => b is null) || definition.WorkingDays.Count is < 1 or > 7 || definition.WorkingDays.Any(d => !Enum.IsDefined(d)))
            throw new DomainRuleException("calendar.invalid_definition", "Invalid calendar definition.");
        if (definition.ShiftEnd <= definition.ShiftStart)
            throw new DomainRuleException("calendar.overnight_unsupported", "Only a same-day shift is supported.");
        var previous = definition.ShiftStart;
        foreach (var rest in definition.Breaks.OrderBy(b => b.Start))
        {
            if (rest.End <= rest.Start || rest.Start < definition.ShiftStart || rest.End > definition.ShiftEnd || rest.Start < previous)
                throw new DomainRuleException("calendar.invalid_break", "Breaks must be inside the shift and cannot overlap.");
            previous = rest.End;
        }
        if (definition.Breaks.Sum(b => b.End.Ticks - b.Start.Ticks) >= definition.ShiftEnd.Ticks - definition.ShiftStart.Ticks)
            throw new DomainRuleException("calendar.no_work_time", "A working shift must include eligible time.");
    }

    public static decimal StandardMinutes(WorkCalendarDefinition definition, DateOnly date)
    {
        Validate(definition);
        if (!definition.WorkingDays.Contains(date.DayOfWeek) || definition.Holidays.Contains(date)) return 0m;
        return (definition.ShiftEnd.Ticks - definition.ShiftStart.Ticks -
            definition.Breaks.Sum(b => b.End.Ticks - b.Start.Ticks)) / (decimal)TimeSpan.TicksPerMinute;
    }

    public static decimal EligibleMinutes(WorkCalendarDefinition definition, DateOnly date,
        DateTimeOffset checkIn, DateTimeOffset? checkOut)
    {
        Validate(definition);
        if (BusinessDate(checkIn) != date)
            throw new DomainRuleException("calendar.business_date_mismatch", "Check-in must belong to the business date.");
        if (!checkOut.HasValue) return 0m;
        if (checkOut.Value < checkIn)
            throw new DomainRuleException("calendar.invalid_interval", "Check-out cannot precede check-in.");
        if (BusinessDate(checkOut.Value) != date)
            throw new DomainRuleException("calendar.overnight_unsupported", "Cross-business-date attendance is not supported.");
        if (StandardMinutes(definition, date) == 0m) return 0m;
        var start = Math.Max(checkIn.ToOffset(TimeSpan.FromHours(7)).TimeOfDay.Ticks, definition.ShiftStart.Ticks);
        var end = Math.Min(checkOut.Value.ToOffset(TimeSpan.FromHours(7)).TimeOfDay.Ticks, definition.ShiftEnd.Ticks);
        if (end <= start) return 0m;
        var breaks = definition.Breaks.Sum(b => Math.Max(0L, Math.Min(end, b.End.Ticks) - Math.Max(start, b.Start.Ticks)));
        return (end - start - breaks) / (decimal)TimeSpan.TicksPerMinute;
    }
}
