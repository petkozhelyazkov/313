using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Infrastructure.MarketData;
using Trading313.Api.Realtime;

namespace Trading313.Api.Background;

/// <summary>
/// Periodically refreshes the PriceCache for every symbol currently held in
/// any open Position or any WatchlistItem. One batched Twelve Data /quote call
/// per tick — the only realistic way to stay within 8 req/min for many users.
/// </summary>
public class QuoteRefreshService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);
    private const int MaxSymbolsPerCall = 120;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITwelveDataClient _td;
    private readonly TwelveDataRateLimiter _limiter;
    private readonly ILogger<QuoteRefreshService> _logger;

    public QuoteRefreshService(
        IServiceScopeFactory scopeFactory,
        ITwelveDataClient td,
        TwelveDataRateLimiter limiter,
        ILogger<QuoteRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _td = td;
        _limiter = limiter;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QuoteRefreshService started; tick every {Seconds}s during US market hours", TickInterval.TotalSeconds);

        // Wait one full interval on startup so we don't compete with the rate limiter during boot.
        try { await Task.Delay(TickInterval, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TickInterval);
        while (await SafeWaitAsync(timer, stoppingToken))
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "QuoteRefreshService tick threw");
            }
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { return await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        if (!MarketHoursClock.IsLikelyOpen(DateTime.UtcNow))
        {
            _logger.LogDebug("Skipped: market closed");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var positionSymbols = await db.Positions
            .Where(p => !p.IsClosed && p.Quantity > 0)
            .Select(p => p.Symbol)
            .ToListAsync(cancellationToken);
        var watchSymbols = await db.WatchlistItems
            .Select(w => w.Symbol)
            .ToListAsync(cancellationToken);

        var symbols = positionSymbols
            .Concat(watchSymbols)
            .Select(s => s.ToUpperInvariant())
            .Distinct()
            .Take(MaxSymbolsPerCall)
            .ToList();

        if (symbols.Count == 0)
        {
            _logger.LogDebug("Skipped: no symbols to refresh");
            return;
        }

        IReadOnlyDictionary<string, Infrastructure.MarketData.Models.TdQuote> quotes;
        try
        {
            quotes = await _td.GetQuotesAsync(symbols, cancellationToken);
        }
        catch (TwelveDataRateLimitException)
        {
            _logger.LogWarning("Quote refresh rate-limited; will retry next tick");
            return;
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Quote refresh fetch failed");
            return;
        }

        if (quotes.Count == 0) return;

        var existing = await db.PriceCache
            .Where(p => symbols.Contains(p.Symbol))
            .ToDictionaryAsync(p => p.Symbol, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var (symbol, q) in quotes)
        {
            if (!existing.TryGetValue(symbol, out var row))
            {
                row = new PriceCacheEntry { Symbol = symbol };
                db.PriceCache.Add(row);
            }
            row.Price = q.Price;
            row.DayChange = q.Change;
            row.DayChangePct = q.PercentChange;
            row.PreviousClose = q.PreviousClose;
            row.Volume = q.Volume;
            row.FetchedAt = now;
            row.IsStale = false;
        }

        await db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Quote refresh: {Returned}/{Requested} symbols updated; today's quota used: {Today}",
            quotes.Count, symbols.Count, _limiter.TodayCountSnapshot);

        // Push live ticks to any subscribed WebSocket clients.
        var publisher = scope.ServiceProvider.GetService<IPricePublisher>();
        if (publisher is not null)
        {
            var payload = quotes.Select(kv => new QuoteResponse(
                Symbol: kv.Key,
                Price: kv.Value.Price,
                DayChange: kv.Value.Change,
                DayChangePct: kv.Value.PercentChange,
                PreviousClose: kv.Value.PreviousClose,
                Volume: kv.Value.Volume,
                FetchedAt: now,
                IsStale: false)).ToList();
            try { await publisher.PublishAsync(payload, cancellationToken); }
            catch (Exception ex) { _logger.LogDebug(ex, "PricePublisher failed"); }
        }
    }
}
