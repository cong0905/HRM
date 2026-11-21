using System.ComponentModel.DataAnnotations;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(422)]
public sealed class PayrollInputsController(IPayrollCalculationService payroll) : ControllerBase
{
    [HttpPost("employees/{employeeId:guid}/salary-history",Name="AppendSalaryHistory")]
    [Authorize(Policy=HrmPolicies.PayrollHistoryManage)]
    [ProducesResponseType<SalaryHistoryResponse>(201)]
    public async Task<ActionResult<SalaryHistoryResponse>> AppendSalary(Guid employeeId,[FromBody] AppendSalaryRequest request,
        [FromHeader(Name="Idempotency-Key"),Required] string key,CancellationToken token)
    {var result=await payroll.AppendSalaryAsync(employeeId,request,key,token);return CreatedAtRoute("ReadSalaryHistory",new{employeeId},result);}

    [HttpGet("employees/{employeeId:guid}/salary-history",Name="ReadSalaryHistory")]
    [Authorize(Policy=HrmPolicies.PayrollRead)]
    [ProducesResponseType<IReadOnlyList<SalaryHistoryResponse>>(200)]
    public async Task<ActionResult<IReadOnlyList<SalaryHistoryResponse>>> SalaryHistory(Guid employeeId,CancellationToken token)=>
        Ok(await payroll.SalaryHistoryAsync(employeeId,token));

    [HttpPost("payroll-periods/{periodId:guid}/adjustments",Name="AddPayrollAdjustment")]
    [Authorize(Policy=HrmPolicies.PayrollCalculate)]
    [ProducesResponseType<PayrollAdjustmentResponse>(201)]
    public async Task<ActionResult<PayrollAdjustmentResponse>> AddAdjustment(Guid periodId,[FromBody] AddPayrollAdjustmentRequest request,
        [FromHeader(Name="Idempotency-Key"),Required] string key,CancellationToken token)
    {var result=await payroll.AddAdjustmentAsync(periodId,request,key,token);return CreatedAtRoute("ReadPayrollAdjustments",new{periodId},result);}

    [HttpGet("payroll-periods/{periodId:guid}/adjustments",Name="ReadPayrollAdjustments")]
    [Authorize(Policy=HrmPolicies.PayrollRead)]
    [ProducesResponseType<IReadOnlyList<PayrollAdjustmentResponse>>(200)]
    public async Task<ActionResult<IReadOnlyList<PayrollAdjustmentResponse>>> Adjustments(Guid periodId,CancellationToken token)=>
        Ok(await payroll.AdjustmentsAsync(periodId,token));

    [HttpPost("payroll-adjustments/{id:guid}/void",Name="VoidPayrollAdjustment")]
    [Authorize(Policy=HrmPolicies.PayrollCalculate)]
    [ProducesResponseType<PayrollAdjustmentResponse>(200)]
    public async Task<ActionResult<PayrollAdjustmentResponse>> VoidAdjustment(Guid id,[FromBody] VoidPayrollAdjustmentRequest request,
        [FromHeader(Name="Idempotency-Key"),Required] string key,CancellationToken token)=>
        Ok(await payroll.VoidAdjustmentAsync(id,request,key,token));

    [HttpPost("payroll-periods/{periodId:guid}/snapshots",Name="CapturePayrollSnapshot")]
    [Authorize(Policy=HrmPolicies.PayrollCalculate)]
    [ProducesResponseType<PayrollBatchResponse>(201)]
    public async Task<ActionResult<PayrollBatchResponse>> Capture(Guid periodId,[FromBody] CapturePayrollRequest request,
        [FromHeader(Name="Idempotency-Key"),Required] string key,CancellationToken token)
    {var result=await payroll.CaptureAsync(periodId,request,key,token);return CreatedAtRoute("ReadPayrollSnapshot",new{periodId,version=result.Version},result);}

    [HttpGet("payroll-periods/{periodId:guid}/snapshots",Name="ReadPayrollSnapshot")]
    [Authorize(Policy=HrmPolicies.PayrollRead)]
    [ProducesResponseType<PayrollBatchResponse>(200)]
    public async Task<ActionResult<PayrollBatchResponse>> Snapshot(Guid periodId,CancellationToken token,[FromQuery,Range(1,int.MaxValue)]int? version=null)
    {var result=await payroll.ReadBatchAsync(periodId,version,token);return result is null?throw new PayrollNotFoundException("payroll.snapshot_not_found"):Ok(result);}
}
