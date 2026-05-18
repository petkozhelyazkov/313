using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.MarketData;

public class QuoteService : IQuoteService
{
    private static readonly TimeSpan FreshnessWindow = TimeSpan.FromSeconds(60);

    private readonly AppDbContext _db;
    private readonly ITwelveDataClient _td;
    private readonly IMemoryCache _cache;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(AppDbContext db, ITwelveDataClient td, IMemoryCache cache, ILogger<QuoteService> logger)
    {
        _db = db;
        _td = td;
        _cache = cache;
        _logger = logger;
    }

    public async Task<QuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var cacheKey = $"quote:{sym}";

        if (_cache.TryGetValue(cacheKey, out QuoteResponse? cachedHot) && cachedHot is not null)
        {
            return cachedHot;
        }

        var now = DateTime.UtcNow;
        var dbEntry = await _db.PriceCache.FirstOrDefaultAsync(p => p.Symbol == sym, cancellationToken);
        if (dbEntry is not null && (now - dbEntry.FetchedAt) < FreshnessWindow && !dbEntry.IsStale)
        {
            var resp = MapToResponse(dbEntry);
            _cache.Set(cacheKey, resp, FreshnessWindow);
            return resp;
        }

        try
        {
            var fresh = await _td.GetQuoteAsync(sym, cancellationToken);
            if (fresh is null)
            {
                if (dbEntry is null) return null;
                dbEntry.IsStale = true;
                return MapToResponse(dbEntry);
            }

            if (dbEntry is null)
            {
                dbEntry = new PriceCacheEntry { Symbol = sym };
                _db.PriceCache.Add(dbEntry);
            }
            dbEntry.Price = fresh.Price;
            dbEntry.DayChange = fresh.Change;
            dbEntry.DayChangePct = fresh.PercentChange;
            dbEntry.PreviousClose = fresh.PreviousClose;
            dbEntry.Volume = fresh.Volume;
            dbEntry.FetchedAt = now;
            dbEntry.IsStale = false;

            await _db.SaveChangesAsync(cancellationToken);

            var resp = MapToResponse(dbEntry);
            _cache.Set(cacheKey, resp, FreshnessWindow);
            return resp;
        }
        catch (TwelveDataRateLimitException)
        {
            _logger.LogWarning("Rate-limited fetching {Symbol}; serving cached value as stale", sym);
            if (dbEntry is null) return null;
            dbEntry.IsStale = true;
            await _db.SaveChangesAsync(cancellationToken);
            return MapToResponse(dbEntry);
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Twelve Data error fetching {Symbol}; serving cached value if present", sym);
            if (dbEntry is null) return null;
            dbEntry.IsStale = true;
            return MapToResponse(dbEntry);
        }
    }

    private static QuoteResponse MapToResponse(PriceCacheEntry e) => new(
        Symbol: e.Symbol,
        Price: e.Price,
        DayChange: e.DayChange,
        DayChangePct: e.DayChangePct,
        PreviousClose: e.PreviousClose,
        Volume: e.Volume,
        FetchedAt: e.FetchedAt,
        IsStale: e.IsStale);
}
