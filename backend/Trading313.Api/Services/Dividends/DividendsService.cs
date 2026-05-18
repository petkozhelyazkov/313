using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Dividends;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.Dividends;

public interface IDividendsService
{
    Task<IReadOnlyList<DividendHistoryItem>> GetHistoryAsync(string symbol, CancellationToken cancellationToken);
    Task<IReadOnlyList<UpcomingDividendItem>> GetUpcomingAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReceivedDividendItem>> GetReceivedAsync(string userId, CancellationToken cancellationToken);
    Task<DividendSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken);
}

public class DividendsService : IDividendsService
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromHours(24);
    private readonly AppDbContext _db;
    private readonly ITwelveDataClient _td;
    private readonly ILogger<DividendsService> _logger;

    public DividendsService(AppDbContext db, ITwelveDataClient td, ILogger<DividendsService> logger)
    {
        _db = db;
        _td = td;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DividendHistoryItem>> GetHistoryAsync(string symbol, CancellationToken cancellationToken)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        await EnsureFreshAsync(sym, cancellationToken);
        var events = await _db.DividendEvents
            .Where(d => d.Symbol == sym)
            .OrderByDescending(d => d.ExDate)
            .ToListAsync(cancellationToken);
        return events
            .Select(e => new DividendHistoryItem(e.Symbol, e.ExDate, e.PaymentDate, e.Amount))
            .ToList();
    }

    public async Task<IReadOnlyList<UpcomingDividendItem>> GetUpcomingAsync(string userId, CancellationToken cancellationToken)
    {
        var positions = await _db.Positions
            .Where(p => p.UserId == userId && !p.IsClosed && p.Quantity > 0)
            .Select(p => new { p.Symbol, p.Quantity })
            .ToListAsync(cancellationToken);
        if (positions.Count == 0) return Array.Empty<UpcomingDividendItem>();

        foreach (var p in positions)
        {
            await EnsureFreshAsync(p.Symbol, cancellationToken);
        }

        var symbols = positions.Select(p => p.Symbol).ToList();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(90);

        var upcoming = await _db.DividendEvents
            .Where(d => symbols.Contains(d.Symbol) && d.ExDate >= today && d.ExDate <= horizon)
            .OrderBy(d => d.ExDate)
            .ToListAsync(cancellationToken);

        var meta = await _db.Stocks
            .Where(s => symbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.Name, s.LogoUrl })
            .ToDictionaryAsync(x => x.Symbol, cancellationToken);

        var posMap = positions.ToDictionary(p => p.Symbol, p => p.Quantity, StringComparer.OrdinalIgnoreCase);

        return upcoming.Select(d =>
        {
            posMap.TryGetValue(d.Symbol, out var qty);
            meta.TryGetValue(d.Symbol, out var m);
            return new UpcomingDividendItem(
                Symbol: d.Symbol,
                Name: m?.Name,
                LogoUrl: m?.LogoUrl,
                ExDate: d.ExDate,
                PaymentDate: d.PaymentDate,
                AmountPerShare: d.Amount,
                CurrentQuantity: qty,
                EstimatedPayment: Math.Round(d.Amount * qty, 4));
        }).ToList();
    }

    public async Task<IReadOnlyList<ReceivedDividendItem>> GetReceivedAsync(string userId, CancellationToken cancellationToken)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);
        if (transactions.Count == 0) return Array.Empty<ReceivedDividendItem>();

        var symbols = transactions.Select(t => t.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var s in symbols)
        {
            await EnsureFreshAsync(s, cancellationToken);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var past = await _db.DividendEvents
            .Where(d => symbols.Contains(d.Symbol) && d.ExDate <= today)
            .OrderByDescending(d => d.ExDate)
            .ToListAsync(cancellationToken);

        var results = new List<ReceivedDividendItem>();
        foreach (var div in past)
        {
            var qty = ComputeQuantityOn(transactions, div.Symbol, div.ExDate);
            if (qty <= 0) continue;
            results.Add(new ReceivedDividendItem(
                Symbol: div.Symbol,
                ExDate: div.ExDate,
                PaymentDate: div.PaymentDate,
                AmountPerShare: div.Amount,
                QuantityHeld: qty,
                TotalReceived: Math.Round(div.Amount * qty, 4)));
        }
        return results;
    }

    public async Task<DividendSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken)
    {
        var received = await GetReceivedAsync(userId, cancellationToken);
        var upcoming = await GetUpcomingAsync(userId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneYearAgo = today.AddYears(-1);
        var inThirtyDays = today.AddDays(30);

        return new DividendSummary(
            LifetimeReceived: received.Sum(r => r.TotalReceived),
            Upcoming30Days: upcoming.Where(u => u.ExDate <= inThirtyDays).Sum(u => u.EstimatedPayment),
            Last12Months: received.Where(r => r.ExDate >= oneYearAgo).Sum(r => r.TotalReceived),
            UniqueSymbols: received.Select(r => r.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    private async Task EnsureFreshAsync(string symbol, CancellationToken cancellationToken)
    {
        var sym = symbol.Trim().ToUpperInvariant();
        var newest = await _db.DividendEvents
            .Where(d => d.Symbol == sym)
            .OrderByDescending(d => d.FetchedAt)
            .Select(d => (DateTime?)d.FetchedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (newest is not null && DateTime.UtcNow - newest.Value < FreshFor) return;

        // Pull a broad window: 5 years back, 1 year forward.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            var entries = await _td.GetDividendsAsync(sym, today.AddYears(-5), today.AddYears(1), cancellationToken);
            var existing = await _db.DividendEvents
                .Where(d => d.Symbol == sym)
                .ToDictionaryAsync(d => d.ExDate, cancellationToken);

            foreach (var e in entries)
            {
                if (existing.TryGetValue(e.ExDate, out var ex))
                {
                    ex.Amount = e.Amount;
                    ex.PaymentDate = e.PaymentDate;
                    ex.FetchedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.DividendEvents.Add(new DividendEvent
                    {
                        Symbol = sym,
                        ExDate = e.ExDate,
                        PaymentDate = e.PaymentDate,
                        Amount = e.Amount,
                        FetchedAt = DateTime.UtcNow,
                    });
                }
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dividend refresh failed for {Symbol}", sym);
        }
    }

    private static decimal ComputeQuantityOn(IEnumerable<Transaction> ordered, string symbol, DateOnly date)
    {
        decimal qty = 0m;
        foreach (var t in ordered)
        {
            if (!string.Equals(t.Symbol, symbol, StringComparison.OrdinalIgnoreCase)) continue;
            if (DateOnly.FromDateTime(t.ExecutedAt) >= date) break;
            qty += t.Type == Domain.Enums.TransactionType.Buy ? t.Quantity : -t.Quantity;
        }
        return qty;
    }
}
