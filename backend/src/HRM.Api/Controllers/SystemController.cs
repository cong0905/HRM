using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Next.Api.Controllers;

public sealed record SystemInfo(string ApiVersion, DateTimeOffset ServerTimeUtc, string BusinessTimeZone);

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(TimeProvider clock) : ControllerBase
{
    [HttpGet("info", Name = "GetSystemInfo")]
    [AllowAnonymous]
    public ActionResult<SystemInfo> Info() => new SystemInfo("v1", clock.GetUtcNow(), "Asia/Ho_Chi_Minh");
}
