using HRM.Next.Application.Authorization;
using HRM.Next.Application.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Authorize]
public sealed class PayslipQueriesController(IPayrollReadService reads) : ControllerBase
{
    [HttpGet("api/v1/payslips/{id:guid}", Name = "GetPayslipDetail")]
    [Authorize(Policy = HrmPolicies.PayrollRead)]
    [ProducesResponseType<PayslipDetailResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<PayslipDetailResponse>> Detail(Guid id, CancellationToken token) =>
        Ok(await reads.DetailAsync(id, false, token));

    [HttpGet("api/v1/me/payslips/{id:guid}", Name = "GetMyPayslipDetail")]
    [ProducesResponseType<PayslipDetailResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<PayslipDetailResponse>> Own(Guid id, CancellationToken token) =>
        Ok(await reads.DetailAsync(id, true, token));

    [HttpGet("api/v1/payroll-periods/{id:guid}/reconciliation", Name = "ReconcilePayrollPeriod")]
    [Authorize(Policy = HrmPolicies.PayrollRead)]
    [ProducesResponseType<PayrollReconciliationResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType<ProblemDetails>(409)]
    public async Task<ActionResult<PayrollReconciliationResponse>> Reconcile(Guid id, CancellationToken token) =>
        Ok(await reads.ReconcileAsync(id, token));
}
