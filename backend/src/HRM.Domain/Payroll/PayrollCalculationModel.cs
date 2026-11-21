namespace HRM.Next.Domain.Payroll;

public enum PayrollDayKind { Weekday, Weekend, Holiday }
public sealed record PayrollPolicy(string Version, decimal WeekdayOvertimeMultiplier=1.5m,
    decimal WeekendOvertimeMultiplier=2m, decimal HolidayOvertimeMultiplier=3m,
    bool AutomaticPerformanceLinkEnabled=false);
public sealed record PayrollCalendarDay(DateOnly Date,decimal StandardMinutes,PayrollDayKind Kind);
public sealed record PayrollSalaryRate(DateOnly EffectiveFrom,DateOnly? EffectiveTo,decimal MonthlyBase);
public sealed record PayrollDayInput(DateOnly Date,decimal AttendanceMinutes,bool CheckoutComplete,
    bool PaidLeaveApproved,decimal ApprovedOvertimeMinutes);
public sealed record PayrollManualAmounts(decimal Allowance=0m,decimal Bonus=0m,
    decimal OtherDeduction=0m,decimal DemoInsurance=0m,decimal DemoTax=0m);
public sealed record PayrollCalculationInput(int Year,int Month,DateOnly HireDate,DateOnly? TerminationDate,
    IReadOnlyList<PayrollCalendarDay> Calendar,IReadOnlyList<PayrollSalaryRate> Salaries,
    IReadOnlyList<PayrollDayInput> Days,PayrollManualAmounts Manual,PayrollPolicy Policy,
    decimal? PerformanceScore=null);
public sealed record PayrollLine(string Code,decimal Amount);
public sealed record PayrollCalculationOutput(string PolicyVersion,decimal StandardMonthMinutes,
    decimal CreditedMinutes,decimal ApprovedOvertimeMinutes,decimal UnroundedStandardPay,
    decimal UnroundedOvertimePay,decimal UnroundedGross,decimal UnroundedNet,
    decimal StandardPay,decimal OvertimePay,decimal Gross,decimal Net,decimal RoundingAdjustment,
    IReadOnlyList<PayrollLine> Lines,IReadOnlyList<string> BlockingReasons,bool CanPublish);
