using System.ComponentModel.DataAnnotations;
using HRM.Next.Application.Authorization;
using HRM.Next.Application.Payroll;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;
[ApiController]
[Route("api/v1/payroll-periods")]
[ProducesResponseType<ProblemDetails>(400)]
[ProducesResponseType<ProblemDetails>(401)]
[ProducesResponseType<ProblemDetails>(403)]
[ProducesResponseType<ProblemDetails>(404)]
[ProducesResponseType<ProblemDetails>(409)]
[ProducesResponseType<ProblemDetails>(422)]
public sealed class PayrollWorkflowController(IPayrollWorkflowService workflow) : ControllerBase
{
    [HttpPost(Name="CreatePayrollPeriod")]
    [Authorize(Policy=HrmPolicies.PayrollCalculate)]
    [ProducesResponseType<PayrollWorkflowState>(201)]
    public async Task<ActionResult<PayrollWorkflowState>> Create(CreatePayrollPeriodRequest request,[FromHeader(Name="Idempotency-Key"),Required]string key,CancellationToken token)
    {var state=await workflow.CreateAsync(request,key,token);return CreatedAtRoute("ReadPayrollWorkflowState",new{id=state.Id},state);}
    [HttpGet("{id:guid}",Name="ReadPayrollWorkflowState")]
    [Authorize(Policy=HrmPolicies.PayrollRead)]
    [ProducesResponseType<PayrollWorkflowState>(200)]
    public async Task<ActionResult<PayrollWorkflowState>> State(Guid id,CancellationToken token)=>Ok(await workflow.StateAsync(id,token));
    [HttpPost("{id:guid}/workflow/{operation}",Name="ExecutePayrollWorkflow")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Execute(Guid id,string operation,PayrollWorkflowRequest request,[FromHeader(Name="Idempotency-Key"),Required]string key,CancellationToken token)
    {await workflow.ExecuteAsync(id,operation,request,key,token);return NoContent();}
}
