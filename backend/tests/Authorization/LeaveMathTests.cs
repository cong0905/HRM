using HRM.Next.Domain;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Leave;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class LeaveMathTests
{
    private static readonly WorkCalendarDefinition DemoCalendar = new(
        FixtureId: "FIXTURE-CAL-2026-04-A",
        TimeZone: "Asia/Ho_Chi_Minh",
        ShiftStart: new TimeOnly(8, 30),
        ShiftEnd: new TimeOnly(17, 30),
        Breaks: [new WorkBreak(new TimeOnly(12, 0), new TimeOnly(13, 0))],
        WorkingDays: [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday],
        Holidays: [new DateOnly(2026, 4, 26), new DateOnly(2026, 4, 27), new DateOnly(2026, 4, 30)]
    );

    [Fact]
    public void ValidateDates_WhenStartBeforeOrEqualEndInSameYear_Succeeds()
    {
        LeaveMath.ValidateDates(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5));
        LeaveMath.ValidateDates(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1));
    }

    [Fact]
    public void ValidateDates_WhenEndPrecedesStart_ThrowsDomainRuleException()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            LeaveMath.ValidateDates(new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 1)));
        Assert.Equal("leave.invalid_date_range", ex.Code);
    }

    [Fact]
    public void ValidateDates_WhenCrossYear_ThrowsDomainRuleException()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            LeaveMath.ValidateDates(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 2)));
        Assert.Equal("leave.cross_year_unsupported", ex.Code);
    }

    [Fact]
    public void CalculateWorkingDays_FullWorkWeek_ReturnsFiveDays()
    {
        // 2026-06-01 is Monday, 2026-06-05 is Friday
        var days = LeaveMath.CalculateWorkingDays(DemoCalendar, new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5));
        Assert.Equal(5.0m, days);
    }

    [Fact]
    public void CalculateWorkingDays_WeekendOnly_ReturnsZeroDays()
    {
        // 2026-06-06 is Saturday, 2026-06-07 is Sunday
        var days = LeaveMath.CalculateWorkingDays(DemoCalendar, new DateOnly(2026, 6, 6), new DateOnly(2026, 6, 7));
        Assert.Equal(0.0m, days);
    }

    [Fact]
    public void CalculateWorkingDays_SpanningWeekend_ExcludesWeekend()
    {
        // 2026-06-05 (Fri) to 2026-06-08 (Mon) -> Fri + Mon = 2 days
        var days = LeaveMath.CalculateWorkingDays(DemoCalendar, new DateOnly(2026, 6, 5), new DateOnly(2026, 6, 8));
        Assert.Equal(2.0m, days);
    }

    [Fact]
    public void CalculateWorkingDays_WithHolidays_ExcludesHolidays()
    {
        // 2026-04-29 (Wed, work), 2026-04-30 (Thu, holiday), 2026-05-01 (Fri, work in this fixture) -> 2 days
        var days = LeaveMath.CalculateWorkingDays(DemoCalendar, new DateOnly(2026, 4, 29), new DateOnly(2026, 5, 1));
        Assert.Equal(2.0m, days);

        // 2026-04-25 (Sat), 2026-04-26 (Sun holiday), 2026-04-27 (Mon holiday) -> 0 days
        var holidayWeekend = LeaveMath.CalculateWorkingDays(DemoCalendar, new DateOnly(2026, 4, 25), new DateOnly(2026, 4, 27));
        Assert.Equal(0.0m, holidayWeekend);
    }

    [Fact]
    public void IsOverlapping_CorrectlyDetectsCollisions()
    {
        // Overlapping
        Assert.True(LeaveMath.IsOverlapping(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5),
            new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 8)));

        // Touching boundary
        Assert.True(LeaveMath.IsOverlapping(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3),
            new DateOnly(2026, 6, 3), new DateOnly(2026, 6, 5)));

        // Non-overlapping (before)
        Assert.False(LeaveMath.IsOverlapping(
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3),
            new DateOnly(2026, 6, 4), new DateOnly(2026, 6, 6)));

        // Non-overlapping (after)
        Assert.False(LeaveMath.IsOverlapping(
            new DateOnly(2026, 6, 10), new DateOnly(2026, 6, 15),
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 5)));
    }
}
