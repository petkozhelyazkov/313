using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Analytics;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.Analytics;

public class EarningsService : IEarningsService
{
    private static readonly TimeSpan EarningsTtl = TimeSpan.FromHours(24);

    private readonly AppDbContext _db;
    private readonly ITwelveDataClient _td;
    private readonly ILogger<EarningsService> _logger;

    public EarningsService(AppDbContext db, ITwelveDataClient td, ILogger<EarningsService> logger)
    {
        _db = db;
        _td = td;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EarningsCalendarItem>> GetUpcomingForUserAsync(string userId, int daysAhead, CancellationToken cancellationToken = default)
    {
        if (daysAhead is < 1 or > 90) daysAhead = 7;

        var heldSymbols = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .Select(p => p.Symbol)
            .ToListAsync(cancellationToken);
        var watchedSymbols = await _db.WatchlistItems
            .Where(w => w.UserId == userId)
            .Select(w => w.Symbol)
            .ToListAsync(cancellationToken);

        var symbols = heldSymbols.Concat(watchedSymbols)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (symbols.Count == 0) return Array.Empty<EarningsCalendarItem>();

        // Refresh stale entries. Pre-compute the freshness threshold so EF treats
        // it as a DateTime parameter; Pomelo's MySQL provider can't serialize a
        // raw TimeSpan literal (DateTime.UtcNow - TimeSpan), which throws
        // FormatException at SQL generation time.
        var freshThreshold = DateTime.UtcNow - EarningsTtl;
        foreach (var symbol in symbols)
        {
            var anyFresh = await _db.EarningsEntries
                .AnyAsync(e => e.Symbol == symbol && e.FetchedAt > freshThreshold, cancellationToken);
            if (!anyFresh)
            {
                await RefreshSymbolAsync(symbol, cancellationToken);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var until = today.AddDays(daysAhead);
        // Twelve Data's free /earnings endpoint returns past reports only — there's
        // no forward calendar. Widen the window backwards 90 days so the widget
        // shows recent reports for symbols the user holds or watches.
        var since = today.AddDays(-90);

        var rows = await _db.EarningsEntries
            .Where(e => symbols.Contains(e.Symbol) && e.ReportDate >= since && e.ReportDate <= until)
            .OrderByDescending(e => e.ReportDate)
            .ThenBy(e => e.Symbol)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return Array.Empty<EarningsCalendarItem>();

        var meta = await _db.Stocks
            .Where(s => symbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.LogoUrl, s.Name })
            .ToDictionaryAsync(x => x.Symbol, cancellationToken);

        var heldSet = heldSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var watchedSet = watchedSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);

        return rows.Select(r =>
        {
            meta.TryGetValue(r.Symbol, out var m);
            return new EarningsCalendarItem(
                Symbol: r.Symbol,
                CompanyName: m?.Name,
                LogoUrl: m?.LogoUrl,
                ReportDate: r.ReportDate,
                Time: r.Time,
                EpsEstimate: r.EpsEstimate,
                EpsActual: r.EpsActual,
                IsHeld: heldSet.Contains(r.Symbol),
                IsWatched: watchedSet.Contains(r.Symbol));
        }).ToList();
    }

    private async Task RefreshSymbolAsync(string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _td.GetEarningsAsync(symbol, cancellationToken);
            if (entries.Count == 0) return;

            var sym = symbol.ToUpperInvariant();
            var existing = await _db.EarningsEntries
                .Where(e => e.Symbol == sym)
                .ToDictionaryAsync(e => e.ReportDate, cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var e in entries)
            {
                if (existing.TryGetValue(e.ReportDate, out var row))
                {
                    row.Time = e.Time;
                    row.EpsEstimate = e.EpsEstimate;
                    row.EpsActual = e.EpsActual;
                    row.SurprisePercent = e.SurprisePercent;
                    row.FetchedAt = now;
                }
                else
                {
                    _db.EarningsEntries.Add(new EarningsEntry
                    {
                        Symbol = sym,
                        ReportDate = e.ReportDate,
                        Time = e.Time,
                        EpsEstimate = e.EpsEstimate,
                        EpsActual = e.EpsActual,
                        SurprisePercent = e.SurprisePercent,
                        FetchedAt = now,
                    });
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Earnings refresh failed for {Symbol}", symbol);
        }
    }
}
