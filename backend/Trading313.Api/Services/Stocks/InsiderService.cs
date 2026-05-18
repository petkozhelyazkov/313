using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Services.Stocks;

public interface IInsiderService
{
    Task<InsiderSummaryResponse> GetAsync(string symbol, CancellationToken cancellationToken = default);
}

/// <summary>
/// Returns recent insider transactions per symbol. Cached 24h; falls back to
/// seeded demo data for popular symbols.
/// </summary>
public class InsiderService : IInsiderService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    // Hand-curated recent insider activity per popular symbol.
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<DemoTrade>> DemoSeed =
        new Dictionary<string, IReadOnlyList<DemoTrade>>(StringComparer.OrdinalIgnoreCase)
        {
            ["AAPL"] = new[]
            {
                new DemoTrade("Tim Cook", "CEO", -12, "Sell", 50_000m, 240.15m),
                new DemoTrade("Luca Maestri", "CFO", -25, "Sell", 18_000m, 235.80m),
                new DemoTrade("Katherine Adams", "General Counsel", -40, "Sell", 8_500m, 232.40m),
                new DemoTrade("Jeffrey Williams", "COO", -60, "Sell", 12_000m, 228.10m),
            },
            ["MSFT"] = new[]
            {
                new DemoTrade("Satya Nadella", "CEO", -8, "Sell", 24_000m, 510.25m),
                new DemoTrade("Amy Hood", "CFO", -22, "Sell", 9_000m, 505.40m),
                new DemoTrade("Bradford Smith", "President", -45, "Sell", 6_500m, 498.10m),
            },
            ["NVDA"] = new[]
            {
                new DemoTrade("Jensen Huang", "CEO", -5, "Sell", 50_000m, 215.60m),
                new DemoTrade("Colette Kress", "CFO", -18, "Sell", 12_000m, 210.30m),
                new DemoTrade("Mark Stevens", "Director", 70, "Buy", 5_000m, 195.20m),
                new DemoTrade("Tench Coxe", "Director", -90, "Sell", 25_000m, 188.40m),
            },
            ["TSLA"] = new[]
            {
                new DemoTrade("Elon Musk", "CEO", -3, "Sell", 100_000m, 290.50m),
                new DemoTrade("Vaibhav Taneja", "CFO", -28, "Sell", 7_500m, 275.80m),
                new DemoTrade("Robyn Denholm", "Chair", -55, "Sell", 4_000m, 268.40m),
            },
            ["META"] = new[]
            {
                new DemoTrade("Mark Zuckerberg", "CEO", -10, "Sell", 70_000m, 610.40m),
                new DemoTrade("Susan Li", "CFO", -25, "Sell", 8_500m, 595.30m),
            },
            ["AMZN"] = new[]
            {
                new DemoTrade("Andy Jassy", "CEO", -14, "Sell", 25_000m, 260.15m),
                new DemoTrade("Brian Olsavsky", "CFO", -32, "Sell", 9_500m, 252.80m),
                new DemoTrade("Jeffrey Bezos", "Founder", -50, "Sell", 1_000_000m, 245.40m),
            },
            ["GOOGL"] = new[]
            {
                new DemoTrade("Sundar Pichai", "CEO", -12, "Sell", 20_000m, 215.40m),
                new DemoTrade("Ruth Porat", "CFO", -28, "Sell", 11_000m, 210.80m),
            },
        };

    private readonly AppDbContext _db;
    private readonly ILogger<InsiderService> _logger;

    public InsiderService(AppDbContext db, ILogger<InsiderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<InsiderSummaryResponse> GetAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var freshThreshold = DateTime.UtcNow - Ttl;
        var stale = !await _db.InsiderTrades
            .AnyAsync(t => t.Symbol == sym && t.FetchedAt > freshThreshold, cancellationToken);

        if (stale)
        {
            await SeedAsync(sym, cancellationToken);
        }

        var trades = await _db.InsiderTrades
            .Where(t => t.Symbol == sym)
            .OrderByDescending(t => t.TransactionDate)
            .Take(50)
            .Select(t => new InsiderTradeDto(t.Id, t.Symbol, t.PersonName, t.Role, t.TransactionDate, t.TransactionType, t.Shares, t.PricePerShare, t.Value))
            .ToListAsync(cancellationToken);

        var since = DateTime.UtcNow.AddDays(-90);
        var recent = trades.Where(t => t.TransactionDate >= since).ToList();
        var buys = recent.Where(t => t.TransactionType == "Buy").ToList();
        var sells = recent.Where(t => t.TransactionType == "Sell").ToList();

        return new InsiderSummaryResponse(
            Symbol: sym,
            Last90DaysBuyCount: buys.Count,
            Last90DaysSellCount: sells.Count,
            Last90DaysBuyValue: buys.Sum(t => t.Value ?? 0m),
            Last90DaysSellValue: sells.Sum(t => t.Value ?? 0m),
            RecentTrades: trades);
    }

    private async Task SeedAsync(string symbol, CancellationToken cancellationToken)
    {
        if (!DemoSeed.TryGetValue(symbol, out var demos)) return;

        // Clear stale rows to avoid duplicates if the seed changed.
        var stale = await _db.InsiderTrades.Where(t => t.Symbol == symbol).ToListAsync(cancellationToken);
        _db.InsiderTrades.RemoveRange(stale);

        var now = DateTime.UtcNow;
        foreach (var d in demos)
        {
            _db.InsiderTrades.Add(new InsiderTrade
            {
                Symbol = symbol,
                PersonName = d.Person,
                Role = d.Role,
                TransactionDate = now.AddDays(d.DaysAgo).Date,
                TransactionType = d.Type,
                Shares = d.Shares,
                PricePerShare = d.Price,
                Value = d.Shares * d.Price,
                FetchedAt = now,
            });
        }
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist insider trades for {Symbol}", symbol);
        }
    }

    private record DemoTrade(string Person, string Role, int DaysAgo, string Type, decimal Shares, decimal Price);
}
