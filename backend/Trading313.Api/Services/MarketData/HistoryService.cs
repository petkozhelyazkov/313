using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.MarketData;

public class HistoryService : IHistoryService
{
    private static readonly IReadOnlyDictionary<string, int> RangeToDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["1M"] = 31,
        ["3M"] = 93,
        ["6M"] = 186,
        ["1Y"] = 366,
        ["5Y"] = 366 * 5,
        ["MAX"] = 366 * 10,
    };

    private readonly AppDbContext _db;
    private readonly ITwelveDataClient _td;
    private readonly ILogger<HistoryService> _logger;

    public HistoryService(AppDbContext db, ITwelveDataClient td, ILogger<HistoryService> logger)
    {
        _db = db;
        _td = td;
        _logger = logger;
    }

    public async Task<HistoryResponse?> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        if (!RangeToDays.TryGetValue(range, out var days))
        {
            range = "1Y";
            days = RangeToDays["1Y"];
        }

        var sym = symbol.Trim().ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var requestedStart = today.AddDays(-days);

        // What's the latest date we already have in cache?
        var latestCached = await _db.HistoricalPrices
            .Where(h => h.Symbol == sym)
            .OrderByDescending(h => h.Date)
            .Select(h => (DateOnly?)h.Date)
            .FirstOrDefaultAsync(cancellationToken);

        var earliestCached = await _db.HistoricalPrices
            .Where(h => h.Symbol == sym)
            .OrderBy(h => h.Date)
            .Select(h => (DateOnly?)h.Date)
            .FirstOrDefaultAsync(cancellationToken);

        bool needsPrefix = earliestCached is null || earliestCached > requestedStart;
        // Re-fetch only if cache is older than 3 calendar days — bridges weekends and
        // most one-day holidays without burning API quota on every page load.
        bool needsSuffix = latestCached is null || (today.DayNumber - latestCached.Value.DayNumber) > 3;

        if (needsPrefix || needsSuffix)
        {
            // Simplification: re-fetch the entire requested range. Twelve Data charges
            // one credit per call regardless of point count, and outputsize covers up to 5000.
            DateOnly fetchFrom = needsPrefix
                ? requestedStart
                : latestCached!.Value.AddDays(1);

            await TryFetchAndPersistAsync(sym, fetchFrom, today, cancellationToken);
        }

        var points = await _db.HistoricalPrices
            .Where(h => h.Symbol == sym && h.Date >= requestedStart)
            .OrderBy(h => h.Date)
            .Select(h => new HistoryPoint(h.Date, h.Open, h.High, h.Low, h.Close, h.Volume))
            .ToListAsync(cancellationToken);

        if (points.Count == 0) return null;
        return new HistoryResponse(sym, range, points);
    }

    private async Task TryFetchAndPersistAsync(string symbol, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        try
        {
            var series = await _td.GetTimeSeriesAsync(symbol, "1day", startDate, endDate, outputSize: 5000, cancellationToken);
            if (series is null || series.Points.Count == 0) return;

            var dates = series.Points.Select(p => DateOnly.FromDateTime(p.Date.Date)).ToHashSet();

            // Avoid duplicate key conflicts: load any already-cached rows in this range.
            var existing = await _db.HistoricalPrices
                .Where(h => h.Symbol == symbol && h.Date >= startDate && h.Date <= endDate)
                .ToDictionaryAsync(h => h.Date, cancellationToken);

            foreach (var p in series.Points)
            {
                var date = DateOnly.FromDateTime(p.Date.Date);
                if (existing.ContainsKey(date)) continue;
                _db.HistoricalPrices.Add(new HistoricalPrice
                {
                    Symbol = symbol,
                    Date = date,
                    Open = p.Open,
                    High = p.High,
                    Low = p.Low,
                    Close = p.Close,
                    Volume = p.Volume,
                });
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (TwelveDataRateLimitException)
        {
            _logger.LogWarning("Rate-limited fetching history for {Symbol}; returning whatever is cached", symbol);
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "History fetch failed for {Symbol}", symbol);
        }
    }
}
