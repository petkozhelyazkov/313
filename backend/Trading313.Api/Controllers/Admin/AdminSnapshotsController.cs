using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Infrastructure.Seeding;
using Trading313.Api.Services.Analytics;

namespace Trading313.Api.Controllers.Admin;

/// <summary>
/// Admin endpoint to manually trigger the daily snapshot job (handy for demos).
/// </summary>
[ApiController]
[Route("api/admin/snapshots")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public class AdminSnapshotsController : ControllerBase
{
    private readonly ISnapshotService _snapshots;

    public AdminSnapshotsController(ISnapshotService snapshots)
    {
        _snapshots = snapshots;
    }

    [HttpPost("run-now")]
    public async Task<IActionResult> RunNow(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var processed = await _snapshots.RunDailyForAllUsersAsync(today, cancellationToken);
        return Ok(new { date = today.ToString("yyyy-MM-dd"), processedUsers = processed });
    }
}
