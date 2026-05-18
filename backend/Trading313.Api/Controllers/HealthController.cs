using Microsoft.AspNetCore.Mvc;

namespace Trading313.Api.Controllers;

/// <summary>
/// Liveness probe. Anonymous-allowed.
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    /// <summary>Returns a simple status payload so the frontend HealthBadge knows the API is up.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        timestamp = DateTimeOffset.UtcNow,
        service = "Trading313.Api"
    });
}
