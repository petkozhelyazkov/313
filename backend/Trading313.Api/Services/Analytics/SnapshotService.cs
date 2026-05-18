using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Services.Analytics;

/// <summary>
/// Computes daily portfolio snapshots by replaying transactions chronologically.
///
/// CRITICAL: never use the current Positions table for historic snapshots — positions
/// you held on day D and later sold must still appear in D's snapshot. Always recompute
/// from Transactions.
/// </summary>
public class SnapshotService : ISnapshotService
{
    private const decimal StartingCashBalance = 10_000m;

    private readonly AppDbContext _db;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(AppDbContext db, ILogger<SnapshotService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SnapshotComputed> ComputeAndPersistAsync(string userId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var endOfDay = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.ExecutedAt <= endOfDay)
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);

        var replay = ReplayTransactions(transactions);

        // Look up close price on `date` for every symbol with quantity > 0.
        var symbols = replay.Positions
            .Where(p => p.Value.Quantity > 0)
            .Select(p => p.Key)
            .ToList();

        var prices = symbols.Count == 0
            ? new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            : await LookupClosesAsync(symbols, date, cancellationToken);

        decimal holdingsValue = 0m;
        decimal costBasis = 0m;
        foreach (var (symbol, state) in replay.Positions)
        {
            if (state.Quantity <= 0) continue;
            costBasis += state.Quantity * state.AverageCost;
            if (prices.TryGetValue(symbol, out var close))
            {
                holdingsValue += state.Quantity * close;
            }
            else
            {
                // No close found on or before date — fall back to cost basis so totalValue stays sane.
                holdingsValue += state.Quantity * state.AverageCost;
            }
        }

        var unrealized = holdingsValue - costBasis;
        var totalValue = replay.CashBalance + holdingsValue;

        var existing = await _db.DailyPortfolioSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SnapshotDate == date, cancellationToken);

        if (existing is null)
        {
            existing = new DailyPortfolioSnapshot
            {
                UserId = userId,
                SnapshotDate = date,
            };
            _db.DailyPortfolioSnapshots.Add(existing);
        }

        existing.CashBalance = replay.CashBalance;
        existing.HoldingsValue = holdingsValue;
        existing.TotalValue = totalValue;
        existing.TotalInvestedAtSnapshot = costBasis;
        existing.UnrealizedPl = unrealized;

        await _db.SaveChangesAsync(cancellationToken);

        return new SnapshotComputed(date, replay.CashBalance, holdingsValue, totalValue, costBasis, unrealized);
    }

    public async Task<int> BackfillAsync(string userId, CancellationToken cancellationToken = default)
    {
        var firstTx = await _db.Transactions
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.ExecutedAt)
            .Select(t => (DateTime?)t.ExecutedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstTx is null) return 0;

        var start = DateOnly.FromDateTime(firstTx.Value);
        var end = DateOnly.FromDateTime(DateTime.UtcNow);

        // Pre-load all transactions once.
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && t.ExecutedAt <= end.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc))
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);

        // All symbols ever held.
        var allSymbols = transactions.Select(t => t.Symbol).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var pricesBySymbolDate = await LookupAllClosesAsync(allSymbols, start, end, cancellationToken);

        // Walk day by day applying transactions; snapshot at end of each day.
        var positions = new Dictionary<string, PositionState>(StringComparer.OrdinalIgnoreCase);
        decimal cash = StartingCashBalance;
        int txIndex = 0;

        var existing = await _db.DailyPortfolioSnapshots
            .Where(s => s.UserId == userId)
            .ToDictionaryAsync(s => s.SnapshotDate, cancellationToken);

        int created = 0;
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var endOfDay = day.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            while (txIndex < transactions.Count && transactions[txIndex].ExecutedAt <= endOfDay)
            {
                ApplyTransaction(transactions[txIndex], positions, ref cash);
                txIndex++;
            }

            decimal holdings = 0m;
            decimal cost = 0m;
            foreach (var (symbol, state) in positions)
            {
                if (state.Quantity <= 0) continue;
                cost += state.Quantity * state.AverageCost;
                var close = LookupClose(pricesBySymbolDate, symbol, day);
                holdings += state.Quantity * (close ?? state.AverageCost);
            }

            var totalValue = cash + holdings;
            var unrealized = holdings - cost;

            if (!existing.TryGetValue(day, out var snap))
            {
                snap = new DailyPortfolioSnapshot
                {
                    UserId = userId,
                    SnapshotDate = day,
                };
                _db.DailyPortfolioSnapshots.Add(snap);
                created++;
            }
            snap.CashBalance = cash;
            snap.HoldingsValue = holdings;
            snap.TotalValue = totalValue;
            snap.TotalInvestedAtSnapshot = cost;
            snap.UnrealizedPl = unrealized;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Backfilled {Created} snapshots for user {UserId}", created, userId);
        return created;
    }

    public async Task<int> RunDailyForAllUsersAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var userIds = await _db.Set<ApplicationUser>()
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        int processed = 0;
        foreach (var userId in userIds)
        {
            try
            {
                await ComputeAndPersistAsync(userId, date, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Snapshot failed for user {UserId} on {Date}", userId, date);
            }
        }
        return processed;
    }

    // ─── Transaction replay ──────────────────────────────────────────────────

    private record PositionState
    {
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
    }

    private record ReplayResult(Dictionary<string, PositionState> Positions, decimal CashBalance);

    private static ReplayResult ReplayTransactions(IList<Transaction> transactions)
    {
        var positions = new Dictionary<string, PositionState>(StringComparer.OrdinalIgnoreCase);
        decimal cash = StartingCashBalance;
        foreach (var t in transactions)
        {
            ApplyTransaction(t, positions, ref cash);
        }
        return new ReplayResult(positions, cash);
    }

    private static void ApplyTransaction(Transaction t, Dictionary<string, PositionState> positions, ref decimal cash)
    {
        var sym = t.Symbol.ToUpperInvariant();
        if (!positions.TryGetValue(sym, out var state))
        {
            state = new PositionState();
            positions[sym] = state;
        }

        if (t.Type == TransactionType.Buy)
        {
            var newQty = state.Quantity + t.Quantity;
            var newAvg = newQty == 0
                ? 0m
                : ((state.Quantity * state.AverageCost) + (t.Quantity * t.PricePerShare) + t.Fees) / newQty;
            state.Quantity = newQty;
            state.AverageCost = newAvg;
            cash -= t.TotalAmount;
        }
        else // Sell
        {
            state.Quantity -= t.Quantity;
            // AverageCost unchanged on sell (standard simplification).
            cash += t.TotalAmount;
        }
    }

    // ─── Price lookups (with weekend / holiday fallback) ─────────────────────

    private async Task<Dictionary<string, decimal>> LookupClosesAsync(IEnumerable<string> symbols, DateOnly date, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var symbol in symbols)
        {
            var close = await _db.HistoricalPrices
                .Where(h => h.Symbol == symbol && h.Date <= date)
                .OrderByDescending(h => h.Date)
                .Select(h => (decimal?)h.Close)
                .FirstOrDefaultAsync(cancellationToken);
            if (close is not null)
            {
                result[symbol] = close.Value;
            }
        }
        return result;
    }

    private async Task<Dictionary<string, SortedDictionary<DateOnly, decimal>>> LookupAllClosesAsync(IList<string> symbols, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, SortedDictionary<DateOnly, decimal>>(StringComparer.OrdinalIgnoreCase);
        if (symbols.Count == 0) return result;

        // Also include data before `start` so the first day's fallback works.
        var earliest = start.AddDays(-30);
        var prices = await _db.HistoricalPrices
            .Where(h => symbols.Contains(h.Symbol) && h.Date >= earliest && h.Date <= end)
            .Select(h => new { h.Symbol, h.Date, h.Close })
            .ToListAsync(cancellationToken);

        foreach (var p in prices)
        {
            var sym = p.Symbol.ToUpperInvariant();
            if (!result.TryGetValue(sym, out var byDate))
            {
                byDate = new SortedDictionary<DateOnly, decimal>();
                result[sym] = byDate;
            }
            byDate[p.Date] = p.Close;
        }
        return result;
    }

    private static decimal? LookupClose(IReadOnlyDictionary<string, SortedDictionary<DateOnly, decimal>> prices, string symbol, DateOnly date)
    {
        if (!prices.TryGetValue(symbol, out var byDate)) return null;
        if (byDate.TryGetValue(date, out var exact)) return exact;

        decimal? mostRecentBefore = null;
        foreach (var (d, close) in byDate)
        {
            if (d > date) break;
            mostRecentBefore = close;
        }
        return mostRecentBefore;
    }
}
