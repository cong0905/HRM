using System.Globalization;
using System.Text.Json;
using HRM.Next.Domain;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Payroll;
using Xunit;

namespace HRM.Next.Tests.Integration;

public sealed class PayrollCalculatorTests
{
    private static readonly JsonSerializerOptions Options=new(JsonSerializerDefaults.Web);
    public static PayrollCalculationInput Input(string fixture="FIXTURE-CAL-2026-04-A",decimal salary=20000000m)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiFactory.RepoRoot,"docs/fixtures/work-calendars.demo.json")));
        var item=doc.RootElement.GetProperty("fixtures").EnumerateArray().Single(x=>x.GetProperty("definition").GetProperty("fixtureId").GetString()==fixture);
        var from=DateOnly.Parse(item.GetProperty("effectiveFrom").GetString()!,CultureInfo.InvariantCulture);var definition=item.GetProperty("definition").Deserialize<WorkCalendarDefinition>(Options)!;
        var calendar=Enumerable.Range(1,DateTime.DaysInMonth(from.Year,from.Month)).Select(i=>new DateOnly(from.Year,from.Month,i)).Select(d=>new PayrollCalendarDay(d,CalendarMath.StandardMinutes(definition,d),definition.Holidays.Contains(d)?PayrollDayKind.Holiday:d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday?PayrollDayKind.Weekend:PayrollDayKind.Weekday)).ToList();
        return new(from.Year,from.Month,new(from.Year,1,1),null,calendar,[new(new(from.Year,1,1),null,salary)],calendar.Where(x=>x.StandardMinutes>0).Select(d=>new PayrollDayInput(d.Date,d.StandardMinutes,true,false,0m)).ToList(),new(),new("POL-2026-V1"));
    }
    private static decimal Money(JsonElement x,string name)=>x.TryGetProperty(name,out var p)?decimal.Parse(p.GetString()!,CultureInfo.InvariantCulture):0m;
    [Theory]
    [InlineData("01")][InlineData("02")][InlineData("03")][InlineData("04")]
    [InlineData("07")][InlineData("08")][InlineData("09")][InlineData("10")]
    public void Payroll_Golden_T00_PayrollCases_MatchIndependentNumbers(string id)
    {
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(ApiFactory.RepoRoot,"docs/fixtures/payroll-golden.demo.json")));
        var row=doc.RootElement.GetProperty("cases").EnumerateArray().Single(x=>x.GetProperty("id").GetString()==id);
        var input=Input(row.GetProperty("calendar").GetString()!,Money(row,"base"));var days=input.Days.ToList();
        if(row.TryGetProperty("workedDays",out var worked))days=days.Take(worked.GetInt32()).ToList();
        if(row.TryGetProperty("paidLeaveStart",out var leaveStart))
        {var from=DateOnly.Parse(leaveStart.GetString()!,CultureInfo.InvariantCulture);var to=DateOnly.Parse(row.GetProperty("paidLeaveEnd").GetString()!,CultureInfo.InvariantCulture);days=days.Select(d=>d.Date>=from&&d.Date<=to?d with{AttendanceMinutes=0m,PaidLeaveApproved=true}:d).ToList();Assert.Equal(4,days.Count(x=>x.PaidLeaveApproved));}
        if(row.TryGetProperty("weekendOtMinutes",out _))days.Add(new(new(2026,4,18),0m,true,false,Money(row,"weekendOtMinutes")));
        if(row.TryGetProperty("newFrom",out var newFrom))
        {var start=DateOnly.Parse(newFrom.GetString()!,CultureInfo.InvariantCulture);input=input with{Salaries=[new(new(2026,1,1),start.AddDays(-1),Money(row,"base")),new(start,null,Money(row,"newBase"))]};}
        input=input with{Days=days,Manual=new(Money(row,"allowance"),0m,Money(row,"deduction"),Money(row,"insurance"),Money(row,"tax"))};
        var actual=PayrollCalculator.Calculate(input);Assert.Equal(Money(row,"expectedStandard"),actual.StandardPay);Assert.Equal(Money(row,"expectedOt"),actual.OvertimePay);Assert.Equal(Money(row,"expectedGross"),actual.Gross);Assert.Equal(Money(row,"expectedNet"),actual.Net);Assert.Equal(actual.Net,actual.Lines.Sum(x=>x.Amount));Assert.Equal(id!="10",actual.CanPublish);
    }
    [Fact]
    public void Payroll_MissingCheckout_CreditsZeroAndBlocksUntilCorrected()
    {
        var input=Input(salary:16000000m);var days=input.Days.ToList();days[0]=days[0] with{CheckoutComplete=false};
        var before=PayrollCalculator.Calculate(input with{Days=days});Assert.Equal(15200000m,before.StandardPay);Assert.False(before.CanPublish);Assert.Contains("payroll.incomplete_attendance",before.BlockingReasons);
        var after=PayrollCalculator.Calculate(input);Assert.Equal(16000000m,after.StandardPay);Assert.True(after.CanPublish);
    }
    [Theory][InlineData(true,"9600000")][InlineData(false,"6000000")]
    public void Payroll_EmploymentJoinOrLeaveMidMonth_UsesCompanyDenominator(bool join,string expected)
    {
        var input=Input("FIXTURE-CAL-2026-06-A",join?17600000m:13200000m);
        input=join?input with{HireDate=new(2026,6,15)}:input with{TerminationDate=new(2026,6,14)};
        var output=PayrollCalculator.Calculate(input);Assert.Equal(10560m,output.StandardMonthMinutes);Assert.Equal(decimal.Parse(expected,CultureInfo.InvariantCulture),output.StandardPay);
    }
    [Fact]
    public void Payroll_VariableShift_CapsDirtyAttendancePlusLeaveAt420()
    {
        var input=Input();var calendar=input.Calendar.Select(x=>x with{StandardMinutes=x.StandardMinutes>0?420m:0m}).ToList();
        var days=input.Days.Select(x=>x with{AttendanceMinutes=1440m,PaidLeaveApproved=true}).ToList();var output=PayrollCalculator.Calculate(input with{Calendar=calendar,Days=days});Assert.Equal(8400m,output.CreditedMinutes);Assert.Equal(20000000m,output.StandardPay);
    }
    [Fact]
    public void Payroll_PrecisionAndRoundingAdjustment_NoIntermediateMoneyRounding()
    {
        var input=Input(salary:1000000m);var output=PayrollCalculator.Calculate(input with{Days=[input.Days[0] with{AttendanceMinutes=.5m}]});Assert.Equal(1000000m*.5m/9600m,output.UnroundedStandardPay);Assert.Equal(52m,output.StandardPay);
        output=PayrollCalculator.Calculate(input with{Days=[],Manual=new(.49m,.49m)});Assert.Equal(1m,output.Net);Assert.Equal(1m,output.RoundingAdjustment);Assert.Equal(output.Net,output.Lines.Sum(x=>x.Amount));
    }
    [Theory][InlineData("0.49","0")][InlineData("0.5","-1")]
    public void Payroll_NegativeUnroundedNet_BlocksEvenWhenDisplayIsZero(string deduction,string expected)
    {var input=Input();var output=PayrollCalculator.Calculate(input with{Days=[],Manual=new(OtherDeduction:decimal.Parse(deduction,CultureInfo.InvariantCulture))});Assert.Equal(decimal.Parse(expected,CultureInfo.InvariantCulture),output.Net);Assert.False(output.CanPublish);}
    [Theory][InlineData(PayrollDayKind.Weekday,"300000")][InlineData(PayrollDayKind.Weekend,"400000")][InlineData(PayrollDayKind.Holiday,"600000")]
    public void Payroll_ConfirmedOvertime_AppliesExplicitDayKindOnly(PayrollDayKind kind,string expected)
    {
        var input=Input(salary:16000000m);var day=new DateOnly(2026,4,18);var calendar=input.Calendar.Select(x=>x.Date==day?x with{Kind=kind}:x).ToList();
        var output=PayrollCalculator.Calculate(input with{Calendar=calendar,Days=input.Days.Append(new(day,0m,true,false,120m)).ToList()});Assert.Equal(decimal.Parse(expected,CultureInfo.InvariantCulture),output.OvertimePay);
        Assert.Equal(0m,PayrollCalculator.Calculate(input).OvertimePay);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)][InlineData(7)]
    public void Payroll_InvalidOrMissingInputs_FailClosed(int variant)
    {
        var input=Input();input=variant switch
        {
            0=>input with{Calendar=input.Calendar.Select(x=>x with{StandardMinutes=0m}).ToList()},
            1=>input with{Salaries=[new(new(2026,1,1),null,1m),new(new(2026,4,1),null,2m)]},
            2=>input with{Salaries=[]},
            3=>input with{Calendar=input.Calendar.Skip(1).ToList()},
            4=>input with{Days=[input.Days[0],input.Days[0]]},
            5=>input with{Days=[input.Days[0] with{AttendanceMinutes=-1m}]},
            6=>input with{Policy=new("KPI-AUTO",AutomaticPerformanceLinkEnabled:true)},
            _=>input with{Days=[input.Days[0] with{PaidLeaveApproved=true,ApprovedOvertimeMinutes=10m}]}
        };Assert.StartsWith("payroll.",Assert.Throws<DomainRuleException>(()=>PayrollCalculator.Calculate(input)).Code);
    }
    [Theory][InlineData(null)][InlineData(0)][InlineData(100)]
    public void Payroll_PerformanceOff_IsNeutralRegardlessOfScore(int? score)
    {var input=Input(salary:15000000m);var output=PayrollCalculator.Calculate(input with{PerformanceScore=score,Manual=new(DemoInsurance:1575000m)});Assert.Equal(13425000m,output.Net);Assert.True(output.CanPublish);}
    [Theory][InlineData("1000000")][InlineData("10000001")][InlineData("13.37")][InlineData("0.02")]
    public void Payroll_FullMonthDeductedExactly_NetZeroIsPublishable(string salary)
    {var value=decimal.Parse(salary,CultureInfo.InvariantCulture);var input=Input("FIXTURE-CAL-2026-06-A",value);var output=PayrollCalculator.Calculate(input with{Manual=new(OtherDeduction:value)});Assert.Equal(0m,output.Net);Assert.True(output.CanPublish,$"Exact zero must not be blocked by accumulated decimal division error: rawNet={output.UnroundedNet}");}
}
