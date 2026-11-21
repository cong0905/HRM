namespace HRM.Next.Domain.Payroll;

public static class PayrollCalculator
{
    private const decimal MaxMoney = 1_000_000_000_000m; // 1e12
    private const decimal MaxMinutes = 1440m; // 24 hours * 60 minutes

    public static PayrollCalculationOutput Calculate(PayrollCalculationInput input)
    {
        ValidateInput(input);

        try
        {
            return checked(ExecuteCalculation(input));
        }
        catch (OverflowException ex)
        {
            throw new DomainRuleException("payroll.overflow", $"Arithmetic overflow during payroll calculation: {ex.Message}");
        }
    }

    private static void ValidateInput(PayrollCalculationInput input)
    {
        if (input == null)
            throw new DomainRuleException("payroll.null_input", "Calculation input cannot be null.");

        if (input.Year < 2000 || input.Year > 2100)
            throw new DomainRuleException("payroll.invalid_year", $"Year {input.Year} is not supported (supported range 2000..2100).");

        if (input.Month < 1 || input.Month > 12)
            throw new DomainRuleException("payroll.invalid_month", $"Month {input.Month} is not supported (supported range 1..12).");

        if (input.Calendar == null)
            throw new DomainRuleException("payroll.null_calendar", "Calendar cannot be null.");

        if (input.Salaries == null)
            throw new DomainRuleException("payroll.null_salaries", "Salaries list cannot be null.");

        if (input.Days == null)
            throw new DomainRuleException("payroll.null_days", "Days list cannot be null.");

        if (input.Manual == null)
            throw new DomainRuleException("payroll.null_manual", "Manual amounts cannot be null.");

        if (input.Policy == null)
            throw new DomainRuleException("payroll.null_policy", "Policy cannot be null.");

        if (input.TerminationDate.HasValue && input.TerminationDate.Value < input.HireDate)
            throw new DomainRuleException("payroll.invalid_employment_period", $"Employment termination date ({input.TerminationDate.Value:yyyy-MM-dd}) cannot precede hire date ({input.HireDate:yyyy-MM-dd}).");

        // Validate Policy
        if (string.IsNullOrWhiteSpace(input.Policy.Version))
            throw new DomainRuleException("payroll.policy_invalid_version", "Policy version cannot be empty or whitespace.");

        if (input.Policy.WeekdayOvertimeMultiplier <= 0m || input.Policy.WeekdayOvertimeMultiplier > 10m)
            throw new DomainRuleException("payroll.policy_invalid_multiplier", $"Weekday overtime multiplier must be positive and <= 10, got {input.Policy.WeekdayOvertimeMultiplier}.");

        if (input.Policy.WeekendOvertimeMultiplier <= 0m || input.Policy.WeekendOvertimeMultiplier > 10m)
            throw new DomainRuleException("payroll.policy_invalid_multiplier", $"Weekend overtime multiplier must be positive and <= 10, got {input.Policy.WeekendOvertimeMultiplier}.");

        if (input.Policy.HolidayOvertimeMultiplier <= 0m || input.Policy.HolidayOvertimeMultiplier > 10m)
            throw new DomainRuleException("payroll.policy_invalid_multiplier", $"Holiday overtime multiplier must be positive and <= 10, got {input.Policy.HolidayOvertimeMultiplier}.");

        if (input.Policy.AutomaticPerformanceLinkEnabled)
            throw new DomainRuleException("payroll.performance_link_unsupported", "Automatic performance link is unsupported in MVP.");

        // Validate Manual Amounts
        if (input.Manual.Allowance < 0m || input.Manual.Allowance > MaxMoney)
            throw new DomainRuleException("payroll.manual_invalid_allowance", $"Allowance must be non-negative and <= 1e12, got {input.Manual.Allowance}.");

        if (input.Manual.Bonus < 0m || input.Manual.Bonus > MaxMoney)
            throw new DomainRuleException("payroll.manual_invalid_bonus", $"Bonus must be non-negative and <= 1e12, got {input.Manual.Bonus}.");

        if (input.Manual.OtherDeduction < 0m || input.Manual.OtherDeduction > MaxMoney)
            throw new DomainRuleException("payroll.manual_invalid_deduction", $"Other deduction must be non-negative and <= 1e12, got {input.Manual.OtherDeduction}.");

        if (input.Manual.DemoInsurance < 0m || input.Manual.DemoInsurance > MaxMoney)
            throw new DomainRuleException("payroll.manual_invalid_insurance", $"Demo insurance must be non-negative and <= 1e12, got {input.Manual.DemoInsurance}.");

        if (input.Manual.DemoTax < 0m || input.Manual.DemoTax > MaxMoney)
            throw new DomainRuleException("payroll.manual_invalid_tax", $"Demo tax must be non-negative and <= 1e12, got {input.Manual.DemoTax}.");

        // Validate Calendar: full month, unique date per day, correct month, nonnegative standard <= 1440, valid enum, sum standard > 0
        int daysInMonth = DateTime.DaysInMonth(input.Year, input.Month);
        if (input.Calendar.Count != daysInMonth)
            throw new DomainRuleException("payroll.calendar_incomplete", $"Calendar must contain exactly {daysInMonth} days for {input.Year:D4}-{input.Month:D2}, but contained {input.Calendar.Count}.");

        var calendarDates = new HashSet<DateOnly>();
        decimal sumStandard = 0m;
        foreach (var calDay in input.Calendar)
        {
            if (calDay == null)
                throw new DomainRuleException("payroll.null_calendar_day", "Calendar day item cannot be null.");

            if (calDay.Date.Year != input.Year || calDay.Date.Month != input.Month)
                throw new DomainRuleException("payroll.calendar_date_mismatch", $"Calendar date {calDay.Date:yyyy-MM-dd} does not belong to {input.Year:D4}-{input.Month:D2}.");

            if (!calendarDates.Add(calDay.Date))
                throw new DomainRuleException("payroll.calendar_duplicate_date", $"Duplicate calendar date {calDay.Date:yyyy-MM-dd}.");

            if (calDay.StandardMinutes < 0m || calDay.StandardMinutes > MaxMinutes)
                throw new DomainRuleException("payroll.calendar_invalid_standard_minutes", $"Calendar standard minutes must be between 0 and 1440, got {calDay.StandardMinutes}.");

            if (!Enum.IsDefined(typeof(PayrollDayKind), calDay.Kind))
                throw new DomainRuleException("payroll.calendar_invalid_day_kind", $"Invalid calendar day kind: {calDay.Kind}.");

            sumStandard += calDay.StandardMinutes;
        }

        if (sumStandard <= 0m)
            throw new DomainRuleException("payroll.calendar_zero_standard_minutes", "Total standard minutes in calendar month must be positive.");

        // Validate Days: unique, correct month, nonnegative attendance <= 1440 and OT <= 1440
        var calendarDict = input.Calendar.ToDictionary(c => c.Date);
        var dayDates = new HashSet<DateOnly>();
        foreach (var day in input.Days)
        {
            if (day == null)
                throw new DomainRuleException("payroll.null_day_input", "Day input item cannot be null.");

            if (day.Date.Year != input.Year || day.Date.Month != input.Month)
                throw new DomainRuleException("payroll.day_date_mismatch", $"Day input date {day.Date:yyyy-MM-dd} does not belong to {input.Year:D4}-{input.Month:D2}.");

            if (!dayDates.Add(day.Date))
                throw new DomainRuleException("payroll.day_duplicate_date", $"Duplicate day input for date {day.Date:yyyy-MM-dd}.");

            if (day.AttendanceMinutes < 0m || day.AttendanceMinutes > MaxMinutes)
                throw new DomainRuleException("payroll.day_invalid_attendance_minutes", $"Attendance minutes must be between 0 and 1440, got {day.AttendanceMinutes}.");

            if (day.ApprovedOvertimeMinutes < 0m || day.ApprovedOvertimeMinutes > MaxMinutes)
                throw new DomainRuleException("payroll.day_invalid_overtime_minutes", $"Approved overtime minutes must be between 0 and 1440, got {day.ApprovedOvertimeMinutes}.");

            // Overtime > 0 with incomplete checkout or paid full-day leave on standard working day must be rejected
            if (day.ApprovedOvertimeMinutes > 0m)
            {
                if (!day.CheckoutComplete)
                    throw new DomainRuleException("payroll.invalid_overtime", $"Approved overtime on {day.Date:yyyy-MM-dd} requires completed attendance checkout.");

                if (day.PaidLeaveApproved && calendarDict[day.Date].StandardMinutes > 0m)
                    throw new DomainRuleException("payroll.invalid_overtime", $"Approved overtime on {day.Date:yyyy-MM-dd} cannot co-exist with paid full-day leave on a standard working day.");
            }
        }

        // Validate Salaries: positive monthly base <= 1e12, intervals inclusive, no overlap (including open-ended), valid end/start
        for (int i = 0; i < input.Salaries.Count; i++)
        {
            var s = input.Salaries[i];
            if (s == null)
                throw new DomainRuleException("payroll.null_salary_rate", "Salary rate item cannot be null.");

            if (s.MonthlyBase <= 0m || s.MonthlyBase > MaxMoney)
                throw new DomainRuleException("payroll.salary_invalid_base", $"Salary monthly base must be positive and <= 1e12, got {s.MonthlyBase}.");

            if (s.EffectiveTo.HasValue && s.EffectiveTo.Value < s.EffectiveFrom)
                throw new DomainRuleException("payroll.salary_invalid_range", $"Salary EffectiveTo ({s.EffectiveTo.Value:yyyy-MM-dd}) cannot precede EffectiveFrom ({s.EffectiveFrom:yyyy-MM-dd}).");
        }

        for (int i = 0; i < input.Salaries.Count; i++)
        {
            var s1 = input.Salaries[i];
            var end1 = s1.EffectiveTo ?? DateOnly.MaxValue;

            for (int j = i + 1; j < input.Salaries.Count; j++)
            {
                var s2 = input.Salaries[j];
                var end2 = s2.EffectiveTo ?? DateOnly.MaxValue;

                if (s1.EffectiveFrom <= end2 && s2.EffectiveFrom <= end1)
                {
                    throw new DomainRuleException("payroll.salary_overlap", $"Salary intervals overlap: [{s1.EffectiveFrom:yyyy-MM-dd}..{s1.EffectiveTo?.ToString("yyyy-MM-dd") ?? "open"}] and [{s2.EffectiveFrom:yyyy-MM-dd}..{s2.EffectiveTo?.ToString("yyyy-MM-dd") ?? "open"}].");
                }
            }
        }
    }

    private static PayrollCalculationOutput ExecuteCalculation(PayrollCalculationInput input)
    {
        decimal standardMonthMinutes = input.Calendar.Sum(c => c.StandardMinutes);

        var daysDict = input.Days.ToDictionary(d => d.Date);
        var sortedCalendar = input.Calendar.OrderBy(c => c.Date).ToList();

        decimal totalCreditedMinutes = 0m;
        decimal totalApprovedOvertimeMinutes = 0m;
        decimal numeratorStandard = 0m;
        decimal numeratorOt = 0m;

        var blockingReasons = new List<string>();

        foreach (var calDay in sortedCalendar)
        {
            bool isEmployed = calDay.Date >= input.HireDate &&
                             (!input.TerminationDate.HasValue || calDay.Date <= input.TerminationDate.Value);

            daysDict.TryGetValue(calDay.Date, out var dayInput);

            // Incomplete attendance check on employed days
            if (isEmployed && dayInput != null && !dayInput.CheckoutComplete)
            {
                if (!blockingReasons.Contains("payroll.incomplete_attendance"))
                {
                    blockingReasons.Add("payroll.incomplete_attendance");
                }
            }

            // Outside employment earns 0 (including OT/leave)
            if (!isEmployed)
            {
                continue;
            }

            decimal attendanceMinutes = 0m;
            if (dayInput != null && dayInput.CheckoutComplete)
            {
                attendanceMinutes = dayInput.AttendanceMinutes;
            }

            decimal leaveMinutes = 0m;
            if (dayInput != null && dayInput.PaidLeaveApproved)
            {
                leaveMinutes = calDay.StandardMinutes;
            }

            decimal aggregateMinutes = attendanceMinutes + leaveMinutes;
            decimal dayCreditedMinutes = Math.Min(calDay.StandardMinutes, aggregateMinutes);

            decimal dayApprovedOT = dayInput != null ? dayInput.ApprovedOvertimeMinutes : 0m;

            // For each employed day inclusive hire..termination, when standard>0 or approvedOT>0 exactly one salary must cover that date or throw payroll.salary_missing
            if (calDay.StandardMinutes > 0m || dayApprovedOT > 0m)
            {
                PayrollSalaryRate? coveringSalary = null;
                foreach (var s in input.Salaries)
                {
                    if (s.EffectiveFrom <= calDay.Date && (!s.EffectiveTo.HasValue || calDay.Date <= s.EffectiveTo.Value))
                    {
                        coveringSalary = s;
                        break;
                    }
                }

                if (coveringSalary == null)
                {
                    throw new DomainRuleException("payroll.salary_missing", $"No salary rate covers employed date {calDay.Date:yyyy-MM-dd}.");
                }

                if (dayCreditedMinutes > 0m)
                {
                    numeratorStandard += coveringSalary.MonthlyBase * dayCreditedMinutes;
                }

                if (dayApprovedOT > 0m)
                {
                    decimal multiplier = calDay.Kind switch
                    {
                        PayrollDayKind.Weekday => input.Policy.WeekdayOvertimeMultiplier,
                        PayrollDayKind.Weekend => input.Policy.WeekendOvertimeMultiplier,
                        PayrollDayKind.Holiday => input.Policy.HolidayOvertimeMultiplier,
                        _ => throw new DomainRuleException("payroll.calendar_invalid_day_kind", $"Unsupported day kind: {calDay.Kind}.")
                    };

                    numeratorOt += coveringSalary.MonthlyBase * dayApprovedOT * multiplier;
                }
            }

            totalCreditedMinutes += dayCreditedMinutes;
            totalApprovedOvertimeMinutes += dayApprovedOT;
        }

        decimal unroundedStandardPay = numeratorStandard / standardMonthMinutes;
        decimal unroundedOvertimePay = numeratorOt / standardMonthMinutes;

        decimal unroundedGross = unroundedStandardPay + unroundedOvertimePay + input.Manual.Allowance + input.Manual.Bonus;
        decimal unroundedNet = unroundedGross - input.Manual.OtherDeduction - input.Manual.DemoInsurance - input.Manual.DemoTax;

        if (unroundedNet < 0m)
        {
            if (!blockingReasons.Contains("payroll.negative_net"))
            {
                blockingReasons.Add("payroll.negative_net");
            }
        }

        decimal standardPay = decimal.Round(unroundedStandardPay, 0, MidpointRounding.AwayFromZero);
        decimal overtimePay = decimal.Round(unroundedOvertimePay, 0, MidpointRounding.AwayFromZero);
        decimal allowance = decimal.Round(input.Manual.Allowance, 0, MidpointRounding.AwayFromZero);
        decimal bonus = decimal.Round(input.Manual.Bonus, 0, MidpointRounding.AwayFromZero);
        decimal otherDeduction = decimal.Round(input.Manual.OtherDeduction, 0, MidpointRounding.AwayFromZero);
        decimal demoInsurance = decimal.Round(input.Manual.DemoInsurance, 0, MidpointRounding.AwayFromZero);
        decimal demoTax = decimal.Round(input.Manual.DemoTax, 0, MidpointRounding.AwayFromZero);

        decimal gross = decimal.Round(unroundedGross, 0, MidpointRounding.AwayFromZero);
        decimal net = decimal.Round(unroundedNet, 0, MidpointRounding.AwayFromZero);

        decimal signedLinesSum = standardPay + overtimePay + allowance + bonus - otherDeduction - demoInsurance - demoTax;
        decimal roundingAdjustment = net - signedLinesSum;

        var lines = new List<PayrollLine>
        {
            new("StandardPay", standardPay),
            new("OvertimePay", overtimePay),
            new("Allowance", allowance),
            new("Bonus", bonus),
            new("OtherDeduction", -otherDeduction),
            new("DemoInsurance", -demoInsurance),
            new("DemoTax", -demoTax),
            new("RoundingAdjustment", roundingAdjustment)
        };

        bool canPublish = blockingReasons.Count == 0;

        return new PayrollCalculationOutput(
            PolicyVersion: input.Policy.Version,
            StandardMonthMinutes: standardMonthMinutes,
            CreditedMinutes: totalCreditedMinutes,
            ApprovedOvertimeMinutes: totalApprovedOvertimeMinutes,
            UnroundedStandardPay: unroundedStandardPay,
            UnroundedOvertimePay: unroundedOvertimePay,
            UnroundedGross: unroundedGross,
            UnroundedNet: unroundedNet,
            StandardPay: standardPay,
            OvertimePay: overtimePay,
            Gross: gross,
            Net: net,
            RoundingAdjustment: roundingAdjustment,
            Lines: lines,
            BlockingReasons: blockingReasons,
            CanPublish: canPublish
        );
    }
}
