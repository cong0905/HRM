using System.Globalization;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Domain;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Calendar;
using HRM.Next.Domain.Payroll;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Calendar;

public sealed class WorkCalendarService(HrmDbContext db, IAuditRepository audit,
    ISecurityContext security, TimeProvider clock) : IWorkCalendarService
{
    public async Task<PeriodEditabilityResponse> EditabilityAsync(int year, int month, CancellationToken token)
    {
        if (year is < 1900 or > 9998 || month is < 1 or > 12)
            throw new RequestValidationException("calendar.invalid_month", "Invalid calendar month.");
        var code = new DateOnly(year, month, 1).ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var dash = code + "-";
        var underscore = code + "_";
        var closed = await db.PayrollPeriods.AsNoTracking().AnyAsync(p =>
            (p.PeriodCode == code || p.PeriodCode.StartsWith(dash) || p.PeriodCode.StartsWith(underscore)) &&
            (p.Status == "Published" || p.Status == "Locked"), token);
        return new(year, month, closed);
    }
    private async Task LockAsync(CancellationToken token)
    {
        // Serialize configuration writes and period captures across app instances on the same database.
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sys.sp_getapplock @Resource=N'HRM.Calendar.Configuration', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000; IF @r < 0 THROW 51009, 'Calendar configuration busy.', 1;", token);
    }

    private Task AuditAsync(string action, Guid id, string reason, CancellationToken token) => audit.RecordAsync(new AuditEvent
    {
        Id = Guid.NewGuid(), OccurredAtUtc = clock.GetUtcNow(), ActorId = security.CurrentUserId,
        ActorUsername = security.CurrentUsername, TraceId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString(),
        Action = action, EntityName = "WorkCalendar", EntityId = id.ToString(),
        Reason = AuditMasker.SanitizeFreeText(reason), Payload = "{}"
    }, token);

    public async Task<CalendarVersionResponse> CreateAsync(CreateCalendarVersionRequest request, CancellationToken token)
    {
        CalendarMath.Validate(request.Definition);
        if (request.EffectiveFrom.Year is < 1900 or > 9998)
            throw new DomainRuleException("calendar.invalid_effective_date", "Effective date is outside the supported range.");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockAsync(token);
        if (await db.WorkCalendarVersions.AnyAsync(v => v.EffectiveFrom == request.EffectiveFrom, token))
            throw new ResourceConflictException("calendar.duplicate_effective_date", "A version already starts on this date.");
        var closed = await db.CalendarPeriodSnapshots.Join(db.PayrollPeriods,
            s => s.PayrollPeriodId, p => p.Id, (s,p) => new {s.Year,s.Month,p.Status})
            .Where(x => x.Status == "Published" || x.Status == "Locked")
            .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month).FirstOrDefaultAsync(token);
        if (closed is not null && request.EffectiveFrom <= new DateOnly(closed.Year,closed.Month,DateTime.DaysInMonth(closed.Year,closed.Month)))
            throw new ResourceConflictException("calendar.closed_period", "A new version cannot change the effective range of a closed period.");
        var version = new WorkCalendarVersion { Id = Guid.NewGuid(), EffectiveFrom = request.EffectiveFrom,
            DefinitionJson = JsonSerializer.Serialize(request.Definition), CreatedAtUtc = clock.GetUtcNow(), CreatedBy = security.CurrentUserId };
        db.WorkCalendarVersions.Add(version);
        await db.SaveChangesAsync(token);
        await AuditAsync("calendar.version_created", version.Id, request.Reason, token);
        await transaction.CommitAsync(token);
        return new(version.Id, version.EffectiveFrom, request.Definition, version.CreatedAtUtc);
    }

    private static CalendarDayResponse Day(WorkCalendarVersion version, DateOnly date)
    {
        var definition = JsonSerializer.Deserialize<WorkCalendarDefinition>(version.DefinitionJson)
            ?? throw new DomainRuleException("calendar.invalid_definition", "Calendar definition is unavailable.");
        return new(date, version.Id, definition, CalendarMath.StandardMinutes(definition,date));
    }

    public async Task<CalendarDayResponse> DayAsync(DateOnly date, CancellationToken token)
    {
        var version = await db.WorkCalendarVersions.AsNoTracking().Where(v => v.EffectiveFrom <= date)
            .OrderByDescending(v => v.EffectiveFrom).FirstOrDefaultAsync(token);
        if (version is null) throw new DomainRuleException("calendar.not_configured", "No calendar covers this business date.");
        return Day(version,date);
    }

    public async Task<CalendarMonthResponse> MonthAsync(int year, int month, CancellationToken token)
    {
        if (year is < 1900 or > 9998 || month is < 1 or > 12)
            throw new DomainRuleException("calendar.invalid_month", "Invalid calendar month.");
        var start = new DateOnly(year,month,1);
        var end = start.AddMonths(1).AddDays(-1);
        // At most one initial version plus one change per day; do not load the entire version history.
        var versions = await db.WorkCalendarVersions.AsNoTracking().Where(v => v.EffectiveFrom <= start)
            .OrderByDescending(v => v.EffectiveFrom).Take(1).ToListAsync(token);
        versions.AddRange(await db.WorkCalendarVersions.AsNoTracking().Where(v => v.EffectiveFrom > start && v.EffectiveFrom <= end)
            .OrderBy(v => v.EffectiveFrom).ToListAsync(token));
        if (versions.Count == 0 || versions[0].EffectiveFrom > start)
            throw new DomainRuleException("calendar.not_configured", "No calendar covers the start of this month.");
        var days = new List<CalendarDayResponse>();
        for (var date = start; date <= end; date = date.AddDays(1))
            days.Add(Day(versions.Last(v => v.EffectiveFrom <= date),date));
        return new(year,month,days.Count(d => d.StandardMinutes > 0m),days.Sum(d => d.StandardMinutes),days);
    }

    private static CalendarPeriodResponse View(CalendarPeriodSnapshot snapshot) => new(snapshot.PayrollPeriodId,
        snapshot.CreatedAtUtc, JsonSerializer.Deserialize<CalendarMonthResponse>(snapshot.ContentJson)!);

    public async Task<CalendarPeriodResponse?> ReadSnapshotAsync(Guid periodId, CancellationToken token)
    {
        var snapshot = await db.CalendarPeriodSnapshots.AsNoTracking().SingleOrDefaultAsync(s => s.PayrollPeriodId == periodId,token);
        return snapshot is null ? null : View(snapshot);
    }

    public async Task<CalendarPeriodResponse?> CaptureSnapshotAsync(Guid periodId, CancellationToken token)
    {
        await using var owned = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(token) : null;
        await LockAsync(token);
        var existing = await ReadSnapshotAsync(periodId,token);
        if (existing is not null) { if (owned is not null) await owned.CommitAsync(token); return existing; }
        var period = await db.PayrollPeriods.FromSqlInterpolated($"SELECT * FROM [payroll].[PayrollPeriods] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={periodId}")
            .AsNoTracking().SingleOrDefaultAsync(token);
        if (period is null) return null;
        if (period.Status is "Published" or "Locked")
            throw new ResourceConflictException("calendar.closed_period", "Cannot create a new snapshot for an already closed period.");
        if (!TryMonth(period.PeriodCode,out var date))
            throw new DomainRuleException("calendar.invalid_period_code", "Calendar periods require a yyyy-MM code.");
        var calendar = await MonthAsync(date.Year,date.Month,token);
        var snapshot = new CalendarPeriodSnapshot {Id=Guid.NewGuid(),PayrollPeriodId=periodId,Year=date.Year,Month=date.Month,
            ContentJson=JsonSerializer.Serialize(calendar),CreatedAtUtc=clock.GetUtcNow()};
        db.CalendarPeriodSnapshots.Add(snapshot);
        await db.SaveChangesAsync(token);
        await AuditAsync("calendar.snapshot_created",snapshot.Id,"Capture calendar for payroll calculation",token);
        if (owned is not null) await owned.CommitAsync(token);
        return View(snapshot);
    }

    private static bool TryMonth(string code, out DateOnly date) => DateOnly.TryParseExact(code + "-01",
        "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    public async Task CaptureForCalculationAsync(Guid periodId, string periodCode, CancellationToken token)
    {
        // T07 has named, non-month demo records and no real payroll calculator. Keep their existing
        // authorization regression behavior; real yyyy-MM records pin a configured T09 calendar.
        if (TryMonth(periodCode,out _) && await db.WorkCalendarVersions.AnyAsync(token))
            await CaptureSnapshotAsync(periodId,token);
    }
}
