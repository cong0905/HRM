using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using HRM.Next.Application.Common;

namespace HRM.Next.Application.Leave;

public sealed class LeaveAccessDeniedException(string code, string message = "Access denied.") : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class LeaveNotFoundException(string code, string message = "Leave record not found.") : Exception(message)
{
    public string Code { get; } = code;
}

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateLeaveRequest(
    [Required] DateOnly StartDate,
    [Required] DateOnly EndDate,
    [Required, StringLength(500)] string Reason);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ApproveLeaveRequest(
    [Required, StringLength(500)] string Reason,
    string? RowVersion = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RejectLeaveRequest(
    [Required, StringLength(500)] string Reason,
    string? RowVersion = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CancelLeaveRequest(
    [StringLength(500)] string? Reason = null,
    string? RowVersion = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record GrantLeaveBalanceRequest(
    [Required] Guid EmployeeId,
    [Required, Range(2000, 2100)] int Year,
    [Required, Range(typeof(decimal), "1", "365")] decimal Days,
    [Required, StringLength(500)] string Reason);

public sealed record LeaveRecordResponse(
    Guid Id,
    Guid EmployeeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason,
    string Status,
    Guid? ApproverId,
    DateTimeOffset CreatedAtUtc,
    decimal WorkingDays = 0m,
    string? RowVersion = null,
    string? DecisionReason = null,
    DateTimeOffset? DecidedAtUtc = null,
    DateTimeOffset? CancelledAtUtc = null,
    string? CancellationReason = null);

public sealed record LeaveBalanceResponse(
    Guid Id,
    Guid EmployeeId,
    int Year,
    decimal AllocatedDays,
    decimal UsedDays,
    decimal RemainingDays,
    DateTimeOffset UpdatedAtUtc,
    string RowVersion);

public sealed record LeaveLedgerEntryResponse(
    Guid Id,
    Guid EmployeeId,
    Guid? LeaveRequestId,
    int Year,
    string Action,
    decimal Days,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string Reason,
    Guid? ActorId,
    DateTimeOffset CreatedAtUtc);

public interface ILeaveService
{
    Task<LeaveRecordResponse> CreateRequestAsync(CreateLeaveRequest request, string? idempotencyKey, CancellationToken token);
    Task<LeaveRecordResponse> GetRequestByIdAsync(Guid id, CancellationToken token);
    Task<PagedResult<LeaveRecordResponse>> GetOwnRequestsAsync(int page, int pageSize, CancellationToken token);
    Task<PagedResult<LeaveRecordResponse>> GetEmployeeRequestsAsync(Guid employeeId, int page, int pageSize, CancellationToken token);
    Task<PagedResult<LeaveRecordResponse>> ListRequestsAsync(Guid? employeeId, int? year, string? status, int page, int pageSize, CancellationToken token);
    Task ApproveRequestAsync(Guid id, ApproveLeaveRequest request, string? idempotencyKey, CancellationToken token);
    Task RejectRequestAsync(Guid id, RejectLeaveRequest request, string? idempotencyKey, CancellationToken token);
    Task CancelRequestAsync(Guid id, CancelLeaveRequest request, string? idempotencyKey, CancellationToken token);
    Task<LeaveBalanceResponse> GrantBalanceAsync(GrantLeaveBalanceRequest request, string? idempotencyKey, CancellationToken token);
    Task<LeaveBalanceResponse?> GetBalanceAsync(Guid employeeId, int year, CancellationToken token);
    Task<IReadOnlyList<LeaveBalanceResponse>> GetBalancesAsync(Guid? employeeId, int? year, CancellationToken token);
    Task<PagedResult<LeaveLedgerEntryResponse>> GetLedgerEntriesAsync(Guid employeeId, int? year, int page, int pageSize, CancellationToken token);
}
