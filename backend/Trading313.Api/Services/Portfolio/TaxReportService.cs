using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.Portfolio;

namespace Trading313.Api.Services.Portfolio;

public interface ITaxReportService
{
    Task<TaxReportResponse> GenerateAsync(string userId, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> AvailableYearsAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds a per-tax-year report: realized gains/losses (short vs long term), dividends
/// received, fees paid, and per-trade detail rows. FIFO-matches sells against earlier
/// buys to determine holding period.
/// </summary>
public class TaxReportService : ITaxReportService
{
    private const int LongTermDays = 365;

    private readonly AppDbContext _db;

    public TaxReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<int>> AvailableYearsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var years = await _db.Transactions
            .Where(t => t.UserId == userId)
            .Select(t => t.ExecutedAt.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(cancellationToken);
        return years;
    }

    public async Task<TaxReportResponse> GenerateAsync(string userId, int year, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddYears(1);

        // Pull all transactions up to end of year. We need pre-year buys to FIFO-match
        // any sells inside the year.
        var allTxns = await _db.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ExecutedAt < end)
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);

        // Per-symbol FIFO buy lots: (executedAt, quantity remaining, pricePerShare).
        var lots = new Dictionary<string, Queue<BuyLot>>(StringComparer.OrdinalIgnoreCase);
        var sellRows = new List<TaxSellRow>();
        decimal shortGains = 0m, shortLosses = 0m, longGains = 0m, longLosses = 0m;
        decimal totalFeesInYear = 0m;

        foreach (var t in allTxns)
        {
            var sym = t.Symbol.ToUpperInvariant();
            if (!lots.ContainsKey(sym)) lots[sym] = new Queue<BuyLot>();

            var inYear = t.ExecutedAt >= start && t.ExecutedAt < end;
            if (inYear) totalFeesInYear += t.Fees;

            if (t.Type == TransactionType.Buy)
            {
                lots[sym].Enqueue(new BuyLot(t.ExecutedAt, t.Quantity, t.PricePerShare));
            }
            else // Sell
            {
                var remaining = t.Quantity;
                var queue = lots[sym];

                while (remaining > 0m && queue.Count > 0)
                {
                    var lot = queue.Peek();
                    var matched = Math.Min(lot.Quantity, remaining);
                    var costBasis = matched * lot.PricePerShare;
                    var proceeds = matched * t.PricePerShare;
                    var gain = proceeds - costBasis;
                    var holdingDays = (t.ExecutedAt - lot.AcquiredAt).TotalDays;
                    var isLong = holdingDays > LongTermDays;

                    if (inYear)
                    {
                        sellRows.Add(new TaxSellRow(
                            Symbol: sym,
                            AcquiredAt: lot.AcquiredAt,
                            SoldAt: t.ExecutedAt,
                            Quantity: matched,
                            CostBasis: costBasis,
                            Proceeds: proceeds,
                            Gain: gain,
                            IsLongTerm: isLong));

                        if (isLong)
                        {
                            if (gain >= 0) longGains += gain;
                            else longLosses += -gain;
                        }
                        else
                        {
                            if (gain >= 0) shortGains += gain;
                            else shortLosses += -gain;
                        }
                    }

                    lot.Quantity -= matched;
                    if (lot.Quantity <= 0) queue.Dequeue();
                    remaining -= matched;
                }
                // If remaining > 0 here, the FIFO ran out — happens with imported / corrupted
                // data. We silently ignore the excess.
            }
        }

        // Dividends: replay dividend events × position size at ex-date.
        var divEvents = await _db.DividendEvents
            .Where(d => d.ExDate >= DateOnly.FromDateTime(start) && d.ExDate < DateOnly.FromDateTime(end))
            .ToListAsync(cancellationToken);

        decimal dividendsReceived = 0m;
        var dividendRows = new List<DividendRow>();
        foreach (var ev in divEvents)
        {
            var sym = ev.Symbol.ToUpperInvariant();
            decimal qtyAtExDate = 0m;
            foreach (var t in allTxns)
            {
                if (!string.Equals(t.Symbol, sym, StringComparison.OrdinalIgnoreCase)) continue;
                if (t.ExecutedAt.Date > ev.ExDate.ToDateTime(TimeOnly.MinValue)) break;
                qtyAtExDate += t.Type == TransactionType.Buy ? t.Quantity : -t.Quantity;
            }
            if (qtyAtExDate <= 0) continue;
            var amount = qtyAtExDate * ev.Amount;
            dividendsReceived += amount;
            dividendRows.Add(new DividendRow(sym, ev.ExDate, ev.Amount, qtyAtExDate, amount));
        }

        var netShort = shortGains - shortLosses;
        var netLong = longGains - longLosses;

        return new TaxReportResponse(
            Year: year,
            ShortTermGains: shortGains,
            ShortTermLosses: shortLosses,
            ShortTermNet: netShort,
            LongTermGains: longGains,
            LongTermLosses: longLosses,
            LongTermNet: netLong,
            DividendsReceived: dividendsReceived,
            FeesPaid: totalFeesInYear,
            NetTotal: netShort + netLong + dividendsReceived - totalFeesInYear,
            SellRows: sellRows,
            DividendRows: dividendRows);
    }

    private class BuyLot
    {
        public DateTime AcquiredAt { get; }
        public decimal Quantity { get; set; }
        public decimal PricePerShare { get; }
        public BuyLot(DateTime acquiredAt, decimal qty, decimal pricePerShare)
        {
            AcquiredAt = acquiredAt;
            Quantity = qty;
            PricePerShare = pricePerShare;
        }
    }
}
