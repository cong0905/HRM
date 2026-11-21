using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Domain;
using HRM.Next.Domain.Attendance;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Calendar;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Attendance;

public sealed class AttendanceService(
    HrmDbContext db,
    ISecurityContext security,
    IWorkCalendarService calendars,
    BusinessClock clock,
    IAuditRepository audit) : IAttendanceService
{
    private static bool TryParseUuidV4(string? raw, out Guid guid)
    {
        guid = Guid.Empty;
        if (string.IsNullOrWhiteSpace(raw) || raw.Length != 36) return false;
        if (!Guid.TryParseExact(raw, "D", out guid)) return false;
        if (raw[14] != '4') return false;
        var variant = raw[19];
        return variant is '8' or '9' or 'a' or 'b' or 'A' or 'B';
    }

    private static string ComputeSha256Hex(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static byte[] ValidateRowVersion(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new RequestValidationException("concurrency.token_required", "RowVersion is required.");

        byte[] version;
        try
        {
            version = Convert.FromBase64String(raw.Trim());
        }
        catch (FormatException)
        {
            throw new RequestValidationException("concurrency.invalid_token", "Invalid Base64 RowVersion.");
        }

        if (version.Length != 8)
            throw new RequestValidationException("concurrency.invalid_token", "RowVersion must be 8 bytes.");

        return version;
    }

    private async Task EnsurePeriodNotClosedAsync(DateOnly day, CancellationToken token)
    {
        var monthCode = day.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var prefixDash = monthCode + "-";
        var prefixUnder = monthCode + "_";
        var isClosed = await db.PayrollPeriods.AsNoTracking().AnyAsync(p =>
            (p.PeriodCode == monthCode || p.PeriodCode.StartsWith(prefixDash) || p.PeriodCode.StartsWith(prefixUnder))
            && (p.Status == "Published" || p.Status == "Locked"), token);

        if (isClosed)
        {
            throw new ResourceConflictException("attendance.period_closed", "Attendance cannot be modified for a closed or published payroll period.");
        }
    }

    private Task RecordAuditEventAsync(
        string action,
        Guid entityId,
        string reason,
        object payload,
        DateTimeOffset occurredAt,
        CancellationToken token) =>
        audit.RecordAsync(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = occurredAt,
            ActorId = security.CurrentUserId,
            ActorUsername = security.CurrentUsername,
            TraceId = System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Action = action,
            EntityName = "AttendanceSummary",
            EntityId = entityId.ToString(),
            Reason = AuditMasker.SanitizeFreeText(reason),
            Payload = AuditMasker.MaskObject(payload)
        }, token);

    public async Task<AttendanceDayResponse?> GetOwnDayAsync(DateOnly? day, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue) return null;

        var targetDay = day ?? clock.Now().BusinessDate;

        var summary = await db.AttendanceSummaries.AsNoTracking()
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId.Value && s.BusinessDate == targetDay, token);

        if (summary is null) return null;

        var otMinutes = await db.AttendanceOvertimeEntries.AsNoTracking()
            .Where(ot => ot.AttendanceId == summary.Id)
            .SumAsync(ot => (decimal?)ot.Minutes, token) ?? 0m;

        var today = clock.Now().BusinessDate;
        var status = AttendanceMath.Status(summary, today);
        var rowVersionStr = summary.RowVersion is { Length: > 0 } ? Convert.ToBase64String(summary.RowVersion) : string.Empty;

        return new AttendanceDayResponse(
            summary.Id,
            summary.EmployeeId,
            summary.BusinessDate,
            summary.CheckInUtc,
            summary.CheckOutUtc,
            summary.EligibleMinutes,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            status,
            summary.CalendarVersionId,
            otMinutes,
            summary.UpdatedAtUtc,
            rowVersionStr);
    }

    public async Task<PagedResult<AttendanceDayResponse>> GetOwnHistoryAsync(int page, int pageSize, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.employee_profile_missing");

        var pageRequest = PageRequest.Create(page, pageSize);

        var query = db.AttendanceSummaries.AsNoTracking().Where(s => s.EmployeeId == employeeId.Value);
        return await ReadHistoryAsync(query, pageRequest, token);
    }

    public async Task<PagedResult<AttendanceDayResponse>> ListAsync(Guid? employeeId, DateOnly? from, DateOnly? to,
        string? status, int page, int pageSize, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");
        if (!security.HasPermission(HrmPermissions.AttendanceReadAll))
            throw new AttendanceAccessDeniedException("authorization.forbidden");
        if (from.HasValue && to.HasValue && from > to)
            throw new RequestValidationException("attendance.invalid_date_range", "From date must not follow to date.");
        var query = db.AttendanceSummaries.AsNoTracking();
        // Match the existing single-record read policy, including company-wide Payroll read.
        if (!security.HasPermission(HrmPermissions.PayrollReadAll))
        {
            var assignedIds = db.HrEmployeeAssignments.AsNoTracking().Where(a => a.HrUserId == security.CurrentUserId).Select(a => a.EmployeeId);
            if (employeeId.HasValue && !await assignedIds.ContainsAsync(employeeId.Value, token))
                throw new AttendanceAccessDeniedException("authorization.scope_forbidden");
            query = query.Where(s => assignedIds.Contains(s.EmployeeId));
        }
        if (employeeId.HasValue) query = query.Where(s => s.EmployeeId == employeeId.Value);
        if (from.HasValue) query = query.Where(s => s.BusinessDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.BusinessDate <= to.Value);
        var today = clock.Now().BusinessDate;
        query = status switch
        {
            null or "" => query,
            "MissingCheckout" => query.Where(s => s.CheckOutUtc == null && s.BusinessDate < today),
            "Corrected" => query.Where(s => s.CheckOutUtc != null && s.Phase == "Corrected"),
            "Complete" => query.Where(s => s.CheckOutUtc != null && s.Phase != "Corrected"),
            "CheckedIn" => query.Where(s => s.CheckOutUtc == null && s.BusinessDate >= today && s.Phase != "Corrected"),
            "Incomplete" => query.Where(s => s.CheckOutUtc == null && s.BusinessDate >= today && s.Phase == "Corrected"),
            _ => throw new RequestValidationException("attendance.invalid_status", "Unsupported attendance status.")
        };
        return await ReadHistoryAsync(query, PageRequest.Create(page, pageSize), token);
    }

    private async Task<PagedResult<AttendanceDayResponse>> ReadHistoryAsync(IQueryable<HRM.Next.Domain.Attendance.AttendanceSummary> source,
        PageRequest pageRequest, CancellationToken token)
    {
        var query = source.OrderByDescending(s => s.BusinessDate).ThenByDescending(s => s.Id);

        var total = await query.LongCountAsync(token);
        var summaries = await query.Skip(pageRequest.Offset).Take(pageRequest.PageSize).ToListAsync(token);

        var summaryIds = summaries.Select(s => s.Id).ToList();
        var otDict = await db.AttendanceOvertimeEntries.AsNoTracking()
            .Where(ot => summaryIds.Contains(ot.AttendanceId))
            .GroupBy(ot => ot.AttendanceId)
            .Select(g => new { AttendanceId = g.Key, Total = g.Sum(x => x.Minutes) })
            .ToDictionaryAsync(x => x.AttendanceId, x => x.Total, token);

        var today = clock.Now().BusinessDate;
        var items = summaries.Select(s =>
        {
            otDict.TryGetValue(s.Id, out var otMinutes);
            var status = AttendanceMath.Status(s, today);
            var rowVersionStr = s.RowVersion is { Length: > 0 } ? Convert.ToBase64String(s.RowVersion) : string.Empty;

            return new AttendanceDayResponse(
                s.Id,
                s.EmployeeId,
                s.BusinessDate,
                s.CheckInUtc,
                s.CheckOutUtc,
                s.EligibleMinutes,
                s.LateMinutes,
                s.EarlyLeaveMinutes,
                status,
                s.CalendarVersionId,
                otMinutes,
                s.UpdatedAtUtc,
                rowVersionStr);
        }).ToList();

        return new PagedResult<AttendanceDayResponse>(items, pageRequest.Page, pageRequest.PageSize, total);
    }

    public async Task<AttendanceDayResponse?> GetEmployeeDayAsync(Guid employeeId, DateOnly day, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        var hasPayrollRead = security.HasPermission(HrmPermissions.PayrollReadAll);
        var hasAttendanceAll = security.HasPermission(HrmPermissions.AttendanceReadAll);
        var hasAttendanceManage = security.HasPermission(HrmPermissions.AttendanceManage);

        if (hasPayrollRead && hasAttendanceAll)
        {
            // Company-wide payroll read
        }
        else if (hasAttendanceManage || hasAttendanceAll)
        {
            var isAssigned = await db.HrEmployeeAssignments.AsNoTracking()
                .AnyAsync(a => a.HrUserId == security.CurrentUserId.Value && a.EmployeeId == employeeId, token);
            if (!isAssigned)
                throw new AttendanceAccessDeniedException("authorization.scope_forbidden");
        }
        else
        {
            throw new AttendanceAccessDeniedException("authorization.forbidden");
        }

        var profileExists = await db.EmployeeProfiles.AsNoTracking().AnyAsync(p => p.Id == employeeId, token);
        if (!profileExists)
            throw new AttendanceNotFoundException("employee.not_found");

        var summary = await db.AttendanceSummaries.AsNoTracking()
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId && s.BusinessDate == day, token);

        if (summary is null) return null;

        var otMinutes = await db.AttendanceOvertimeEntries.AsNoTracking()
            .Where(ot => ot.AttendanceId == summary.Id)
            .SumAsync(ot => (decimal?)ot.Minutes, token) ?? 0m;

        var today = clock.Now().BusinessDate;
        var status = AttendanceMath.Status(summary, today);
        var rowVersionStr = summary.RowVersion is { Length: > 0 } ? Convert.ToBase64String(summary.RowVersion) : string.Empty;

        return new AttendanceDayResponse(
            summary.Id,
            summary.EmployeeId,
            summary.BusinessDate,
            summary.CheckInUtc,
            summary.CheckOutUtc,
            summary.EligibleMinutes,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            status,
            summary.CalendarVersionId,
            otMinutes,
            summary.UpdatedAtUtc,
            rowVersionStr);
    }

    public async Task<AttendanceDayResponse> CheckInAsync(AttendanceCommandRequest request, string key, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.employee_profile_missing");

        if (!TryParseUuidV4(key, out var keyGuid))
            throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

        var canonical = JsonSerializer.Serialize(new
        {
            employeeId = employeeId.Value,
            note = request.Note,
            clientTimeUtc = request.ClientTimeUtc?.ToUniversalTime()
        });
        var requestHash = ComputeSha256Hex(canonical);

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await AttendanceLocks.EmployeeAsync(db, employeeId.Value, token);

        var initialProfile = await db.EmployeeProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == employeeId.Value, token);
        if (initialProfile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {initialProfile.UserId}")
            .AsTracking().SingleOrDefaultAsync(token);
        if (user is null)
            throw new AttendanceNotFoundException("account.not_found");

        var profile = await db.EmployeeProfiles.AsTracking().SingleOrDefaultAsync(p => p.Id == employeeId.Value, token);
        if (profile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        if (!user.IsActive)
            throw new AttendanceAccessDeniedException("account.inactive");
        if (string.Equals(profile.Status, "DaNghiViec", StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("employees.terminated", "Terminated employees cannot perform operational activities.");

        var idemp = await db.AttendanceIdempotencyRecords
            .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "CheckIn" && r.Key == keyGuid, token);
        if (idemp is not null)
        {
            if (idemp.RequestHash == requestHash)
            {
                var replay = JsonSerializer.Deserialize<AttendanceDayResponse>(idemp.ResponseJson)!;
                await transaction.CommitAsync(token);
                return replay;
            }
            throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
        }

        var nowTime = clock.Now();
        var serverNow = nowTime.ServerTimeUtc;
        var today = nowTime.BusinessDate;

        if (profile.HireDate.HasValue && today < profile.HireDate.Value)
            throw new DomainRuleException("attendance.before_hire_date", "Cannot record attendance before hire date.");
        if (profile.TerminationDate.HasValue && today > profile.TerminationDate.Value)
            throw new DomainRuleException("employees.past_termination_date", "Operation date cannot be after employee termination date.");

        await AttendanceLocks.MonthAsync(db, today, false, token);
        await EnsurePeriodNotClosedAsync(today, token);

        var existing = await db.AttendanceSummaries.AsNoTracking()
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId.Value && s.BusinessDate == today, token);
        if (existing is not null)
            throw new ResourceConflictException("attendance.already_checked_in", "Employee is already checked in for today.");

        var calDay = await calendars.DayAsync(today, token);
        var metrics = AttendanceMath.Calculate(calDay.Definition, today, serverNow, null);
        await WorkLeaveInvariant.EnsureAttendanceAsync(db, employeeId.Value, today, metrics.EligibleMinutes, false, calDay.StandardMinutes, false, token);

        var summary = new AttendanceSummary
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId.Value,
            BusinessDate = today,
            CheckInUtc = serverNow,
            CheckOutUtc = null,
            CalendarVersionId = calDay.VersionId,
            CalendarDefinitionJson = JsonSerializer.Serialize(calDay.Definition),
            EligibleMinutes = metrics.EligibleMinutes,
            LateMinutes = metrics.LateMinutes,
            EarlyLeaveMinutes = metrics.EarlyLeaveMinutes,
            Phase = "Open",
            UpdatedAtUtc = serverNow
        };
        db.AttendanceSummaries.Add(summary);

        var evt = new AttendanceEvent
        {
            Id = Guid.NewGuid(),
            AttendanceId = summary.Id,
            ActorId = security.CurrentUserId.Value,
            Kind = "CheckIn",
            OccurredAtUtc = serverNow,
            EffectiveCheckInUtc = serverNow,
            EffectiveCheckOutUtc = null
        };
        db.AttendanceEvents.Add(evt);

        await RecordAuditEventAsync("attendance.check_in", summary.Id, request.Note ?? "Check in", new
        {
            attendanceId = summary.Id,
            employeeId = employeeId.Value,
            businessDate = today,
            checkInUtc = serverNow,
            clientTimeUtc = request.ClientTimeUtc
        }, serverNow, token);

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            throw new ResourceConflictException("resource.conflict", "Concurrency conflict: record has been modified.");
        }

        var status = AttendanceMath.Status(summary, today);
        var rowVersionStr = Convert.ToBase64String(summary.RowVersion);

        var response = new AttendanceDayResponse(
            summary.Id,
            summary.EmployeeId,
            summary.BusinessDate,
            summary.CheckInUtc,
            summary.CheckOutUtc,
            summary.EligibleMinutes,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            status,
            summary.CalendarVersionId,
            0m,
            summary.UpdatedAtUtc,
            rowVersionStr);

        var idempRecord = new AttendanceIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            UserId = security.CurrentUserId.Value,
            Operation = "CheckIn",
            Key = keyGuid,
            RequestHash = requestHash,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAtUtc = serverNow
        };
        db.AttendanceIdempotencyRecords.Add(idempRecord);
        await db.SaveChangesAsync(token);

        await transaction.CommitAsync(token);
        return response;
    }

    public async Task<AttendanceDayResponse> CheckOutAsync(AttendanceCommandRequest request, string key, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.employee_profile_missing");

        if (!TryParseUuidV4(key, out var keyGuid))
            throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

        var canonical = JsonSerializer.Serialize(new
        {
            employeeId = employeeId.Value,
            note = request.Note,
            clientTimeUtc = request.ClientTimeUtc?.ToUniversalTime()
        });
        var requestHash = ComputeSha256Hex(canonical);

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await AttendanceLocks.EmployeeAsync(db, employeeId.Value, token);

        var initialProfile = await db.EmployeeProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == employeeId.Value, token);
        if (initialProfile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {initialProfile.UserId}")
            .AsTracking().SingleOrDefaultAsync(token);
        if (user is null)
            throw new AttendanceNotFoundException("account.not_found");

        var profile = await db.EmployeeProfiles.AsTracking().SingleOrDefaultAsync(p => p.Id == employeeId.Value, token);
        if (profile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        if (!user.IsActive)
            throw new AttendanceAccessDeniedException("account.inactive");
        if (string.Equals(profile.Status, "DaNghiViec", StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("employees.terminated", "Terminated employees cannot perform operational activities.");

        var idemp = await db.AttendanceIdempotencyRecords
            .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "CheckOut" && r.Key == keyGuid, token);
        if (idemp is not null)
        {
            if (idemp.RequestHash == requestHash)
            {
                var replay = JsonSerializer.Deserialize<AttendanceDayResponse>(idemp.ResponseJson)!;
                await transaction.CommitAsync(token);
                return replay;
            }
            throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
        }

        var nowTime = clock.Now();
        var serverNow = nowTime.ServerTimeUtc;
        var today = nowTime.BusinessDate;

        if (profile.HireDate.HasValue && today < profile.HireDate.Value)
            throw new DomainRuleException("attendance.before_hire_date", "Cannot record attendance before hire date.");
        if (profile.TerminationDate.HasValue && today > profile.TerminationDate.Value)
            throw new DomainRuleException("employees.past_termination_date", "Operation date cannot be after employee termination date.");

        await AttendanceLocks.MonthAsync(db, today, false, token);
        await EnsurePeriodNotClosedAsync(today, token);

        var summary = await db.AttendanceSummaries.AsTracking()
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId.Value && s.BusinessDate == today, token);
        if (summary is null)
            throw new ResourceConflictException("attendance.not_checked_in", "No check-in record found for today.");

        if (summary.Phase == "Corrected")
            throw new ResourceConflictException("attendance.manually_corrected", "Only HR can amend a manually corrected attendance day.");

        if (summary.CheckOutUtc.HasValue)
            throw new ResourceConflictException("attendance.already_checked_out", "Employee is already checked out for today.");

        if (serverNow < summary.CheckInUtc)
            throw new DomainRuleException("attendance.invalid_interval", "Check-out cannot precede check-in.");

        var definition = JsonSerializer.Deserialize<WorkCalendarDefinition>(summary.CalendarDefinitionJson)
            ?? throw new DomainRuleException("calendar.invalid_definition", "Stored calendar definition is unavailable.");

        var metrics = AttendanceMath.Calculate(definition, today, summary.CheckInUtc, serverNow);
        await WorkLeaveInvariant.EnsureAttendanceAsync(db, employeeId.Value, today, metrics.EligibleMinutes, true, CalendarMath.StandardMinutes(definition, today), false, token);

        summary.CheckOutUtc = serverNow;
        summary.EligibleMinutes = metrics.EligibleMinutes;
        summary.LateMinutes = metrics.LateMinutes;
        summary.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
        summary.Phase = "Complete";
        summary.UpdatedAtUtc = serverNow;

        var evt = new AttendanceEvent
        {
            Id = Guid.NewGuid(),
            AttendanceId = summary.Id,
            ActorId = security.CurrentUserId.Value,
            Kind = "CheckOut",
            OccurredAtUtc = serverNow,
            EffectiveCheckInUtc = summary.CheckInUtc,
            EffectiveCheckOutUtc = serverNow
        };
        db.AttendanceEvents.Add(evt);

        await RecordAuditEventAsync("attendance.check_out", summary.Id, request.Note ?? "Check out", new
        {
            attendanceId = summary.Id,
            employeeId = employeeId.Value,
            businessDate = today,
            checkInUtc = summary.CheckInUtc,
            checkOutUtc = serverNow,
            eligibleMinutes = metrics.EligibleMinutes,
            clientTimeUtc = request.ClientTimeUtc
        }, serverNow, token);

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            throw new ResourceConflictException("resource.conflict", "Concurrency conflict: record has been modified.");
        }

        var otMinutes = await db.AttendanceOvertimeEntries.AsNoTracking()
            .Where(ot => ot.AttendanceId == summary.Id)
            .SumAsync(ot => (decimal?)ot.Minutes, token) ?? 0m;

        var status = AttendanceMath.Status(summary, today);
        var rowVersionStr = Convert.ToBase64String(summary.RowVersion);

        var response = new AttendanceDayResponse(
            summary.Id,
            summary.EmployeeId,
            summary.BusinessDate,
            summary.CheckInUtc,
            summary.CheckOutUtc,
            summary.EligibleMinutes,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            status,
            summary.CalendarVersionId,
            otMinutes,
            summary.UpdatedAtUtc,
            rowVersionStr);

        var idempRecord = new AttendanceIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            UserId = security.CurrentUserId.Value,
            Operation = "CheckOut",
            Key = keyGuid,
            RequestHash = requestHash,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAtUtc = serverNow
        };
        db.AttendanceIdempotencyRecords.Add(idempRecord);
        await db.SaveChangesAsync(token);

        await transaction.CommitAsync(token);
        return response;
    }

    public async Task<AttendanceDayResponse> AdjustAsync(
        Guid employeeId,
        DateOnly day,
        AdjustAttendanceRequest request,
        string key,
        CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        if (!security.HasPermission(HrmPermissions.AttendanceManage))
            throw new AttendanceAccessDeniedException("authorization.forbidden");

        var callerEmployeeId = await security.GetEmployeeIdAsync(token);
        if (callerEmployeeId.HasValue && callerEmployeeId.Value == employeeId)
            throw new AttendanceAccessDeniedException("attendance.self_adjustment_forbidden");

        var isAssigned = await db.HrEmployeeAssignments.AsNoTracking()
            .AnyAsync(a => a.HrUserId == security.CurrentUserId.Value && a.EmployeeId == employeeId, token);
        if (!isAssigned)
            throw new AttendanceAccessDeniedException("authorization.scope_forbidden");

        if (!TryParseUuidV4(key, out var keyGuid))
            throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
            throw new RequestValidationException("attendance.reason_required", "Reason must be at least 10 characters.");

        var expectedVersion = ValidateRowVersion(request.RowVersion);

        var canonical = JsonSerializer.Serialize(new
        {
            employeeId,
            day = day.ToString("yyyy-MM-dd"),
            checkInUtc = request.CheckInUtc.ToUniversalTime(),
            checkOutUtc = request.CheckOutUtc?.ToUniversalTime(),
            reason = request.Reason.Trim(),
            rowVersion = request.RowVersion.Trim()
        });
        var requestHash = ComputeSha256Hex(canonical);

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await AttendanceLocks.EmployeeAsync(db, employeeId, token);

        var initialProfile = await db.EmployeeProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == employeeId, token);
        if (initialProfile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        if (initialProfile.UserId == security.CurrentUserId.Value)
            throw new AttendanceAccessDeniedException("attendance.self_adjustment_forbidden");

        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {initialProfile.UserId}")
            .AsTracking().SingleOrDefaultAsync(token);
        if (user is null)
            throw new AttendanceNotFoundException("account.not_found");

        var profile = await db.EmployeeProfiles.AsTracking().SingleOrDefaultAsync(p => p.Id == employeeId, token);
        if (profile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        var idemp = await db.AttendanceIdempotencyRecords
            .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "Adjust" && r.Key == keyGuid, token);
        if (idemp is not null)
        {
            if (idemp.RequestHash == requestHash)
            {
                var replay = JsonSerializer.Deserialize<AttendanceDayResponse>(idemp.ResponseJson)!;
                await transaction.CommitAsync(token);
                return replay;
            }
            throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
        }

        var nowTime = clock.Now();
        var serverNow = nowTime.ServerTimeUtc;
        var today = nowTime.BusinessDate;

        if (day > today)
            throw new RequestValidationException("attendance.future_date", "Adjustment date cannot be in the future.");

        if (profile.TerminationDate.HasValue && day > profile.TerminationDate.Value)
            throw new DomainRuleException("employees.past_termination_date", "Operation date cannot be after employee termination date.");
        if (profile.HireDate.HasValue && day < profile.HireDate.Value)
            throw new DomainRuleException("attendance.before_hire_date", "Operation date cannot be before employee hire date.");

        await AttendanceLocks.MonthAsync(db, day, false, token);
        await EnsurePeriodNotClosedAsync(day, token);

        var summary = await db.AttendanceSummaries.AsTracking()
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId && s.BusinessDate == day, token);
        if (summary is null)
            throw new AttendanceNotFoundException("attendance.not_found");

        if (summary.RowVersion is { Length: > 0 } && !expectedVersion.SequenceEqual(summary.RowVersion))
            throw new ResourceConflictException("resource.conflict", "Concurrency conflict: record has been modified.");

        var hasConfirmedOt = await db.AttendanceOvertimeEntries.AsNoTracking()
            .AnyAsync(ot => ot.AttendanceId == summary.Id, token);
        if (hasConfirmedOt)
            throw new ResourceConflictException("attendance.ot_already_confirmed", "Cannot adjust attendance record with confirmed overtime.");

        if (CalendarMath.BusinessDate(request.CheckInUtc) != day)
            throw new DomainRuleException("calendar.business_date_mismatch", "Check-in instant must belong to the route business date.");

        if (request.CheckOutUtc.HasValue && CalendarMath.BusinessDate(request.CheckOutUtc.Value) != day)
            throw new DomainRuleException("calendar.business_date_mismatch", "Check-out instant must belong to the route business date.");

        if (request.CheckInUtc > serverNow || (request.CheckOutUtc.HasValue && request.CheckOutUtc.Value > serverNow))
            throw new DomainRuleException("attendance.future_instant", "Check-in and check-out instants cannot be in the future.");

        if (request.CheckOutUtc.HasValue && request.CheckOutUtc.Value < request.CheckInUtc)
            throw new DomainRuleException("calendar.invalid_interval", "Check-out cannot precede check-in.");

        var definition = JsonSerializer.Deserialize<WorkCalendarDefinition>(summary.CalendarDefinitionJson)
            ?? throw new DomainRuleException("calendar.invalid_definition", "Stored calendar definition is unavailable.");

        var metrics = AttendanceMath.Calculate(definition, day, request.CheckInUtc, request.CheckOutUtc);
        await WorkLeaveInvariant.EnsureAttendanceAsync(db, employeeId, day, metrics.EligibleMinutes, request.CheckOutUtc.HasValue, CalendarMath.StandardMinutes(definition, day), false, token);

        var beforeCheckIn = summary.CheckInUtc;
        var beforeCheckOut = summary.CheckOutUtc;
        var beforeEligible = summary.EligibleMinutes;

        summary.CheckInUtc = request.CheckInUtc;
        summary.CheckOutUtc = request.CheckOutUtc;
        summary.EligibleMinutes = metrics.EligibleMinutes;
        summary.LateMinutes = metrics.LateMinutes;
        summary.EarlyLeaveMinutes = metrics.EarlyLeaveMinutes;
        summary.Phase = "Corrected";
        summary.UpdatedAtUtc = serverNow;

        db.Entry(summary).Property(s => s.RowVersion).OriginalValue = expectedVersion;
        db.Entry(summary).Property(s => s.UpdatedAtUtc).IsModified = true;

        var sanitizedReason = AuditMasker.SanitizeFreeText(request.Reason.Trim());
        var auditLog = new AttendanceAuditLog
        {
            Id = Guid.NewGuid(),
            AttendanceId = summary.Id,
            ActorId = security.CurrentUserId.Value,
            Reason = sanitizedReason,
            RecordedAtUtc = serverNow,
            BeforeCheckInUtc = beforeCheckIn,
            BeforeCheckOutUtc = beforeCheckOut,
            AfterCheckInUtc = request.CheckInUtc,
            AfterCheckOutUtc = request.CheckOutUtc,
            BeforeEligibleMinutes = beforeEligible,
            AfterEligibleMinutes = metrics.EligibleMinutes
        };
        db.AttendanceAuditLogs.Add(auditLog);

        var evt = new AttendanceEvent
        {
            Id = Guid.NewGuid(),
            AttendanceId = summary.Id,
            ActorId = security.CurrentUserId.Value,
            Kind = "Adjustment",
            OccurredAtUtc = serverNow,
            EffectiveCheckInUtc = request.CheckInUtc,
            EffectiveCheckOutUtc = request.CheckOutUtc
        };
        db.AttendanceEvents.Add(evt);

        await RecordAuditEventAsync("attendance.adjusted", summary.Id, sanitizedReason, new
        {
            attendanceId = summary.Id,
            employeeId,
            businessDate = day,
            beforeCheckInUtc = beforeCheckIn,
            beforeCheckOutUtc = beforeCheckOut,
            afterCheckInUtc = request.CheckInUtc,
            afterCheckOutUtc = request.CheckOutUtc,
            beforeEligibleMinutes = beforeEligible,
            afterEligibleMinutes = metrics.EligibleMinutes
        }, serverNow, token);

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            throw new ResourceConflictException("resource.conflict", "Concurrency conflict: record has been modified.");
        }

        var status = AttendanceMath.Status(summary, today);
        var rowVersionStr = Convert.ToBase64String(summary.RowVersion);

        var response = new AttendanceDayResponse(
            summary.Id,
            summary.EmployeeId,
            summary.BusinessDate,
            summary.CheckInUtc,
            summary.CheckOutUtc,
            summary.EligibleMinutes,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            status,
            summary.CalendarVersionId,
            0m,
            summary.UpdatedAtUtc,
            rowVersionStr);

        var idempRecord = new AttendanceIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            UserId = security.CurrentUserId.Value,
            Operation = "Adjust",
            Key = keyGuid,
            RequestHash = requestHash,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAtUtc = serverNow
        };
        db.AttendanceIdempotencyRecords.Add(idempRecord);
        await db.SaveChangesAsync(token);

        await transaction.CommitAsync(token);
        return response;
    }

    public async Task<AttendanceDayResponse> ConfirmOvertimeAsync(
        Guid employeeId,
        DateOnly day,
        ConfirmOvertimeRequest request,
        string key,
        CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new AttendanceAccessDeniedException("authorization.unauthenticated");

        if (!security.HasPermission(HrmPermissions.AttendanceManage))
            throw new AttendanceAccessDeniedException("authorization.forbidden");

        var callerEmployeeId = await security.GetEmployeeIdAsync(token);
        if (callerEmployeeId.HasValue && callerEmployeeId.Value == employeeId)
            throw new AttendanceAccessDeniedException("attendance.self_adjustment_forbidden");

        var isAssigned = await db.HrEmployeeAssignments.AsNoTracking()
            .AnyAsync(a => a.HrUserId == security.CurrentUserId.Value && a.EmployeeId == employeeId, token);
        if (!isAssigned)
            throw new AttendanceAccessDeniedException("authorization.scope_forbidden");

        if (!TryParseUuidV4(key, out var keyGuid))
            throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

        if (request.Minutes <= 0m || request.Minutes > 1440m)
            throw new RequestValidationException("attendance.invalid_overtime_minutes", "Overtime minutes must be between 1 and 1440.");

        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
            throw new RequestValidationException("attendance.reason_required", "Reason must be at least 10 characters.");

        var expectedVersion = ValidateRowVersion(request.RowVersion);

        var canonical = JsonSerializer.Serialize(new
        {
            employeeId,
            day = day.ToString("yyyy-MM-dd"),
            minutes = request.Minutes,
            reason = request.Reason.Trim(),
            rowVersion = request.RowVersion.Trim()
        });
        var requestHash = ComputeSha256Hex(canonical);

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await AttendanceLocks.EmployeeAsync(db, employeeId, token);

        var initialProfile = await db.EmployeeProfiles.AsNoTracking().SingleOrDefaultAsync(p => p.Id == employeeId, token);
        if (initialProfile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        if (initialProfile.UserId == security.CurrentUserId.Value)
            throw new AttendanceAccessDeniedException("attendance.self_adjustment_forbidden");

        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {initialProfile.UserId}")
            .AsTracking().SingleOrDefaultAsync(token);
        if (user is null)
            throw new AttendanceNotFoundException("account.not_found");

        var profile = await db.EmployeeProfiles.AsTracking().SingleOrDefaultAsync(p => p.Id == employeeId, token);
        if (profile is null)
            throw new AttendanceNotFoundException("employee.not_found");

        var idemp = await db.AttendanceIdempotencyRecords
            .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "ConfirmOvertime" && r.Key == keyGuid, token);
        if (idemp is not null)
        {
            if (idemp.RequestHash == requestHash)
            {
                var replay = JsonSerializer.Deserialize<AttendanceDayResponse>(idemp.ResponseJson)!;
                await transaction.CommitAsync(token);
                return replay;
            }
            throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
        }

        var nowTime = clock.Now();
        var serverNow = nowTime.ServerTimeUtc;
        var today = nowTime.BusinessDate;

        if (day > today)
            throw new RequestValidationException("attendance.future_date", "Overtime date cannot be in the future.");

        if (profile.TerminationDate.HasValue && day > profile.TerminationDate.Value)
            throw new DomainRuleException("employees.past_termination_date", "Operation date cannot be after employee termination date.");
        if (profile.HireDate.HasValue && day < profile.HireDate.Value)
            throw new DomainRuleException("attendance.before_hire_date", "Operation date cannot be before employee hire date.");

        await AttendanceLocks.MonthAsync(db, day, false, token);
        await EnsurePeriodNotClosedAsync(day, token);

        var summary = await db.AttendanceSummaries.AsTracking()
            .SingleOrDefaultAsync(s => s.EmployeeId == employeeId && s.BusinessDate == day, token);
        if (summary is null)
            throw new AttendanceNotFoundException("attendance.not_found");

        if (!summary.CheckOutUtc.HasValue)
            throw new ResourceConflictException("attendance.not_completed", "Overtime can only be confirmed for completed attendance days.");

        if (summary.RowVersion is { Length: > 0 } && !expectedVersion.SequenceEqual(summary.RowVersion))
            throw new ResourceConflictException("resource.conflict", "Concurrency conflict: record has been modified.");

        var pinnedCalendar = JsonSerializer.Deserialize<WorkCalendarDefinition>(summary.CalendarDefinitionJson)
            ?? throw new DomainRuleException("calendar.invalid_definition", "Stored calendar definition is unavailable.");
        await WorkLeaveInvariant.EnsureAttendanceAsync(db, employeeId, day, summary.EligibleMinutes, true, CalendarMath.StandardMinutes(pinnedCalendar, day), true, token);

        var existingOt = await db.AttendanceOvertimeEntries.AsNoTracking()
            .AnyAsync(ot => ot.AttendanceId == summary.Id, token);
        if (existingOt)
            throw new ResourceConflictException("attendance.overtime_already_confirmed", "Overtime has already been confirmed for this day.");

        summary.UpdatedAtUtc = serverNow;
        db.Entry(summary).Property(s => s.RowVersion).OriginalValue = expectedVersion;
        db.Entry(summary).Property(s => s.UpdatedAtUtc).IsModified = true;

        var sanitizedReason = AuditMasker.SanitizeFreeText(request.Reason.Trim());
        var otEntry = new AttendanceOvertimeEntry
        {
            Id = Guid.NewGuid(),
            AttendanceId = summary.Id,
            ConfirmedBy = security.CurrentUserId.Value,
            Minutes = request.Minutes,
            Reason = sanitizedReason,
            ConfirmedAtUtc = serverNow
        };
        db.AttendanceOvertimeEntries.Add(otEntry);

        var evt = new AttendanceEvent
        {
            Id = Guid.NewGuid(),
            AttendanceId = summary.Id,
            ActorId = security.CurrentUserId.Value,
            Kind = "OvertimeConfirmed",
            OccurredAtUtc = serverNow,
            EffectiveCheckInUtc = summary.CheckInUtc,
            EffectiveCheckOutUtc = summary.CheckOutUtc
        };
        db.AttendanceEvents.Add(evt);

        await RecordAuditEventAsync("attendance.overtime_confirmed", summary.Id, sanitizedReason, new
        {
            attendanceId = summary.Id,
            employeeId,
            businessDate = day,
            minutes = request.Minutes,
            confirmedBy = security.CurrentUserId.Value
        }, serverNow, token);

        try
        {
            await db.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(token);
            throw new ResourceConflictException("resource.conflict", "Concurrency conflict: record has been modified.");
        }

        var status = AttendanceMath.Status(summary, today);
        var rowVersionStr = Convert.ToBase64String(summary.RowVersion);

        var response = new AttendanceDayResponse(
            summary.Id,
            summary.EmployeeId,
            summary.BusinessDate,
            summary.CheckInUtc,
            summary.CheckOutUtc,
            summary.EligibleMinutes,
            summary.LateMinutes,
            summary.EarlyLeaveMinutes,
            status,
            summary.CalendarVersionId,
            request.Minutes,
            summary.UpdatedAtUtc,
            rowVersionStr);

        var idempRecord = new AttendanceIdempotencyRecord
        {
            Id = Guid.NewGuid(),
            UserId = security.CurrentUserId.Value,
            Operation = "ConfirmOvertime",
            Key = keyGuid,
            RequestHash = requestHash,
            ResponseJson = JsonSerializer.Serialize(response),
            CreatedAtUtc = serverNow
        };
        db.AttendanceIdempotencyRecords.Add(idempRecord);
        await db.SaveChangesAsync(token);

        await transaction.CommitAsync(token);
        return response;
    }
}
