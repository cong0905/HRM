using HRM.Next.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

public sealed record CurrentUserAccessResponse(
    Guid Id,
    string? Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
);

[ApiController]
[Authorize]
[Route("api/v1/access")]
public sealed class AccessController(ISecurityContext security) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType<CurrentUserAccessResponse>(200)]
    [ProducesResponseType<ProblemDetails>(401)]
    public ActionResult<CurrentUserAccessResponse> GetCurrentAccess()
    {
        Response.Headers.CacheControl = "no-store";
        if (security.CurrentUserId is not { } id)
        {
            return Unauthorized();
        }

        return Ok(new CurrentUserAccessResponse(
            id,
            security.CurrentUsername,
            security.Roles,
            security.Permissions
        ));
    }
}
