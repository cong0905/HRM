using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Next.Application.Audit;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Calendar;
using HRM.Next.Application.Common;
using HRM.Next.Application.Leave;
using HRM.Next.Domain;
using HRM.Next.Domain.Audit;
using HRM.Next.Domain.Leave;
using HRM.Next.Domain.Personnel;
using HRM.Next.Infrastructure.Attendance;
using HRM.Next.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRM.Next.Infrastructure.Leave;

public sealed class LeaveService(
    HrmDbContext db,
    ISecurityContext security,
    IWorkCalendarService calendars,
    BusinessClock clock,
    IAuditRepository audit) : ILeaveService
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

    private async Task LockReceiptAsync(string operation, Guid key, CancellationToken token)
    {
        if (key == Guid.Empty) return;
        var resource = $"HRM.Leave.Receipt.{security.CurrentUserId:N}.{operation}.{key:N}";
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000; IF @r<0 THROW 51011, 'Leave receipt lock busy.', 1;", token);
    }

    private async Task<LeaveRecord> LockRequestAsync(Guid id, bool ownerOnly, CancellationToken token)
    {
        // Read only routing information before the employee lock, never tracked state.
        var employeeId = await db.LeaveRecords.AsNoTracking().Where(l => l.Id == id)
            .Select(l => (Guid?)l.EmployeeId).SingleOrDefaultAsync(token);
        if (!employeeId.HasValue)
            throw new LeaveNotFoundException("leave.not_found", "Leave request not found.");
        await AttendanceLocks.EmployeeAsync(db, employeeId.Value, token);
        var callerEmployeeId = await security.GetEmployeeIdAsync(token);
        if (ownerOnly)
        {
            if (callerEmployeeId != employeeId)
                throw new LeaveAccessDeniedException("leave.cancel_forbidden", "Only the requesting employee can cancel their pending leave.");
        }
        else
        {
            if (callerEmployeeId == employeeId)
                throw new LeaveAccessDeniedException("leave.self_approval_forbidden", "Approver cannot decide their own leave request.");
            var assigned = await db.HrEmployeeAssignments.AsNoTracking().AnyAsync(a =>
                a.HrUserId == security.CurrentUserId && a.EmployeeId == employeeId.Value, token);
            if (!assigned)
                throw new LeaveAccessDeniedException("authorization.scope_forbidden", "Caller is not assigned to manage this employee.");
        }
        return await db.LeaveRecords.AsTracking().SingleAsync(l => l.Id == id, token);
    }

    private async Task EnsureApprovalEmployeeActiveAsync(LeaveRecord leave, CancellationToken token)
    {
        var profile = await db.EmployeeProfiles.AsNoTracking().SingleAsync(e => e.Id == leave.EmployeeId, token);
        // Same user-before-profile write lock order as offboarding/attendance.
        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {profile.UserId}")
            .AsTracking().SingleOrDefaultAsync(token);
        if (profile.TerminationDate.HasValue && leave.EndDate > profile.TerminationDate.Value)
            throw new ResourceConflictException("leave.approval_after_termination_forbidden", "Cannot approve leave after employee termination date.");
        if (user is null || !user.IsActive || string.Equals(profile.Status, "DaNghiViec", StringComparison.OrdinalIgnoreCase))
            throw new ResourceConflictException("leave.employee_inactive_or_terminated", "Cannot approve new leave for an inactive or terminated employee.");
        if (profile.HireDate.HasValue && leave.StartDate < profile.HireDate.Value)
            throw new ResourceConflictException("leave.before_hire_date", "Leave cannot precede the employee hire date.");
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
            throw new ResourceConflictException("leave.period_closed", "Leave cannot be modified for a closed or published payroll period.");
        }
    }

    private async Task EnsureRangePeriodsNotClosedAsync(DateOnly start, DateOnly end, CancellationToken token)
    {
        var current = new DateOnly(start.Year, start.Month, 1);
        var endMonth = new DateOnly(end.Year, end.Month, 1);
        while (current <= endMonth)
        {
            await AttendanceLocks.MonthAsync(db, current, false, token);
            await EnsurePeriodNotClosedAsync(current, token);
            current = current.AddMonths(1);
        }
    }

    private async Task CheckScopeAsync(Guid employeeId, CancellationToken token)
    {
        var callerEmployeeId = await security.GetEmployeeIdAsync(token);
        if (callerEmployeeId.HasValue && callerEmployeeId.Value == employeeId)
            return;

        if (security.HasPermission(HrmPermissions.LeavesReadAll) || security.HasPermission(HrmPermissions.LeavesApprove))
        {
            var isAssigned = await db.HrEmployeeAssignments.AsNoTracking()
                .AnyAsync(a => a.HrUserId == security.CurrentUserId && a.EmployeeId == employeeId, token);

            if (!isAssigned)
                throw new LeaveAccessDeniedException("authorization.scope_forbidden", "Caller is not assigned to manage this employee.");
            return;
        }

        throw new LeaveAccessDeniedException("authorization.forbidden", "You do not have permission to access this employee's leave records.");
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
            EntityName = "LeaveRecord",
            EntityId = entityId.ToString(),
            Reason = AuditMasker.SanitizeFreeText(reason),
            Payload = AuditMasker.MaskObject(payload)
        }, token);

    private static LeaveRecordResponse MapRecord(LeaveRecord leave) =>
        new(leave.Id,
            leave.EmployeeId,
            leave.StartDate,
            leave.EndDate,
            leave.Reason,
            leave.Status,
            leave.ApproverId,
            leave.CreatedAtUtc,
            leave.WorkingDays,
            leave.RowVersion.Length > 0 ? Convert.ToBase64String(leave.RowVersion) : null,
            leave.DecisionReason,
            leave.DecidedAtUtc,
            leave.CancelledAtUtc,
            leave.CancellationReason);

    private static LeaveBalanceResponse MapBalance(LeaveBalance balance) =>
        new(balance.Id,
            balance.EmployeeId,
            balance.Year,
            balance.AllocatedDays,
            balance.UsedDays,
            balance.RemainingDays,
            balance.UpdatedAtUtc,
            balance.RowVersion.Length > 0 ? Convert.ToBase64String(balance.RowVersion) : string.Empty);

    public async Task<LeaveRecordResponse> CreateRequestAsync(CreateLeaveRequest request, string? idempotencyKey, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new LeaveAccessDeniedException("authorization.unauthenticated");

        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue)
            throw new LeaveAccessDeniedException("authorization.employee_link_required", "Authenticated user is not linked to an employee profile.");

        LeaveMath.ValidateDates(request.StartDate, request.EndDate);

        Guid keyGuid = Guid.Empty;
        string requestHash = string.Empty;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (!TryParseUuidV4(idempotencyKey, out keyGuid))
                throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

            var canonical = JsonSerializer.Serialize(new
            {
                employeeId = employeeId.Value,
                startDate = request.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                endDate = request.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                reason = request.Reason.Trim()
            });
            requestHash = ComputeSha256Hex(canonical);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockReceiptAsync("leave.create", keyGuid, token);
        await AttendanceLocks.EmployeeAsync(db, employeeId.Value, token);

        var user = await db.Users.FromSqlInterpolated($"SELECT * FROM [AspNetUsers] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = {security.CurrentUserId.Value}")
            .AsTracking().SingleOrDefaultAsync(token);
        if (user is null || !user.IsActive)
            throw new LeaveAccessDeniedException("account.inactive", "Account is inactive.");

        var profile = await db.EmployeeProfiles.AsTracking().SingleOrDefaultAsync(p => p.Id == employeeId.Value, token);
        if (profile is null)
            throw new LeaveNotFoundException("employee.not_found", "Employee profile not found.");

        if (string.Equals(profile.Status, "DaNghiViec", StringComparison.OrdinalIgnoreCase))
            throw new DomainRuleException("employees.terminated", "Terminated employees cannot perform operational activities.");

        if (profile.HireDate.HasValue && request.StartDate < profile.HireDate.Value)
            throw new DomainRuleException("leave.before_hire_date", "Leave cannot be requested before employee hire date.");

        if (profile.TerminationDate.HasValue && (request.StartDate > profile.TerminationDate.Value || request.EndDate > profile.TerminationDate.Value))
            throw new DomainRuleException("leave.after_termination_date", "Leave cannot be requested after employee termination date.");

        if (keyGuid != Guid.Empty)
        {
            var idemp = await db.LeaveIdempotencyRecords
                .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "leave.create" && r.Key == keyGuid, token);
            if (idemp is not null)
            {
                if (idemp.RequestHash == requestHash)
                {
                    var replay = JsonSerializer.Deserialize<LeaveRecordResponse>(idemp.ResponseJson)!;
                    await transaction.CommitAsync(token);
                    return replay;
                }
                throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
            }
        }

        await EnsureRangePeriodsNotClosedAsync(request.StartDate, request.EndDate, token);

        // Calculate working days from calendar
        decimal workingDays = 0m;
        for (var current = request.StartDate; current <= request.EndDate; current = current.AddDays(1))
        {
            var dayCal = await calendars.DayAsync(current, token);
            if (dayCal.StandardMinutes > 0m)
                workingDays += 1.0m;
        }

        if (workingDays <= 0m)
            throw new DomainRuleException("leave.no_working_days", "The requested date range does not contain any working days.");

        // Check date overlap
        var hasOverlap = await db.LeaveRecords.AsNoTracking().AnyAsync(l =>
            l.EmployeeId == employeeId.Value &&
            (l.Status == "Pending" || l.Status == "Approved") &&
            l.StartDate <= request.EndDate && l.EndDate >= request.StartDate, token);

        if (hasOverlap)
            throw new ResourceConflictException("leave.date_overlap", "Leave request overlaps with an existing pending or approved request.");

        // Check leave balance at submission time
        var balance = await db.LeaveBalances.AsNoTracking()
            .SingleOrDefaultAsync(b => b.EmployeeId == employeeId.Value && b.Year == request.StartDate.Year, token);

        if (balance is null || balance.RemainingDays < workingDays)
            throw new DomainRuleException("leave.insufficient_balance", "Insufficient leave balance for the requested year.");

        var serverNow = clock.Now().ServerTimeUtc;
        var leave = new LeaveRecord
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId.Value,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            WorkingDays = workingDays,
            Reason = request.Reason.Trim(),
            Status = "Pending",
            CreatedAtUtc = serverNow
        };

        db.LeaveRecords.Add(leave);
        await db.SaveChangesAsync(token);

        var response = MapRecord(leave);

        await RecordAuditEventAsync("leaves.created", leave.Id, request.Reason,
            new { leaveId = leave.Id, employeeId = leave.EmployeeId, request.StartDate, request.EndDate, workingDays },
            serverNow, token);

        if (keyGuid != Guid.Empty)
        {
            db.LeaveIdempotencyRecords.Add(new LeaveIdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = security.CurrentUserId.Value,
                Operation = "leave.create",
                Key = keyGuid,
                RequestHash = requestHash,
                ResponseJson = JsonSerializer.Serialize(response),
                StatusCode = 201,
                CreatedAtUtc = serverNow
            });
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
        return response;
    }

    public async Task<LeaveRecordResponse> GetRequestByIdAsync(Guid id, CancellationToken token)
    {
        var leave = await db.LeaveRecords.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, token);
        if (leave is null)
            throw new LeaveNotFoundException("leave.not_found", "Leave request not found.");

        await CheckScopeAsync(leave.EmployeeId, token);
        return MapRecord(leave);
    }

    public async Task<PagedResult<LeaveRecordResponse>> GetOwnRequestsAsync(int page, int pageSize, CancellationToken token)
    {
        var employeeId = await security.GetEmployeeIdAsync(token);
        if (!employeeId.HasValue)
            throw new LeaveAccessDeniedException("authorization.employee_link_required", "Authenticated user is not linked to an employee profile.");

        var query = db.LeaveRecords.AsNoTracking().Where(l => l.EmployeeId == employeeId.Value);
        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Select(l => MapRecord(l))
            .ToListAsync(token);

        return new PagedResult<LeaveRecordResponse>(items, page, pageSize, total);
    }

    public async Task<PagedResult<LeaveRecordResponse>> GetEmployeeRequestsAsync(Guid employeeId, int page, int pageSize, CancellationToken token)
    {
        await CheckScopeAsync(employeeId, token);

        var query = db.LeaveRecords.AsNoTracking().Where(l => l.EmployeeId == employeeId);
        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Select(l => MapRecord(l))
            .ToListAsync(token);

        return new PagedResult<LeaveRecordResponse>(items, page, pageSize, total);
    }

    public async Task<PagedResult<LeaveRecordResponse>> ListRequestsAsync(Guid? employeeId, int? year, string? status, int page, int pageSize, CancellationToken token)
    {
        IQueryable<LeaveRecord> query = db.LeaveRecords.AsNoTracking();

        if (employeeId.HasValue)
        {
            await CheckScopeAsync(employeeId.Value, token);
            query = query.Where(l => l.EmployeeId == employeeId.Value);
        }
        else
        {
            var callerEmployeeId = await security.GetEmployeeIdAsync(token);
            if (!security.HasPermission(HrmPermissions.LeavesReadAll))
            {
                if (!callerEmployeeId.HasValue)
                    throw new LeaveAccessDeniedException("authorization.forbidden");
                query = query.Where(l => l.EmployeeId == callerEmployeeId.Value);
            }
            else
            {
                var assignedIds = db.HrEmployeeAssignments.AsNoTracking()
                    .Where(a => a.HrUserId == security.CurrentUserId)
                    .Select(a => a.EmployeeId);
                query = query.Where(l => assignedIds.Contains(l.EmployeeId));
            }
        }

        if (year.HasValue)
            query = query.Where(l => l.StartDate.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status == status);

        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Select(l => MapRecord(l))
            .ToListAsync(token);

        return new PagedResult<LeaveRecordResponse>(items, page, pageSize, total);
    }

    public async Task ApproveRequestAsync(Guid id, ApproveLeaveRequest request, string? idempotencyKey, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new LeaveAccessDeniedException("authorization.unauthenticated");

        if (!security.HasPermission(HrmPermissions.LeavesApprove))
            throw new LeaveAccessDeniedException("authorization.forbidden", "Leaves.Approve permission required.");

        Guid keyGuid = Guid.Empty;
        string requestHash = string.Empty;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (!TryParseUuidV4(idempotencyKey, out keyGuid))
                throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

            var canonical = JsonSerializer.Serialize(new
            {
                leaveId = id,
                reason = request.Reason.Trim(),
                rowVersion = request.RowVersion
            });
            requestHash = ComputeSha256Hex(canonical);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockReceiptAsync("leave.approve", keyGuid, token);
        var leave = await LockRequestAsync(id, false, token);
        var callerEmployeeId = await security.GetEmployeeIdAsync(token);

        if (keyGuid != Guid.Empty)
        {
            var idemp = await db.LeaveIdempotencyRecords
                .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "leave.approve" && r.Key == keyGuid, token);
            if (idemp is not null)
            {
                if (idemp.RequestHash == requestHash)
                {
                    await transaction.CommitAsync(token);
                    return; // Idempotent replay, do not deduct balance again
                }
                throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
            }
        }

        await EnsureApprovalEmployeeActiveAsync(leave, token);

        // Check period closed
        await EnsureRangePeriodsNotClosedAsync(leave.StartDate, leave.EndDate, token);

        // Check current status
        if (string.Equals(leave.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            throw new ResourceConflictException("leave.already_processed", "Leave request has already been approved.");

        if (!string.Equals(leave.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new ResourceConflictException("leave.invalid_status", "Only pending leave requests can be approved.");

        // Stale row version check if provided
        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var expectedVersion = ValidateRowVersion(request.RowVersion);
            if (!expectedVersion.SequenceEqual(leave.RowVersion))
                throw new ResourceConflictException("leave.stale_version", "Leave request has been modified by another operation.");
        }

        await WorkLeaveInvariant.EnsureApprovalAsync(db, leave, token);

        if (leave.WorkingDays <= 0m)
            throw new ResourceConflictException("leave.working_days_missing", "Legacy request has no valid working-day calculation; cancel and recreate it before approval.");

        // Lock and read LeaveBalance with UPDLOCK, HOLDLOCK
        var balance = await db.LeaveBalances
            .FromSqlInterpolated($"SELECT * FROM [personnel].[LeaveBalances] WITH (UPDLOCK, HOLDLOCK) WHERE [EmployeeId] = {leave.EmployeeId} AND [Year] = {leave.StartDate.Year}")
            .AsTracking().SingleOrDefaultAsync(token);

        if (balance is null || balance.RemainingDays < leave.WorkingDays)
            throw new ResourceConflictException("leave.insufficient_balance", "Remaining leave balance is insufficient.");

        var serverNow = clock.Now().ServerTimeUtc;
        var balanceBefore = balance.RemainingDays;

        balance.UsedDays += leave.WorkingDays;
        balance.UpdatedAtUtc = serverNow;

        var balanceAfter = balance.RemainingDays;

        leave.Status = "Approved";
        leave.ApproverId = callerEmployeeId;
        leave.DecisionReason = request.Reason.Trim();
        leave.DecidedAtUtc = serverNow;

        var ledger = new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = leave.EmployeeId,
            LeaveRequestId = leave.Id,
            Year = leave.StartDate.Year,
            Action = "Debit",
            Days = leave.WorkingDays,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Reason = request.Reason.Trim(),
            ActorId = security.CurrentUserId,
            CreatedAtUtc = serverNow
        };

        db.LeaveLedgerEntries.Add(ledger);

        // Flush database changes inside the transaction before recording audit
        await db.SaveChangesAsync(token);

        await RecordAuditEventAsync("leaves.approved", leave.Id, request.Reason,
            new { leaveId = leave.Id, approverId = callerEmployeeId, workingDays = leave.WorkingDays, balanceBefore, balanceAfter },
            serverNow, token);

        if (keyGuid != Guid.Empty)
        {
            db.LeaveIdempotencyRecords.Add(new LeaveIdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = security.CurrentUserId.Value,
                Operation = "leave.approve",
                Key = keyGuid,
                RequestHash = requestHash,
                ResponseJson = "{}",
                StatusCode = 204,
                CreatedAtUtc = serverNow
            });
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
    }

    public async Task RejectRequestAsync(Guid id, RejectLeaveRequest request, string? idempotencyKey, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new LeaveAccessDeniedException("authorization.unauthenticated");

        if (!security.HasPermission(HrmPermissions.LeavesApprove))
            throw new LeaveAccessDeniedException("authorization.forbidden", "Leaves.Approve permission required.");

        Guid keyGuid = Guid.Empty;
        string requestHash = string.Empty;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (!TryParseUuidV4(idempotencyKey, out keyGuid))
                throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

            var canonical = JsonSerializer.Serialize(new
            {
                leaveId = id,
                reason = request.Reason.Trim(),
                rowVersion = request.RowVersion
            });
            requestHash = ComputeSha256Hex(canonical);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockReceiptAsync("leave.reject", keyGuid, token);
        var leave = await LockRequestAsync(id, false, token);
        var callerEmployeeId = await security.GetEmployeeIdAsync(token);

        if (keyGuid != Guid.Empty)
        {
            var idemp = await db.LeaveIdempotencyRecords
                .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "leave.reject" && r.Key == keyGuid, token);
            if (idemp is not null)
            {
                if (idemp.RequestHash == requestHash)
                {
                    await transaction.CommitAsync(token);
                    return;
                }
                throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
            }
        }

        if (!string.Equals(leave.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new ResourceConflictException("leave.invalid_status", "Only pending leave requests can be rejected.");

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var expectedVersion = ValidateRowVersion(request.RowVersion);
            if (!expectedVersion.SequenceEqual(leave.RowVersion))
                throw new ResourceConflictException("leave.stale_version", "Leave request has been modified by another operation.");
        }

        var serverNow = clock.Now().ServerTimeUtc;
        leave.Status = "Rejected";
        leave.ApproverId = callerEmployeeId;
        leave.DecisionReason = request.Reason.Trim();
        leave.DecidedAtUtc = serverNow;

        await db.SaveChangesAsync(token);

        await RecordAuditEventAsync("leaves.rejected", leave.Id, request.Reason,
            new { leaveId = leave.Id, approverId = callerEmployeeId, reason = request.Reason },
            serverNow, token);

        if (keyGuid != Guid.Empty)
        {
            db.LeaveIdempotencyRecords.Add(new LeaveIdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = security.CurrentUserId.Value,
                Operation = "leave.reject",
                Key = keyGuid,
                RequestHash = requestHash,
                ResponseJson = "{}",
                StatusCode = 204,
                CreatedAtUtc = serverNow
            });
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
    }

    public async Task CancelRequestAsync(Guid id, CancelLeaveRequest request, string? idempotencyKey, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new LeaveAccessDeniedException("authorization.unauthenticated");

        var callerEmployeeId = await security.GetEmployeeIdAsync(token);
        if (!callerEmployeeId.HasValue)
            throw new LeaveAccessDeniedException("authorization.employee_link_required", "Authenticated user is not linked to an employee profile.");

        Guid keyGuid = Guid.Empty;
        string requestHash = string.Empty;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (!TryParseUuidV4(idempotencyKey, out keyGuid))
                throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

            var canonical = JsonSerializer.Serialize(new
            {
                leaveId = id,
                reason = request.Reason?.Trim(),
                rowVersion = request.RowVersion
            });
            requestHash = ComputeSha256Hex(canonical);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockReceiptAsync("leave.cancel", keyGuid, token);
        var leave = await LockRequestAsync(id, true, token);

        if (keyGuid != Guid.Empty)
        {
            var idemp = await db.LeaveIdempotencyRecords
                .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "leave.cancel" && r.Key == keyGuid, token);
            if (idemp is not null)
            {
                if (idemp.RequestHash == requestHash)
                {
                    await transaction.CommitAsync(token);
                    return;
                }
                throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
            }
        }

        if (string.Equals(leave.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            throw new ResourceConflictException("leave.cancel_approved_unsupported", "Cancelling an approved leave request is not supported in MVP.");

        if (!string.Equals(leave.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new ResourceConflictException("leave.invalid_status", "Only pending leave requests can be cancelled.");

        if (!string.IsNullOrWhiteSpace(request.RowVersion))
        {
            var expectedVersion = ValidateRowVersion(request.RowVersion);
            if (!expectedVersion.SequenceEqual(leave.RowVersion))
                throw new ResourceConflictException("leave.stale_version", "Leave request has been modified by another operation.");
        }

        var serverNow = clock.Now().ServerTimeUtc;
        leave.Status = "Cancelled";
        leave.CancelledAtUtc = serverNow;
        leave.CancellationReason = request.Reason?.Trim();

        await db.SaveChangesAsync(token);

        await RecordAuditEventAsync("leaves.cancelled", leave.Id, request.Reason ?? "Cancelled by employee",
            new { leaveId = leave.Id, employeeId = leave.EmployeeId },
            serverNow, token);

        if (keyGuid != Guid.Empty)
        {
            db.LeaveIdempotencyRecords.Add(new LeaveIdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = security.CurrentUserId.Value,
                Operation = "leave.cancel",
                Key = keyGuid,
                RequestHash = requestHash,
                ResponseJson = "{}",
                StatusCode = 204,
                CreatedAtUtc = serverNow
            });
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
    }

    public async Task<LeaveBalanceResponse> GrantBalanceAsync(GrantLeaveBalanceRequest request, string? idempotencyKey, CancellationToken token)
    {
        if (!security.IsAuthenticated || !security.CurrentUserId.HasValue)
            throw new LeaveAccessDeniedException("authorization.unauthenticated");

        if (!security.HasPermission(HrmPermissions.LeavesApprove) && !security.HasPermission(HrmPermissions.EmployeesManage))
            throw new LeaveAccessDeniedException("authorization.forbidden", "Leaves.Approve or Employees.Manage permission required.");

        var isAssigned = await db.HrEmployeeAssignments.AsNoTracking()
            .AnyAsync(a => a.HrUserId == security.CurrentUserId && a.EmployeeId == request.EmployeeId, token);

        if (!isAssigned && !security.IsInRole(HrmRoles.SystemAdmin))
            throw new LeaveAccessDeniedException("authorization.scope_forbidden", "Caller is not assigned to manage this employee.");

        Guid keyGuid = Guid.Empty;
        string requestHash = string.Empty;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            if (!TryParseUuidV4(idempotencyKey, out keyGuid))
                throw new RequestValidationException("idempotency.invalid_key", "A valid UUIDv4 Idempotency-Key is required.");

            var canonical = JsonSerializer.Serialize(new
            {
                employeeId = request.EmployeeId,
                year = request.Year,
                days = request.Days,
                reason = request.Reason.Trim()
            });
            requestHash = ComputeSha256Hex(canonical);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await LockReceiptAsync("leave.grant", keyGuid, token);
        await AttendanceLocks.EmployeeAsync(db, request.EmployeeId, token);

        if (keyGuid != Guid.Empty)
        {
            var idemp = await db.LeaveIdempotencyRecords
                .SingleOrDefaultAsync(r => r.UserId == security.CurrentUserId.Value && r.Operation == "leave.grant" && r.Key == keyGuid, token);
            if (idemp is not null)
            {
                if (idemp.RequestHash == requestHash)
                {
                    var replay = JsonSerializer.Deserialize<LeaveBalanceResponse>(idemp.ResponseJson)!;
                    await transaction.CommitAsync(token);
                    return replay;
                }
                throw new ResourceConflictException("idempotency.payload_mismatch", "The idempotency key has already been used with a different request payload.");
            }
        }

        var employeeExists = await db.EmployeeProfiles.AsNoTracking().AnyAsync(e => e.Id == request.EmployeeId, token);
        if (!employeeExists)
            throw new LeaveNotFoundException("employee.not_found", "Employee profile not found.");

        var exists = await db.LeaveBalances.AsNoTracking()
            .AnyAsync(b => b.EmployeeId == request.EmployeeId && b.Year == request.Year, token);

        if (exists)
            throw new ResourceConflictException("leave.already_granted", "Leave balance has already been granted for this employee in this year.");

        var serverNow = clock.Now().ServerTimeUtc;
        var balance = new LeaveBalance
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            AllocatedDays = request.Days,
            UsedDays = 0m,
            UpdatedAtUtc = serverNow
        };

        db.LeaveBalances.Add(balance);

        var ledger = new LeaveLedgerEntry
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            LeaveRequestId = null,
            Year = request.Year,
            Action = "Grant",
            Days = request.Days,
            BalanceBefore = 0m,
            BalanceAfter = request.Days,
            Reason = request.Reason.Trim(),
            ActorId = security.CurrentUserId,
            CreatedAtUtc = serverNow
        };

        db.LeaveLedgerEntries.Add(ledger);
        await db.SaveChangesAsync(token);

        var response = MapBalance(balance);

        await RecordAuditEventAsync("leave_balances.granted", balance.Id, request.Reason,
            new { employeeId = request.EmployeeId, year = request.Year, allocatedDays = request.Days },
            serverNow, token);

        if (keyGuid != Guid.Empty)
        {
            db.LeaveIdempotencyRecords.Add(new LeaveIdempotencyRecord
            {
                Id = Guid.NewGuid(),
                UserId = security.CurrentUserId.Value,
                Operation = "leave.grant",
                Key = keyGuid,
                RequestHash = requestHash,
                ResponseJson = JsonSerializer.Serialize(response),
                StatusCode = 201,
                CreatedAtUtc = serverNow
            });
            await db.SaveChangesAsync(token);
        }

        await transaction.CommitAsync(token);
        return response;
    }

    public async Task<LeaveBalanceResponse?> GetBalanceAsync(Guid employeeId, int year, CancellationToken token)
    {
        await CheckScopeAsync(employeeId, token);

        var balance = await db.LeaveBalances.AsNoTracking()
            .SingleOrDefaultAsync(b => b.EmployeeId == employeeId && b.Year == year, token);

        return balance is null ? null : MapBalance(balance);
    }

    public async Task<IReadOnlyList<LeaveBalanceResponse>> GetBalancesAsync(Guid? employeeId, int? year, CancellationToken token)
    {
        IQueryable<LeaveBalance> query = db.LeaveBalances.AsNoTracking();

        if (employeeId.HasValue)
        {
            await CheckScopeAsync(employeeId.Value, token);
            query = query.Where(b => b.EmployeeId == employeeId.Value);
        }
        else
        {
            var callerEmployeeId = await security.GetEmployeeIdAsync(token);
            if (!security.HasPermission(HrmPermissions.LeavesReadAll))
            {
                if (!callerEmployeeId.HasValue)
                    throw new LeaveAccessDeniedException("authorization.forbidden");
                query = query.Where(b => b.EmployeeId == callerEmployeeId.Value);
            }
            else
            {
                var assignedIds = db.HrEmployeeAssignments.AsNoTracking()
                    .Where(a => a.HrUserId == security.CurrentUserId)
                    .Select(a => a.EmployeeId);
                query = query.Where(b => assignedIds.Contains(b.EmployeeId));
            }
        }

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);

        return await query
            .OrderByDescending(b => b.Year)
            .Select(b => MapBalance(b))
            .ToListAsync(token);
    }

    public async Task<PagedResult<LeaveLedgerEntryResponse>> GetLedgerEntriesAsync(Guid employeeId, int? year, int page, int pageSize, CancellationToken token)
    {
        await CheckScopeAsync(employeeId, token);

        var query = db.LeaveLedgerEntries.AsNoTracking().Where(l => l.EmployeeId == employeeId);
        if (year.HasValue)
            query = query.Where(l => l.Year == year.Value);

        var total = await query.LongCountAsync(token);
        var offset = (page - 1) * pageSize;

        var items = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .Skip(offset)
            .Take(pageSize)
            .Select(l => new LeaveLedgerEntryResponse(
                l.Id,
                l.EmployeeId,
                l.LeaveRequestId,
                l.Year,
                l.Action,
                l.Days,
                l.BalanceBefore,
                l.BalanceAfter,
                l.Reason,
                l.ActorId,
                l.CreatedAtUtc))
            .ToListAsync(token);

        return new PagedResult<LeaveLedgerEntryResponse>(items, page, pageSize, total);
    }
}
