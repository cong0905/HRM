using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Calendar;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class AttendanceMathTests
{
    [Theory]
    [InlineData("03:00:00","12:00:00",390,90,0)]
    [InlineData("01:30:00","05:30:00",210,0,270)]
    [InlineData("05:30:00","10:30:00",270,210,0)]
    [InlineData("01:30:00","10:30:00",480,0,0)]
    [InlineData("01:30:30","10:30:00",479.5,0.5,0)]
    public void Attendance_Metrics_CountOnlyWorkingIntervals(string start,string end,double eligible,double late,double early)
    {
        var value=AttendanceMath.Calculate(CalendarMathTests.Definition(),new(2026,4,1),
            DateTimeOffset.Parse("2026-04-01T"+start+"Z"),DateTimeOffset.Parse("2026-04-01T"+end+"Z"));
        Assert.Equal((decimal)eligible,value.EligibleMinutes);Assert.Equal((decimal)late,value.LateMinutes);Assert.Equal((decimal)early,value.EarlyLeaveMinutes);
    }
    [Fact]
    public void Attendance_MissingCheckout_AndHoliday_DoNotEarnStandardMinutes()
    {
        var def=CalendarMathTests.Definition("holiday",new DateOnly(2026,4,1));
        Assert.Equal(0m,AttendanceMath.Calculate(def,new(2026,4,1),DateTimeOffset.Parse("2026-04-01T01:30:00Z"),DateTimeOffset.Parse("2026-04-01T10:30:00Z")).EligibleMinutes);
        Assert.Equal(0m,AttendanceMath.Calculate(CalendarMathTests.Definition(),new(2026,4,1),DateTimeOffset.Parse("2026-04-01T01:30:00Z"),null).EligibleMinutes);
        var summary=new AttendanceSummary{BusinessDate=new(2026,4,1),CheckInUtc=DateTimeOffset.Parse("2026-04-01T01:30:00Z")};
        Assert.Equal("CheckedIn",AttendanceMath.Status(summary,new(2026,4,1)));
        Assert.Equal("MissingCheckout",AttendanceMath.Status(summary,new(2026,4,2)));
    }
}
