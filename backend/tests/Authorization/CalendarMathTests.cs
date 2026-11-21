using System.Globalization;
using HRM.Next.Domain;
using HRM.Next.Domain.Calendar;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class CalendarMathTests
{
    public static WorkCalendarDefinition Definition(string fixture = "FIXTURE-CAL-2026-04-A", params DateOnly[] holidays) =>
        new(fixture, CalendarMath.TimeZoneId,new TimeOnly(8,30),new TimeOnly(17,30),
            [new(new TimeOnly(12,0),new TimeOnly(13,0))],
            [DayOfWeek.Monday,DayOfWeek.Tuesday,DayOfWeek.Wednesday,DayOfWeek.Thursday,DayOfWeek.Friday],holidays);

    [Theory]
    [InlineData("2026-04-01T23:30:00Z",2026,4,2)]
    [InlineData("2026-04-02T00:30:00Z",2026,4,2)]
    [InlineData("2026-04-30T18:05:00Z",2026,5,1)]
    [InlineData("2024-02-29T18:05:00Z",2024,3,1)]
    public void Calendar_UtcBoundary_MapsToVietnamDate(string instant,int year,int month,int day) =>
        Assert.Equal(new DateOnly(year,month,day),CalendarMath.BusinessDate(DateTimeOffset.Parse(instant,CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData("01:25:00","10:35:00",28800)] // 08:25 to 17:35 local, no automatic OT.
    [InlineData("03:00:00","12:00:00",23400)] // T00 10:00 to 19:00 -> 390 minutes.
    [InlineData("04:30:00","05:30:00",1800)] // 11:30 to 12:30 -> 30 minutes.
    [InlineData("05:15:00","06:15:00",900)]
    [InlineData("01:30:30","10:30:00",28770)] // Preserve the half minute.
    [InlineData("00:00:00","01:00:00",0)]
    [InlineData("11:00:00","12:00:00",0)]
    public void Calendar_EligibleMinutes_ClipsShiftAndSubtractsOnlyBreakOverlap(string start,string end,int seconds) =>
        Assert.Equal(seconds/60m, CalendarMath.EligibleMinutes(Definition(),new DateOnly(2026,4,1),
            DateTimeOffset.Parse("2026-04-01T"+start+"Z",CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-04-01T"+end+"Z",CultureInfo.InvariantCulture)));

    [Fact]
    public void Calendar_IncompleteAndWeekendHoliday_DoNotCreateStandardWork()
    {
        var def=Definition("overlapping-holiday",new DateOnly(2026,4,26),new DateOnly(2026,4,26));
        Assert.Equal(0m,CalendarMath.StandardMinutes(def,new DateOnly(2026,4,26)));
        Assert.Equal(0m,CalendarMath.EligibleMinutes(def,new DateOnly(2026,4,1),DateTimeOffset.Parse("2026-04-01T01:30:00Z"),null));
        Assert.Equal(0m,CalendarMath.EligibleMinutes(def,new DateOnly(2026,4,26),DateTimeOffset.Parse("2026-04-26T01:30:00Z"),DateTimeOffset.Parse("2026-04-26T10:30:00Z")));
    }

    [Theory]
    [InlineData(2026,4,20)]
    [InlineData(2026,5,20)]
    [InlineData(2026,6,22)]
    [InlineData(2024,2,21)]
    public void Calendar_StandardMonth_MatchesApprovedFixturesAndLeapYear(int year,int month,int expectedDays)
    {
        var def=Definition("leap-year-standard");
        if (year==2026)
        {
            var fixtureFile=Path.Combine(ApiFactory.RepoRoot,"docs/fixtures/work-calendars.demo.json");
            using var document=System.Text.Json.JsonDocument.Parse(File.ReadAllText(fixtureFile));
            var fixture=document.RootElement.GetProperty("fixtures").EnumerateArray().Single(f=>f.GetProperty("effectiveFrom").GetString()==$"2026-{month:D2}-01");
            def=System.Text.Json.JsonSerializer.Deserialize<WorkCalendarDefinition>(fixture.GetProperty("definition").GetRawText(),new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
            Assert.Equal($"FIXTURE-CAL-2026-{month:D2}-A",def.FixtureId);
        }
        var days=Enumerable.Range(1,DateTime.DaysInMonth(year,month)).Select(d=>new DateOnly(year,month,d));
        Assert.Equal(expectedDays*480m,days.Sum(d=>CalendarMath.StandardMinutes(def,d)));
    }

    [Fact]
    public void Calendar_AprilDemoWithoutHolidays_Has22Days()
    {
        var def=Definition("FIXTURE-CAL-2026-04-DEMO-22D");
        Assert.Equal(10560m,Enumerable.Range(1,30).Sum(day=>CalendarMath.StandardMinutes(def,new DateOnly(2026,4,day))));
    }

    [Fact]
    public void Calendar_InvalidShiftBreaksAndAttendance_FailClosed()
    {
        var def=Definition();
        Assert.Throws<DomainRuleException>(()=>CalendarMath.Validate(def with {ShiftStart=new TimeOnly(22,0),ShiftEnd=new TimeOnly(6,0)}));
        Assert.Throws<DomainRuleException>(()=>CalendarMath.Validate(def with {Breaks=[new(new(12,0),new(13,0)),new(new(12,30),new(13,30))]}));
        Assert.Throws<DomainRuleException>(()=>CalendarMath.Validate(def with {Breaks=[new(new(7,0),new(8,0))]}));
        Assert.Throws<DomainRuleException>(()=>CalendarMath.Validate(def with {TimeZone="UTC"}));
        Assert.Throws<DomainRuleException>(()=>CalendarMath.EligibleMinutes(def,new(2026,4,1),DateTimeOffset.Parse("2026-04-01T10:30:00Z"),DateTimeOffset.Parse("2026-04-01T01:30:00Z")));
        Assert.Throws<DomainRuleException>(()=>CalendarMath.EligibleMinutes(def,new(2026,4,1),DateTimeOffset.Parse("2026-04-01T10:30:00Z"),DateTimeOffset.Parse("2026-04-01T18:30:00Z")));
    }
}
