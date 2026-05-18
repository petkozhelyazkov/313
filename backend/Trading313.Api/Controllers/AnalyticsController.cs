using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Dtos.Analytics;
using Trading313.Api.Services.Analytics;
using Trading313.Api.Services.MarketData;

namespace Trading313.Api.Controllers;

/// <summary>
/// Authenticated analytics endpoints powering the Analytics page line/pie/bar charts.
/// </summary>
[ApiController]
[Route("api/analytics")]
[Authorize]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, int> RangeToDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["1M"] = 31,
        ["3M"] = 93,
        ["6M"] = 186,
        ["1Y"] = 366,
        ["MAX"] = 366 * 10,
    };

    private readonly AppDbContext _db;
    private readonly ISnapshotService _snapshots;
    private readonly IQuoteService _quotes;
    private readonly IEarningsService _earnings;
    private readonly IHistoryService _history;
    private readonly IAdvancedMetricsService _advanced;
    private readonly Services.Stocks.ICompanyProfileService _profiles;

    public AnalyticsController(
        AppDbContext db,
        ISnapshotService snapshots,
        IQuoteService quotes,
        IEarningsService earnings,
        IHistoryService history,
        IAdvancedMetricsService advanced,
        Services.Stocks.ICompanyProfileService profiles)
    {
        _db = db;
        _snapshots = snapshots;
        _quotes = quotes;
        _earnings = earnings;
        _history = history;
        _advanced = advanced;
        _profiles = profiles;
    }

    /// <summary>TWR, MWR, Sortino, best/worst day, win rate — advanced performance metrics.</summary>
    [HttpGet("advanced")]
    [ProducesResponseType(typeof(AdvancedMetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAdvanced([FromQuery] string range = "1Y", CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var result = await _advanced.ComputeAsync(userId, range, cancellationToken);
        return Ok(result);
    }

    /// <summary>Upcoming earnings for symbols the user holds or watches.</summary>
    [HttpGet("earnings-calendar")]
    [ProducesResponseType(typeof(IEnumerable<EarningsCalendarItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarningsCalendar([FromQuery] int days = 7, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var items = await _earnings.GetUpcomingForUserAsync(userId, days, cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Historical end-of-day portfolio value. On first call (no snapshots yet), backfills
    /// from the user's earliest transaction.
    /// </summary>
    [HttpGet("snapshots")]
    [ProducesResponseType(typeof(IEnumerable<SnapshotPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSnapshots(
        [FromQuery] string range = "1Y",
        [FromQuery] bool includeBenchmark = false,
        [FromQuery] string benchmarkSymbol = "SPY",
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (!RangeToDays.TryGetValue(range, out var days))
        {
            range = "1Y";
            days = RangeToDays["1Y"];
        }

        var existing = await _db.DailyPortfolioSnapshots.AnyAsync(s => s.UserId == userId, cancellationToken);
        if (!existing)
        {
            await _snapshots.BackfillAsync(userId, cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-days);

        var points = await _db.DailyPortfolioSnapshots
            .Where(s => s.UserId == userId && s.SnapshotDate >= from)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new SnapshotPoint(
                s.SnapshotDate,
                s.TotalValue,
                s.CashBalance,
                s.HoldingsValue,
                s.TotalInvestedAtSnapshot,
                s.UnrealizedPl,
                null))
            .ToListAsync(cancellationToken);

        // Append today's live value as the trailing point.
        var todayLive = await ComputeTodayLiveAsync(userId, cancellationToken);
        if (todayLive is not null)
        {
            if (points.Count == 0 || points[^1].Date != today) points.Add(todayLive);
            else points[^1] = todayLive;
        }

        // Overlay benchmark series — normalized so it starts at the user's first
        // portfolio value, making "did I beat the market?" visually obvious.
        if (includeBenchmark && points.Count > 0)
        {
            points = await AttachBenchmarkAsync(points, benchmarkSymbol, cancellationToken);
        }

        return Ok(points);
    }

    private async Task<List<SnapshotPoint>> AttachBenchmarkAsync(List<SnapshotPoint> points, string symbol, CancellationToken cancellationToken)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var firstDate = points[0].Date;
        var lastDate = points[^1].Date;

        var prices = await _db.HistoricalPrices
            .Where(h => h.Symbol == sym && h.Date >= firstDate.AddDays(-7) && h.Date <= lastDate)
            .OrderBy(h => h.Date)
            .Select(h => new { h.Date, h.Close })
            .ToListAsync(cancellationToken);

        // If we don't have benchmark history cached yet, request it and try again once.
        if (prices.Count == 0)
        {
            await _history.GetHistoryAsync(sym, "5Y", cancellationToken);
            prices = await _db.HistoricalPrices
                .Where(h => h.Symbol == sym && h.Date >= firstDate.AddDays(-7) && h.Date <= lastDate)
                .OrderBy(h => h.Date)
                .Select(h => new { h.Date, h.Close })
                .ToListAsync(cancellationToken);
        }
        if (prices.Count == 0) return points;

        // Anchor: starting portfolio value / starting benchmark close.
        var startValue = points[0].TotalValue;
        var startClose = prices[0].Close;
        if (startClose <= 0) return points;

        var priceByDate = new SortedDictionary<DateOnly, decimal>();
        foreach (var p in prices) priceByDate[p.Date] = p.Close;

        decimal? CloseOn(DateOnly d)
        {
            if (priceByDate.TryGetValue(d, out var x)) return x;
            decimal? prev = null;
            foreach (var (k, v) in priceByDate)
            {
                if (k > d) break;
                prev = v;
            }
            return prev;
        }

        return points.Select(pt =>
        {
            var close = CloseOn(pt.Date);
            decimal? benchmark = close is null ? (decimal?)null : startValue * (close.Value / startClose);
            return pt with { Benchmark = benchmark };
        }).ToList();
    }

    /// <summary>Current portfolio allocation by symbol.</summary>
    [HttpGet("allocation")]
    [ProducesResponseType(typeof(IEnumerable<AllocationSlice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllocation(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var openPositions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed)
            .ToListAsync(cancellationToken);

        var values = new List<(string Symbol, decimal Value, decimal Quantity)>();
        decimal total = 0m;
        foreach (var p in openPositions)
        {
            var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            var price = quote?.Price ?? p.AverageCost;
            var v = p.Quantity * price;
            values.Add((p.Symbol, v, p.Quantity));
            total += v;
        }

        var slices = values
            .OrderByDescending(v => v.Value)
            .Select(v => new AllocationSlice(
                v.Symbol,
                v.Value,
                total == 0 ? 0m : (v.Value / total) * 100m,
                v.Quantity))
            .ToList();

        return Ok(slices);
    }

    /// <summary>Allocation grouped by sector (from Stocks.Sector enriched via /profile).</summary>
    [HttpGet("sector-allocation")]
    [ProducesResponseType(typeof(IEnumerable<SectorSlice>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectorAllocation(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var positions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);

        var symbols = positions.Select(p => p.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var sectors = await _db.Stocks
            .Where(s => symbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.Sector })
            .ToDictionaryAsync(s => s.Symbol, s => s.Sector, cancellationToken);

        // Opportunistically fetch the company profile for any held symbol whose
        // Sector is still empty. CompanyProfileService caches with a 7-day TTL
        // and silently no-ops if Twelve Data is rate-limited — so this is safe
        // to call every time the analytics page loads.
        var missing = symbols
            .Where(s => !sectors.TryGetValue(s, out var sec) || string.IsNullOrEmpty(sec))
            .ToList();
        if (missing.Count > 0)
        {
            foreach (var sym in missing)
            {
                try
                {
                    await _profiles.GetAsync(sym, cancellationToken);
                }
                catch
                {
                    /* fall through — keep showing "Unknown" rather than failing the whole call */
                }
            }
            // Re-read sectors after the refresh attempts.
            sectors = await _db.Stocks
                .Where(s => symbols.Contains(s.Symbol))
                .Select(s => new { s.Symbol, s.Sector })
                .ToDictionaryAsync(s => s.Symbol, s => s.Sector, cancellationToken);
        }

        var bySector = new Dictionary<string, (decimal Value, int Count)>(StringComparer.OrdinalIgnoreCase);
        decimal total = 0m;
        foreach (var p in positions)
        {
            var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            var price = quote?.Price ?? p.AverageCost;
            var v = p.Quantity * price;
            total += v;
            var sector = sectors.TryGetValue(p.Symbol, out var s) && !string.IsNullOrEmpty(s) ? s : "Unknown";
            if (!bySector.TryGetValue(sector, out var prev))
                bySector[sector] = (v, 1);
            else
                bySector[sector] = (prev.Value + v, prev.Count + 1);
        }

        var slices = bySector
            .OrderByDescending(kv => kv.Value.Value)
            .Select(kv => new SectorSlice(
                kv.Key,
                kv.Value.Value,
                total == 0 ? 0m : (kv.Value.Value / total) * 100m,
                kv.Value.Count))
            .ToList();
        return Ok(slices);
    }

    /// <summary>Portfolio risk metrics computed from daily snapshots + per-symbol beta.</summary>
    [HttpGet("risk")]
    [ProducesResponseType(typeof(RiskMetricsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRisk(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-366);
        var snapshots = await _db.DailyPortfolioSnapshots
            .Where(s => s.UserId == userId && s.SnapshotDate >= since)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => s.TotalValue)
            .ToListAsync(cancellationToken);

        if (snapshots.Count < 5)
            return Ok(new RiskMetricsResponse(null, null, null, null, snapshots.Count));

        var returns = new List<double>(snapshots.Count - 1);
        for (int i = 1; i < snapshots.Count; i++)
        {
            var prev = (double)snapshots[i - 1];
            if (prev <= 0) continue;
            returns.Add(((double)snapshots[i] - prev) / prev);
        }
        if (returns.Count < 2)
            return Ok(new RiskMetricsResponse(null, null, null, null, snapshots.Count));

        var mean = returns.Average();
        var variance = returns.Sum(r => (r - mean) * (r - mean)) / (returns.Count - 1);
        var dailyStdev = Math.Sqrt(variance);
        var annualizedVol = dailyStdev * Math.Sqrt(252);
        var annualizedReturn = mean * 252;
        const double rfRate = 0.04;
        var sharpe = annualizedVol > 0 ? (annualizedReturn - rfRate) / annualizedVol : 0;

        decimal peak = snapshots[0];
        decimal maxDrawdown = 0m;
        foreach (var v in snapshots)
        {
            if (v > peak) peak = v;
            if (peak > 0)
            {
                var dd = (peak - v) / peak;
                if (dd > maxDrawdown) maxDrawdown = dd;
            }
        }

        var openPositions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);
        decimal? portfolioBeta = null;
        if (openPositions.Count > 0)
        {
            decimal totalValue = 0m;
            decimal weightedBeta = 0m;
            int withBeta = 0;
            foreach (var p in openPositions)
            {
                var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
                var price = quote?.Price ?? p.AverageCost;
                var value = p.Quantity * price;
                totalValue += value;
                var stockBeta = await _db.Stocks
                    .Where(s => s.Symbol == p.Symbol)
                    .Select(s => s.Beta)
                    .FirstOrDefaultAsync(cancellationToken);
                if (stockBeta is { } b)
                {
                    weightedBeta += value * b;
                    withBeta++;
                }
            }
            portfolioBeta = (totalValue > 0 && withBeta > 0) ? weightedBeta / totalValue : null;
        }

        return Ok(new RiskMetricsResponse(
            Beta: portfolioBeta,
            AnnualizedVolatility: (decimal)annualizedVol * 100m,
            SharpeRatio: (decimal)sharpe,
            MaxDrawdown: maxDrawdown * 100m,
            DataPoints: snapshots.Count));
    }

    /// <summary>0-100 diversification score with breakdown + suggestions.</summary>
    [HttpGet("diversification")]
    [ProducesResponseType(typeof(DiversificationResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiversification(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var positions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);

        if (positions.Count == 0)
        {
            return Ok(new DiversificationResponse(
                Score: 0, PositionsCount: 0, SectorsCount: 0,
                LargestPositionPct: 0m, LargestSectorPct: 0m,
                Verdict: "No open positions yet.",
                Suggestions: new[] { "Make your first trade to start tracking diversification." }));
        }

        var symbols = positions.Select(p => p.Symbol).ToList();
        var sectors = await _db.Stocks
            .Where(s => symbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.Sector })
            .ToDictionaryAsync(s => s.Symbol, s => s.Sector ?? "Unknown", cancellationToken);

        decimal totalValue = 0m;
        var values = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var sectorValues = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in positions)
        {
            var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            var price = quote?.Price ?? p.AverageCost;
            var v = p.Quantity * price;
            values[p.Symbol] = v;
            totalValue += v;
            var sector = sectors.TryGetValue(p.Symbol, out var s) ? s : "Unknown";
            sectorValues[sector] = sectorValues.GetValueOrDefault(sector) + v;
        }
        if (totalValue == 0)
            return Ok(new DiversificationResponse(0, positions.Count, sectorValues.Count, 0m, 0m,
                "Holdings have no current value.", Array.Empty<string>()));

        var largestPositionPct = values.Values.Max() / totalValue * 100m;
        var largestSectorPct = sectorValues.Values.Max() / totalValue * 100m;
        var sectorsCount = sectorValues.Count(kv => kv.Key != "Unknown");

        var positionScore = Math.Min(positions.Count / 10.0, 1.0) * 100;
        var concentrationScore = Math.Max(0.0, 1.0 - ((double)largestPositionPct - 5.0) / 35.0) * 100;
        var sectorScore = 0.0;
        if (sectorsCount > 0)
        {
            double entropy = 0;
            foreach (var kv in sectorValues.Where(kv => kv.Key != "Unknown"))
            {
                var w = (double)(kv.Value / totalValue);
                if (w > 0) entropy -= w * Math.Log(w);
            }
            var maxEntropy = Math.Log(Math.Max(2, sectorsCount));
            sectorScore = Math.Min(1.0, entropy / maxEntropy) * 100;
        }

        var score = (int)Math.Round((positionScore + concentrationScore + sectorScore) / 3.0);
        score = Math.Max(0, Math.Min(100, score));

        var suggestions = new List<string>();
        if (positions.Count < 5)
            suggestions.Add($"Hold at least 5–10 positions (currently {positions.Count}).");
        if (largestPositionPct > 30m)
            suggestions.Add($"Your largest position is {largestPositionPct:F0}% — consider trimming below 30%.");
        if (largestSectorPct > 40m)
            suggestions.Add($"You're {largestSectorPct:F0}% in one sector — consider adding positions outside it.");
        if (sectorsCount < 3)
            suggestions.Add("Spread across at least 3 sectors for better risk balancing.");
        if (suggestions.Count == 0)
            suggestions.Add("Your portfolio is well-diversified across positions and sectors.");

        var verdict = score >= 80 ? "Excellent diversification"
            : score >= 60 ? "Good diversification — minor improvements possible"
            : score >= 40 ? "Moderate diversification — concentration risk exists"
            : score >= 20 ? "Low diversification — significant concentration risk"
            : "Highly concentrated — single-stock/sector exposure";

        return Ok(new DiversificationResponse(
            Score: score,
            PositionsCount: positions.Count,
            SectorsCount: sectorsCount,
            LargestPositionPct: largestPositionPct,
            LargestSectorPct: largestSectorPct,
            Verdict: verdict,
            Suggestions: suggestions));
    }

    /// <summary>Realized + unrealized P/L per symbol.</summary>
    [HttpGet("returns")]
    [ProducesResponseType(typeof(IEnumerable<ReturnsRow>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReturns(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var positions = await _db.Positions
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        var rows = new List<ReturnsRow>();
        foreach (var p in positions)
        {
            decimal unrealized = 0m;
            if (!p.IsClosed && p.Quantity > 0)
            {
                var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
                var price = quote?.Price ?? p.AverageCost;
                unrealized = (price - p.AverageCost) * p.Quantity;
            }

            var total = unrealized + p.RealizedPlLifetime;
            decimal? totalPct = p.TotalInvested == 0 ? null : (total / p.TotalInvested) * 100m;
            rows.Add(new ReturnsRow(p.Symbol, unrealized, p.RealizedPlLifetime, total, totalPct));
        }

        return Ok(rows.OrderByDescending(r => r.TotalPl));
    }

    private async Task<SnapshotPoint?> ComputeTodayLiveAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _db.Set<Trading313.Api.Domain.Entities.ApplicationUser>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return null;

        var openPositions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .ToListAsync(cancellationToken);

        decimal holdings = 0m;
        decimal cost = 0m;
        foreach (var p in openPositions)
        {
            var quote = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            var price = quote?.Price ?? p.AverageCost;
            holdings += p.Quantity * price;
            cost += p.Quantity * p.AverageCost;
        }

        var totalValue = user.CashBalance + holdings;
        return new SnapshotPoint(
            DateOnly.FromDateTime(DateTime.UtcNow),
            totalValue,
            user.CashBalance,
            holdings,
            cost,
            holdings - cost);
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Authenticated user missing NameIdentifier claim.");
}
