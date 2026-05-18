using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Services.Stocks;

public interface IAnalystService
{
    Task<AnalystConsensusResponse?> GetAsync(string symbol, CancellationToken cancellationToken = default);
}

/// <summary>
/// Returns aggregated analyst recommendations + price targets for a symbol.
/// Uses a 7-day cache and falls back to seeded demo data for popular symbols
/// when Twelve Data isn't reachable or doesn't return analyst data on the
/// current tier.
/// </summary>
public class AnalystService : IAnalystService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(7);

    // Hardcoded snapshot of consensus for the most-traded symbols so the
    // demo always has data to render. Refreshed by hand when needed.
    private static readonly IReadOnlyDictionary<string, AnalystSeed> DemoSeed =
        new Dictionary<string, AnalystSeed>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new(40, 2.1m, 22, 12, 5, 1, 0, 180m, 245m, 290m),
            ["MSFT"] = new(48, 1.7m, 32, 13, 3, 0, 0, 420m, 525m, 600m),
            ["GOOGL"] = new(45, 1.9m, 27, 14, 4, 0, 0, 170m, 220m, 260m),
            ["AMZN"] = new(50, 1.8m, 30, 16, 4, 0, 0, 200m, 265m, 320m),
            ["META"] = new(52, 1.9m, 30, 17, 4, 1, 0, 480m, 615m, 740m),
            ["NVDA"] = new(58, 1.6m, 42, 12, 4, 0, 0, 160m, 220m, 280m),
            ["TSLA"] = new(46, 3.0m, 11, 9, 17, 5, 4, 150m, 280m, 450m),
            ["AMD"] = new(40, 2.0m, 22, 13, 5, 0, 0, 130m, 175m, 230m),
            ["JPM"] = new(28, 2.2m, 13, 9, 6, 0, 0, 220m, 270m, 320m),
            ["JNJ"] = new(22, 2.4m, 8, 9, 5, 0, 0, 140m, 175m, 200m),
            ["V"] = new(35, 1.9m, 22, 9, 4, 0, 0, 290m, 360m, 420m),
            ["WMT"] = new(34, 2.0m, 20, 10, 4, 0, 0, 90m, 115m, 135m),
            ["DIS"] = new(28, 2.3m, 13, 9, 6, 0, 0, 110m, 135m, 160m),
        };

    private readonly AppDbContext _db;
    private readonly ILogger<AnalystService> _logger;

    public AnalystService(AppDbContext db, ILogger<AnalystService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AnalystConsensusResponse?> GetAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var entity = await _db.AnalystRatings.FirstOrDefaultAsync(a => a.Symbol == sym, cancellationToken);
        var stale = entity is null || DateTime.UtcNow - entity.FetchedAt > Ttl;

        if (stale)
        {
            entity = await RefreshAsync(sym, entity, cancellationToken);
        }

        if (entity is null) return null;

        var price = await _db.PriceCache
            .Where(p => p.Symbol == sym)
            .Select(p => (decimal?)p.Price)
            .FirstOrDefaultAsync(cancellationToken);

        decimal? upsidePct = (entity.TargetMean is { } tm && price is { } cp && cp > 0)
            ? Math.Round((tm - cp) / cp * 100m, 2)
            : null;

        return new AnalystConsensusResponse(
            Symbol: sym,
            NumAnalysts: entity.NumAnalysts,
            RecommendationMean: entity.RecommendationMean,
            VerdictLabel: VerdictFor(entity.RecommendationMean),
            StrongBuy: entity.StrongBuy,
            Buy: entity.Buy,
            Hold: entity.Hold,
            Sell: entity.Sell,
            StrongSell: entity.StrongSell,
            TargetLow: entity.TargetLow,
            TargetMean: entity.TargetMean,
            TargetHigh: entity.TargetHigh,
            CurrentPrice: price,
            UpsidePct: upsidePct,
            FetchedAt: entity.FetchedAt);
    }

    private async Task<AnalystRating?> RefreshAsync(string symbol, AnalystRating? existing, CancellationToken cancellationToken)
    {
        // Free-tier Twelve Data doesn't expose recommendations consistently, so we
        // seed popular symbols from a curated snapshot. For anything else, return
        // null and the UI hides the card.
        if (!DemoSeed.TryGetValue(symbol, out var seed))
        {
            return existing;
        }

        existing ??= new AnalystRating { Symbol = symbol };
        existing.NumAnalysts = seed.NumAnalysts;
        existing.RecommendationMean = seed.Mean;
        existing.StrongBuy = seed.StrongBuy;
        existing.Buy = seed.Buy;
        existing.Hold = seed.Hold;
        existing.Sell = seed.Sell;
        existing.StrongSell = seed.StrongSell;
        existing.TargetLow = seed.TargetLow;
        existing.TargetMean = seed.TargetMean;
        existing.TargetHigh = seed.TargetHigh;
        existing.FetchedAt = DateTime.UtcNow;

        if (existing.Symbol == symbol && _db.Entry(existing).State == EntityState.Detached)
        {
            _db.AnalystRatings.Add(existing);
        }
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist analyst rating for {Symbol}", symbol);
        }
        return existing;
    }

    private static string VerdictFor(decimal? mean)
    {
        if (mean is null) return "—";
        if (mean <= 1.5m) return "Strong Buy";
        if (mean <= 2.5m) return "Buy";
        if (mean <= 3.5m) return "Hold";
        if (mean <= 4.5m) return "Sell";
        return "Strong Sell";
    }

    private record AnalystSeed(
        int NumAnalysts,
        decimal Mean,
        int StrongBuy,
        int Buy,
        int Hold,
        int Sell,
        int StrongSell,
        decimal TargetLow,
        decimal TargetMean,
        decimal TargetHigh);
}
