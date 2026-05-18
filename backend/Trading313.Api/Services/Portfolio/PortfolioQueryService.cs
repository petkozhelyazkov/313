using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Portfolio;
using Trading313.Api.Services.MarketData;

namespace Trading313.Api.Services.Portfolio;

public class PortfolioQueryService : IPortfolioQueryService
{
    private readonly AppDbContext _db;
    private readonly IQuoteService _quotes;

    public PortfolioQueryService(AppDbContext db, IQuoteService quotes)
    {
        _db = db;
        _quotes = quotes;
    }

    public async Task<PortfolioSummary> GetSummaryAsync(string userId, bool includeClosed, CancellationToken cancellationToken = default)
    {
        var user = await _db.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        var positions = await _db.Positions
            .Where(p => p.UserId == userId && (includeClosed || !p.IsClosed))
            .OrderBy(p => p.Symbol)
            .ToListAsync(cancellationToken);

        var realizedLifetime = await _db.Positions
            .Where(p => p.UserId == userId)
            .SumAsync(p => p.RealizedPlLifetime, cancellationToken);

        // Fetch current prices (cached in PriceCache/IMemoryCache; rare API hit).
        var pricesBySymbol = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in positions.Where(p => !p.IsClosed))
        {
            if (pricesBySymbol.ContainsKey(p.Symbol)) continue;
            var q = await _quotes.GetQuoteAsync(p.Symbol, cancellationToken);
            pricesBySymbol[p.Symbol] = q?.Price;
        }

        decimal holdingsValue = 0m;
        decimal costBasis = 0m;
        foreach (var p in positions.Where(p => !p.IsClosed))
        {
            costBasis += p.Quantity * p.AverageCost;
            if (pricesBySymbol[p.Symbol] is { } price)
            {
                holdingsValue += p.Quantity * price;
            }
        }

        decimal unrealized = holdingsValue - costBasis;
        decimal unrealizedPct = costBasis == 0 ? 0m : (unrealized / costBasis) * 100m;
        decimal totalValue = user.CashBalance + holdingsValue;

        // Pull logo + name for all positions in one query.
        var symbolKeys = positions.Select(p => p.Symbol).Distinct().ToList();
        var stockMeta = await _db.Stocks
            .Where(s => symbolKeys.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.LogoUrl, s.Name })
            .ToDictionaryAsync(x => x.Symbol, cancellationToken);

        var positionDtos = positions
            .Select(p =>
            {
                stockMeta.TryGetValue(p.Symbol, out var meta);
                return PortfolioService.MapPosition(
                    p,
                    currentPrice: pricesBySymbol.TryGetValue(p.Symbol, out var pr) ? pr : null,
                    portfolioHoldingsValue: holdingsValue == 0 ? null : holdingsValue,
                    logoUrl: meta?.LogoUrl,
                    name: meta?.Name);
            })
            .ToList();

        return new PortfolioSummary(
            CashBalance: user.CashBalance,
            HoldingsValue: holdingsValue,
            TotalValue: totalValue,
            TotalInvested: costBasis,
            UnrealizedPl: unrealized,
            UnrealizedPlPct: unrealizedPct,
            RealizedPlLifetime: realizedLifetime,
            Positions: positionDtos);
    }

    public async Task<TransactionListResponse> GetTransactionsAsync(string userId, int page, int pageSize, string? symbolFilter, string? tagFilter = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = _db.Transactions.AsNoTracking().Where(t => t.UserId == userId);
        if (!string.IsNullOrWhiteSpace(symbolFilter))
        {
            var sym = symbolFilter.Trim().ToUpperInvariant();
            query = query.Where(t => t.Symbol == sym);
        }
        if (!string.IsNullOrWhiteSpace(tagFilter))
        {
            var tag = tagFilter.Trim();
            // Match comma-separated tag exactly (delimited by start/end or commas).
            query = query.Where(t => t.Tags != null && (
                t.Tags == tag ||
                EF.Functions.Like(t.Tags, tag + ",%") ||
                EF.Functions.Like(t.Tags, "%, " + tag + ",%") ||
                EF.Functions.Like(t.Tags, "%," + tag + ",%") ||
                EF.Functions.Like(t.Tags, "%, " + tag) ||
                EF.Functions.Like(t.Tags, "%," + tag)
            ));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.ExecutedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id, t.Symbol, t.Type.ToString(), t.Quantity, t.PricePerShare, t.Fees,
                t.TotalAmount, t.ExecutedAt, t.RealizedPl, t.Notes, t.Tags))
            .ToListAsync(cancellationToken);

        return new TransactionListResponse(page, pageSize, total, items);
    }

    public async Task<PositionDto?> GetPositionAsync(string userId, string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var p = await _db.Positions
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Symbol == sym && !x.IsClosed, cancellationToken);
        if (p is null) return null;

        var meta = await _db.Stocks
            .Where(s => s.Symbol == sym)
            .Select(s => new { s.LogoUrl, s.Name })
            .FirstOrDefaultAsync(cancellationToken);

        var quote = await _quotes.GetQuoteAsync(sym, cancellationToken);
        return PortfolioService.MapPosition(p, quote?.Price, logoUrl: meta?.LogoUrl, name: meta?.Name);
    }
}
