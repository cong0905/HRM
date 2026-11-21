using HRM.Next.Application.Common;
using System.ComponentModel.DataAnnotations;
using HRM.Next.Application.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

[ApiController]
[Route("api/v1/departments")]
[Authorize(Policy = "Departments.Read")]
public sealed class DepartmentsController(IDepartmentQueries queries) : ControllerBase
{
    [HttpGet(Name = "ListDepartments")]
    [ProducesResponseType<PagedResult<DepartmentResponse>>(200)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(401)]
    [ProducesResponseType<ProblemDetails>(403)]
    [ProducesResponseType<ProblemDetails>(500)]
    public async Task<ActionResult<PagedResult<DepartmentResponse>>> List(
        CancellationToken cancellationToken, [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        [FromQuery, RegularExpression("^(name|-name|createdAt|-createdAt)$")] string sort = "name") =>
        Ok(await queries.ListAsync(PageRequest.Create(page, pageSize, sort), cancellationToken));
}
