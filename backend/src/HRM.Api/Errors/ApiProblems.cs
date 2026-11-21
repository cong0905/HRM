using HRM.Next.Application.Payroll;
using System.Diagnostics;
using HRM.Next.Application.Common;
using HRM.Next.Application.Attendance;
using HRM.Next.Application.Leave;
using HRM.Next.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace HRM.Next.Api.Errors;

public static class ApiProblems
{
    public static ProblemDetails Create(HttpContext context, int status, string code, string? title = null)
    {
        var problem = new ProblemDetails { Status = status, Type = "about:blank", Title = title ?? ReasonPhrases.GetReasonPhrase(status) };
        problem.Extensions["errorCode"] = code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        return problem;
    }
}

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499;
            return true;
        }
        var (status, code, message) = exception switch
        {
            PayrollAccessDeniedException => (403, "authorization.forbidden", "Payroll permission required."),
            PayrollNotFoundException payrollMissing => (404, payrollMissing.Code, payrollMissing.Message),
            AttendanceAccessDeniedException denied => (403, denied.Code, denied.Message),
            AttendanceNotFoundException missing => (404, missing.Code, missing.Message),
            LeaveAccessDeniedException deniedLeave => (403, deniedLeave.Code, deniedLeave.Message),
            LeaveNotFoundException missingLeave => (404, missingLeave.Code, missingLeave.Message),
            RequestValidationException validation => (400, validation.Code, validation.Message),
            ResourceConflictException conflict => (409, conflict.Code, conflict.Message),
            DomainRuleException rule => (422, rule.Code, rule.Message),
            _ => (500, "request.failed", "Không thể xử lý yêu cầu. Dùng traceId để tra cứu lỗi.")
        };
        // Do not put SQL, connection strings, user input or exception text into API/log output.
        if (status == 500)
            logger.LogError("Request failure {ExceptionType}; TraceId {TraceId}", exception.GetType().Name, context.TraceIdentifier);
        await Results.Problem(ApiProblems.Create(context, status, code, message)).ExecuteAsync(context);
        return true;
    }
}
