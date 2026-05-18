using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.Stocks;

public interface IStockSplitsService
{
    Task<IReadOnlyList<StockSplit>> GetHistoryAsync(string symbol, CancellationToken cancellationToken);
}

public class StockSplitsService : IStockSplitsService
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromDays(7);
    private readonly AppDbContext _db;
    private readonly ITwelveDataClient _td;
    private readonly ILogger<StockSplitsService> _logger;

    public StockSplitsService(AppDbContext db, ITwelveDataClient td, ILogger<StockSplitsService> logger)
    {
        _db = db;
        _td = td;
        _logger = logger;
    }

    public async Task<IReadOnlyList<StockSplit>> GetHistoryAsync(string symbol, CancellationToken cancellationToken)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        await EnsureFreshAsync(sym, cancellationToken);
        return await _db.StockSplits
            .Where(s => s.Symbol == sym)
            .OrderByDescending(s => s.Date)
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureFreshAsync(string symbol, CancellationToken cancellationToken)
    {
        var newest = await _db.StockSplits
            .Where(s => s.Symbol == symbol)
            .OrderByDescending(s => s.FetchedAt)
            .Select(s => (DateTime?)s.FetchedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (newest is not null && DateTime.UtcNow - newest.Value < FreshFor) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            var entries = await _td.GetSplitsAsync(symbol, today.AddYears(-25), today, cancellationToken);
            var existing = await _db.StockSplits
                .Where(s => s.Symbol == symbol)
                .ToDictionaryAsync(s => s.Date, cancellationToken);

            foreach (var e in entries)
            {
                if (existing.TryGetValue(e.Date, out var ex))
                {
                    ex.FromFactor = e.FromFactor;
                    ex.ToFactor = e.ToFactor;
                    ex.FetchedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.StockSplits.Add(new StockSplit
                    {
                        Symbol = symbol,
                        Date = e.Date,
                        FromFactor = e.FromFactor,
                        ToFactor = e.ToFactor,
                        FetchedAt = DateTime.UtcNow,
                    });
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Split refresh failed for {Symbol}", symbol);
        }
    }
}
