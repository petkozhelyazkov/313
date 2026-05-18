using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trading313.Api.Services.Stocks;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/splits")]
[Produces("application/json")]
public class SplitsController : ControllerBase
{
    private readonly IStockSplitsService _splits;

    public SplitsController(IStockSplitsService splits)
    {
        _splits = splits;
    }

    public record SplitHistoryItem(string Symbol, DateOnly Date, decimal FromFactor, decimal ToFactor, string Ratio);

    [HttpGet("history/{symbol}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<SplitHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> History(string symbol, CancellationToken cancellationToken)
    {
        var items = await _splits.GetHistoryAsync(symbol, cancellationToken);
        return Ok(items.Select(s => new SplitHistoryItem(
            s.Symbol, s.Date, s.FromFactor, s.ToFactor,
            $"{Format(s.ToFactor)}-for-{Format(s.FromFactor)}")));
    }

    private static string Format(decimal v) => v % 1 == 0 ? ((int)v).ToString() : v.ToString("0.##");
}
