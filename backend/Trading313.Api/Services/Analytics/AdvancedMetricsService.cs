using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Analytics;

namespace Trading313.Api.Services.Analytics;

public interface IAdvancedMetricsService
{
    Task<AdvancedMetricsResponse> ComputeAsync(string userId, string range, CancellationToken cancellationToken = default);
}

/// <summary>
/// Computes performance metrics that go beyond Sharpe/volatility:
///   • TWR — Time-Weighted Return, immune to deposit/withdrawal timing
///   • MWR — Money-Weighted Return (IRR), reflects the investor's actual cash decisions
///   • Sortino — Sharpe variant penalizing only downside volatility
///   • Best/worst day, win rate, average daily return
///
/// All values derived from DailyPortfolioSnapshots, with cashflows (deposits/withdrawals)
/// pulled from CashTransactions so TWR isn't distorted by funding activity.
/// </summary>
public class AdvancedMetricsService : IAdvancedMetricsService
{
    private const double RiskFreeRateAnnual = 0.04;
    private const int TradingDaysPerYear = 252;

    private static readonly IReadOnlyDictionary<string, int> RangeToDays = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["1M"] = 31,
        ["3M"] = 93,
        ["6M"] = 186,
        ["1Y"] = 366,
        ["MAX"] = 366 * 10,
    };

    private readonly AppDbContext _db;

    public AdvancedMetricsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdvancedMetricsResponse> ComputeAsync(string userId, string range, CancellationToken cancellationToken = default)
    {
        if (!RangeToDays.TryGetValue(range, out var days))
        {
            range = "1Y";
            days = RangeToDays["1Y"];
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-days);

        var snapshots = await _db.DailyPortfolioSnapshots
            .Where(s => s.UserId == userId && s.SnapshotDate >= from)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => new { s.SnapshotDate, s.TotalValue })
            .ToListAsync(cancellationToken);

        if (snapshots.Count < 2)
        {
            return Empty(range, snapshots.Count);
        }

        var fromDateTime = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateTime = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var cashflows = await _db.CashTransactions
            .Where(c => c.UserId == userId && c.ExecutedAt >= fromDateTime && c.ExecutedAt < toDateTime)
            .Select(c => new { c.ExecutedAt, c.Type, c.Amount })
            .ToListAsync(cancellationToken);

        var netDepositByDay = new Dictionary<DateOnly, decimal>();
        foreach (var c in cashflows)
        {
            var d = DateOnly.FromDateTime(c.ExecutedAt);
            var signed = c.Type == CashTransactionType.Deposit ? c.Amount : -c.Amount;
            netDepositByDay[d] = netDepositByDay.GetValueOrDefault(d) + signed;
        }

        // ── Daily returns, adjusted for cashflows (TWR-style) ────────────────
        // r_i = (V_i - C_i) / V_{i-1} - 1
        // where C_i = net deposit on day i (deposit positive, withdraw negative).
        // Inflows on day i are excluded from the return for that day.
        var returns = new List<double>(snapshots.Count - 1);
        var returnDates = new List<DateOnly>(snapshots.Count - 1);
        for (int i = 1; i < snapshots.Count; i++)
        {
            var prev = (double)snapshots[i - 1].TotalValue;
            if (prev <= 0) continue;
            var cf = (double)netDepositByDay.GetValueOrDefault(snapshots[i].SnapshotDate);
            var adjusted = (double)snapshots[i].TotalValue - cf;
            var r = (adjusted - prev) / prev;
            returns.Add(r);
            returnDates.Add(snapshots[i].SnapshotDate);
        }

        if (returns.Count < 2)
        {
            return Empty(range, snapshots.Count);
        }

        // ── TWR — chain link products ───────────────────────────────────────
        double twrLinked = 1.0;
        foreach (var r in returns) twrLinked *= (1 + r);
        var twr = twrLinked - 1;

        // ── MWR — IRR over cashflows ────────────────────────────────────────
        // Treat each daily net deposit as a cashflow; starting value is an
        // initial outflow, ending value is a terminal inflow.
        var firstValue = (double)snapshots[0].TotalValue;
        var lastValue = (double)snapshots[^1].TotalValue;
        var totalDays = (snapshots[^1].SnapshotDate.ToDateTime(TimeOnly.MinValue) -
                         snapshots[0].SnapshotDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
        if (totalDays <= 0) totalDays = 1;

        double? mwr = null;
        if (firstValue > 0 && lastValue > 0)
        {
            var flows = new List<(double Days, double Amount)>
            {
                (0, -firstValue),
            };
            foreach (var (date, amount) in netDepositByDay.OrderBy(kv => kv.Key))
            {
                if (date < snapshots[0].SnapshotDate || date > snapshots[^1].SnapshotDate) continue;
                var dayOffset = (date.ToDateTime(TimeOnly.MinValue) - snapshots[0].SnapshotDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
                flows.Add((dayOffset, -(double)amount));
            }
            flows.Add((totalDays, lastValue));
            mwr = ComputeIrr(flows, totalDays);
        }

        // ── Sortino — annualized excess return over downside deviation ──────
        var meanReturn = returns.Average();
        var downside = returns.Where(r => r < 0).ToList();
        double? sortino = null;
        if (downside.Count > 0)
        {
            var downsideVariance = downside.Sum(r => r * r) / downside.Count;
            var downsideStdev = Math.Sqrt(downsideVariance);
            if (downsideStdev > 0)
            {
                var annualizedReturn = meanReturn * TradingDaysPerYear;
                var annualizedDownside = downsideStdev * Math.Sqrt(TradingDaysPerYear);
                sortino = (annualizedReturn - RiskFreeRateAnnual) / annualizedDownside;
            }
        }

        // ── Best / worst day, win rate ──────────────────────────────────────
        int bestIdx = 0;
        int worstIdx = 0;
        for (int i = 1; i < returns.Count; i++)
        {
            if (returns[i] > returns[bestIdx]) bestIdx = i;
            if (returns[i] < returns[worstIdx]) worstIdx = i;
        }
        var positive = returns.Count(r => r > 0);
        var negative = returns.Count(r => r < 0);
        var winRate = returns.Count > 0 ? (double)positive / returns.Count * 100.0 : (double?)null;

        return new AdvancedMetricsResponse(
            TimeWeightedReturn: (decimal)twr * 100m,
            MoneyWeightedReturn: mwr is null ? null : (decimal?)((decimal)mwr.Value * 100m),
            SortinoRatio: sortino is null ? null : (decimal?)(decimal)sortino.Value,
            BestDayReturn: (decimal)returns[bestIdx] * 100m,
            BestDayDate: returnDates[bestIdx],
            WorstDayReturn: (decimal)returns[worstIdx] * 100m,
            WorstDayDate: returnDates[worstIdx],
            PositiveDays: positive,
            NegativeDays: negative,
            WinRate: winRate is null ? null : (decimal)winRate.Value,
            AverageDailyReturn: (decimal)meanReturn * 100m,
            DataPoints: snapshots.Count,
            Range: range);
    }

    private static AdvancedMetricsResponse Empty(string range, int dataPoints) =>
        new(null, null, null, null, null, null, null, 0, 0, null, null, dataPoints, range);

    // ── IRR via Newton-Raphson on day-fraction cashflows ────────────────────
    // Returns annualized IRR. Returns null if it fails to converge.
    private static double? ComputeIrr(IReadOnlyList<(double Days, double Amount)> flows, double horizonDays)
    {
        // NPV(r) = sum( amount * (1+r)^(-days/365) ) — solve for r such that NPV == 0.
        double r = 0.1;
        const int maxIter = 100;
        const double tolerance = 1e-7;

        for (int iter = 0; iter < maxIter; iter++)
        {
            double npv = 0;
            double dnpv = 0;
            foreach (var (days, amount) in flows)
            {
                var t = days / 365.0;
                var pow = Math.Pow(1 + r, -t);
                npv += amount * pow;
                dnpv += -t * amount * pow / (1 + r);
            }
            if (Math.Abs(dnpv) < 1e-12) break;
            var next = r - npv / dnpv;
            if (Math.Abs(next - r) < tolerance) return next;
            r = next;
            if (r < -0.999) r = -0.999;
            if (r > 1000) return null;
        }
        return null;
    }
}
