using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Dtos.Dividends;
using Trading313.Api.Services.Dividends;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/dividends")]
[Produces("application/json")]
public class DividendsController : ControllerBase
{
    private readonly IDividendsService _dividends;

    public DividendsController(IDividendsService dividends)
    {
        _dividends = dividends;
    }

    /// <summary>Public dividend history for a single symbol.</summary>
    [HttpGet("history/{symbol}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<DividendHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(string symbol, CancellationToken cancellationToken)
    {
        var items = await _dividends.GetHistoryAsync(symbol, cancellationToken);
        return Ok(items);
    }

    /// <summary>Upcoming dividends across the current user's open positions (next 90 days).</summary>
    [HttpGet("upcoming")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UpcomingDividendItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upcoming(CancellationToken cancellationToken)
    {
        var items = await _dividends.GetUpcomingAsync(GetUserId(), cancellationToken);
        return Ok(items);
    }

    /// <summary>Past dividends the user would have received based on holdings at each ex-date.</summary>
    [HttpGet("received")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<ReceivedDividendItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Received(CancellationToken cancellationToken)
    {
        var items = await _dividends.GetReceivedAsync(GetUserId(), cancellationToken);
        return Ok(items);
    }

    /// <summary>Lifetime / 12-month / next-30-day dividend summary.</summary>
    [HttpGet("summary")]
    [Authorize]
    [ProducesResponseType(typeof(DividendSummary), StatusCodes.Status200OK)]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var s = await _dividends.GetSummaryAsync(GetUserId(), cancellationToken);
        return Ok(s);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
