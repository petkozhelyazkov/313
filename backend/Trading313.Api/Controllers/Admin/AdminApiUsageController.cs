using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trading313.Api.Data;
using Trading313.Api.Dtos.Admin;
using Trading313.Api.Infrastructure.MarketData;
using Trading313.Api.Infrastructure.Seeding;

namespace Trading313.Api.Controllers.Admin;

/// <summary>
/// Admin-only Twelve Data quota snapshot. Powers the system panel in the admin frontend.
/// </summary>
[ApiController]
[Route("api/admin/api-usage")]
[Authorize(Roles = RoleNames.Admin)]
[Produces("application/json")]
public class AdminApiUsageController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TwelveDataOptions _options;

    public AdminApiUsageController(AppDbContext db, IOptions<TwelveDataOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiUsageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var startOfDay = DateOnly.FromDateTime(now).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var hourAgo = now.AddHours(-1);

        var todayCount = await _db.ApiUsageLog.CountAsync(e => e.RequestedAt >= startOfDay, cancellationToken);
        var lastHourCount = await _db.ApiUsageLog.CountAsync(e => e.RequestedAt >= hourAgo, cancellationToken);

        var recent = await _db.ApiUsageLog
            .OrderByDescending(e => e.RequestedAt)
            .Take(20)
            .Select(e => new ApiUsageCallEntry(e.Id, e.Endpoint, e.Symbols, e.RequestedAt, e.StatusCode, e.ResponseTimeMs))
            .ToListAsync(cancellationToken);

        var dailyQuota = _options.RequestsPerDay;
        var hourlyQuota = _options.RequestsPerMinute * 60;

        var response = new ApiUsageResponse(
            Today: new ApiUsageWindow(todayCount, dailyQuota, dailyQuota == 0 ? 0 : 100.0 * todayCount / dailyQuota),
            LastHour: new ApiUsageWindow(lastHourCount, hourlyQuota, hourlyQuota == 0 ? 0 : 100.0 * lastHourCount / hourlyQuota),
            RecentCalls: recent);

        return Ok(response);
    }
}
