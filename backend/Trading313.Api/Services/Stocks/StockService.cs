using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.Stocks;

public class StockService : IStockService
{
    private static readonly TimeSpan MetadataTtl = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly ITwelveDataClient _td;
    private readonly ILogger<StockService> _logger;

    public StockService(AppDbContext db, ITwelveDataClient td, ILogger<StockService> logger)
    {
        _db = db;
        _td = td;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StockSearchResult>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<StockSearchResult>();

        var q = query.Trim();
        var local = await SearchLocalAsync(q, limit, cancellationToken);

        // If we got fewer than 5 local hits OR the top hit's metadata is stale, hit Twelve Data.
        var shouldQueryRemote =
            local.Count < 5 ||
            local.First() is { } top && (top.LastMetadataRefreshAt is null ||
                                         DateTime.UtcNow - top.LastMetadataRefreshAt > MetadataTtl);

        if (!shouldQueryRemote)
        {
            return local.Select(MapStock).Take(limit).ToList();
        }

        try
        {
            var remote = await _td.SearchSymbolsAsync(q, cancellationToken);
            if (remote.Count > 0)
            {
                await UpsertRemoteMatchesAsync(remote, cancellationToken);
                local = await SearchLocalAsync(q, limit, cancellationToken);
            }
        }
        catch (TwelveDataRateLimitException)
        {
            _logger.LogWarning("Symbol search rate-limited; falling back to local results only");
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Symbol search via Twelve Data failed; falling back to local");
        }

        return local.Select(MapStock).Take(limit).ToList();
    }

    public async Task<StockSearchResult?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var s = symbol.Trim().ToUpperInvariant();
        var stock = await _db.Stocks.FirstOrDefaultAsync(x => x.Symbol == s, cancellationToken);
        if (stock is not null) return MapStock(stock);

        try
        {
            var remote = await _td.SearchSymbolsAsync(s, cancellationToken);
            var exact = remote.FirstOrDefault(r => string.Equals(r.Symbol, s, StringComparison.OrdinalIgnoreCase));
            if (exact is null) return null;

            await UpsertRemoteMatchesAsync(new[] { exact }, cancellationToken);
            stock = await _db.Stocks.FirstOrDefaultAsync(x => x.Symbol == s, cancellationToken);
            return stock is null ? null : MapStock(stock);
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Symbol lookup for {Symbol} failed", s);
            return null;
        }
    }

    private async Task<List<Stock>> SearchLocalAsync(string query, int limit, CancellationToken cancellationToken)
    {
        var like = $"%{query}%";
        var prefix = $"{query}%";
        var upper = query.ToUpperInvariant();

        return await _db.Stocks
            .Where(s => s.IsActive && (EF.Functions.Like(s.Symbol, like) || EF.Functions.Like(s.Name, like)))
            // Rank: exact-symbol match → symbol prefix → name prefix → other (substring).
            .OrderBy(s => s.Symbol == upper ? 0
                          : EF.Functions.Like(s.Symbol, prefix) ? 1
                          : EF.Functions.Like(s.Name, prefix) ? 2
                          : 3)
            // Prefer major US exchanges then USD, typical Trading 212 user expectation.
            .ThenByDescending(s => s.Exchange == "NASDAQ" || s.Exchange == "NYSE")
            .ThenByDescending(s => s.Currency == "USD")
            .ThenBy(s => s.Symbol)
            .Take(Math.Max(limit, 5))
            .ToListAsync(cancellationToken);
    }

    private async Task UpsertRemoteMatchesAsync(IEnumerable<Infrastructure.MarketData.Models.TdSymbolMatch> matches, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Twelve Data can return the same symbol on multiple exchanges (e.g., AAPL on
        // NASDAQ + Mexico). Collapse to first occurrence per symbol.
        var dedup = matches
            .GroupBy(m => m.Symbol.ToUpperInvariant())
            .Select(g => (Symbol: g.Key, Match: g.First()))
            .ToList();

        var symbols = dedup.Select(d => d.Symbol).ToHashSet();
        var existing = await _db.Stocks
            .Where(s => symbols.Contains(s.Symbol))
            .ToDictionaryAsync(s => s.Symbol, cancellationToken);

        foreach (var (sym, m) in dedup)
        {
            if (existing.TryGetValue(sym, out var stock))
            {
                stock.Name = m.Name;
                stock.Exchange = m.Exchange;
                stock.Currency = string.IsNullOrEmpty(m.Currency) ? stock.Currency : m.Currency!;
                stock.Country = m.Country;
                stock.InstrumentType = m.InstrumentType;
                stock.LastMetadataRefreshAt = now;
            }
            else
            {
                _db.Stocks.Add(new Stock
                {
                    Symbol = sym,
                    Name = m.Name,
                    Exchange = m.Exchange,
                    Currency = string.IsNullOrEmpty(m.Currency) ? "USD" : m.Currency!,
                    Country = m.Country,
                    InstrumentType = m.InstrumentType,
                    IsActive = true,
                    LastMetadataRefreshAt = now,
                    CreatedAt = now,
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static StockSearchResult MapStock(Stock s) => new(
        Symbol: s.Symbol,
        Name: s.Name,
        Exchange: s.Exchange,
        Currency: s.Currency,
        Country: s.Country,
        InstrumentType: s.InstrumentType,
        LogoUrl: s.LogoUrl);

    /// <summary>
    /// Fetches the company logo URL from Twelve Data on first request and caches
    /// it on the Stock row. No-op if logo already cached.
    /// </summary>
    public async Task EnsureLogoCachedAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == sym, cancellationToken);
        if (stock is null || !string.IsNullOrEmpty(stock.LogoUrl)) return;

        try
        {
            var url = await _td.GetLogoUrlAsync(sym, cancellationToken);
            if (!string.IsNullOrEmpty(url))
            {
                stock.LogoUrl = url;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Logo fetch failed for {Symbol}", sym);
        }
    }
}
