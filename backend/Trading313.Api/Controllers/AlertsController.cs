using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Alerts;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Stocks;

namespace Trading313.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
[Produces("application/json")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IStockService _stocks;
    private readonly IQuoteService _quotes;

    public AlertsController(AppDbContext db, IStockService stocks, IQuoteService quotes)
    {
        _db = db;
        _stocks = stocks;
        _quotes = quotes;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PriceAlertDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var alerts = await _db.PriceAlerts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var dtos = new List<PriceAlertDto>(alerts.Count);
        foreach (var a in alerts) dtos.Add(await MapAsync(a, cancellationToken));
        return Ok(dtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PriceAlertDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAlertRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (request.TriggerPrice <= 0)
            return Problem(statusCode: 400, title: "InvalidPrice", detail: "Trigger price must be > 0.");
        var sym = request.Symbol.Trim().ToUpperInvariant();
        var stock = await _stocks.GetBySymbolAsync(sym, cancellationToken);
        if (stock is null)
            return Problem(statusCode: 400, title: "SymbolNotResolved", detail: $"Unknown symbol '{sym}'.");

        var alert = new PriceAlert
        {
            UserId = userId,
            Symbol = sym,
            Direction = request.Direction,
            TriggerPrice = request.TriggerPrice,
            Status = AlertStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Notes = request.Notes,
        };
        _db.PriceAlerts.Add(alert);
        await _db.SaveChangesAsync(cancellationToken);
        return StatusCode(201, await MapAsync(alert, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var alert = await _db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        if (alert is null) return NotFound();
        alert.Status = AlertStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/ack")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Acknowledge(long id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var alert = await _db.PriceAlerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
        if (alert is null) return NotFound();
        alert.Acknowledged = true;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<PriceAlertDto> MapAsync(PriceAlert a, CancellationToken cancellationToken)
    {
        var meta = await _db.Stocks
            .Where(s => s.Symbol == a.Symbol)
            .Select(s => new { s.LogoUrl, s.Name })
            .FirstOrDefaultAsync(cancellationToken);
        var quote = await _quotes.GetQuoteAsync(a.Symbol, cancellationToken);
        return new PriceAlertDto(
            Id: a.Id,
            Symbol: a.Symbol,
            Name: meta?.Name,
            LogoUrl: meta?.LogoUrl,
            Direction: a.Direction.ToString(),
            TriggerPrice: a.TriggerPrice,
            Status: a.Status.ToString(),
            CurrentPrice: quote?.Price,
            Acknowledged: a.Acknowledged,
            CreatedAt: a.CreatedAt,
            TriggeredAt: a.TriggeredAt,
            TriggeredPrice: a.TriggeredPrice,
            Notes: a.Notes);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
