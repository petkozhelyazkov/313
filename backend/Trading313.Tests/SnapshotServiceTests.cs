using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Services.Analytics;

namespace Trading313.Tests;

/// <summary>
/// Critical correctness — historic snapshots must be computed by replaying transactions,
/// never by reading the current Positions table. Tests pin this behavior.
/// </summary>
public class SnapshotServiceTests
{
    private static SnapshotService Build(TestDb db) =>
        new(db.Context, NullLogger<SnapshotService>.Instance);

    [Fact]
    public async Task Backfill_NoTransactions_ProducesZeroSnapshots()
    {
        using var db = new TestDb();
        db.SeedUser("u1");
        var svc = Build(db);

        var created = await svc.BackfillAsync("u1");

        created.Should().Be(0);
    }

    [Fact]
    public async Task Backfill_SingleBuy_LeavesHoldingsValuedAtThatDayPrice()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1");

        // Set up historical prices for AAPL covering today only.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.SeedHistoricalPrices("AAPL", today, days: 1, startPrice: 150m, endPrice: 150m);

        // User buys 10 AAPL @ $100 today.
        var buyTime = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10);
        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id,
            Symbol = "AAPL",
            Type = TransactionType.Buy,
            Quantity = 10m,
            PricePerShare = 100m,
            Fees = 0m,
            TotalAmount = 1_000m,
            ExecutedAt = buyTime,
        });
        user.CashBalance = 9_000m; // mirror what the buy would have done
        await db.Context.SaveChangesAsync();

        var svc = Build(db);
        await svc.BackfillAsync(user.Id);

        var snap = db.Context.DailyPortfolioSnapshots.Single();
        snap.CashBalance.Should().Be(9_000m);
        snap.HoldingsValue.Should().Be(1_500m); // 10 shares × $150 close
        snap.TotalValue.Should().Be(10_500m);
        snap.TotalInvestedAtSnapshot.Should().Be(1_000m); // cost basis = qty × avg cost
        snap.UnrealizedPl.Should().Be(500m);
    }

    [Fact]
    public async Task Backfill_AvgCostUpdatedOnSecondBuy()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1");

        var d1 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var d2 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var d3 = DateOnly.FromDateTime(DateTime.UtcNow);
        db.SeedHistoricalPrices("AAPL", d1, days: 3, startPrice: 100m, endPrice: 200m);

        // Buy 10 @ $100 on d1, buy 10 @ $200 on d2 → avg cost should be $150.
        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id, Symbol = "AAPL", Type = TransactionType.Buy,
            Quantity = 10m, PricePerShare = 100m, TotalAmount = 1_000m,
            ExecutedAt = d1.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10),
        });
        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id, Symbol = "AAPL", Type = TransactionType.Buy,
            Quantity = 10m, PricePerShare = 200m, TotalAmount = 2_000m,
            ExecutedAt = d2.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10),
        });
        await db.Context.SaveChangesAsync();

        var svc = Build(db);
        await svc.BackfillAsync(user.Id);

        // 3 snapshots (d1, d2, d3). The d3 snapshot has:
        //   cash = 10_000 - 1_000 - 2_000 = 7_000
        //   holdings @ d3 = 20 shares × $200 close = 4_000
        //   total invested (cost basis) = 20 × $150 = 3_000
        //   unrealized = 4_000 - 3_000 = 1_000
        var snap = db.Context.DailyPortfolioSnapshots.Single(s => s.SnapshotDate == d3);
        snap.CashBalance.Should().Be(7_000m);
        snap.HoldingsValue.Should().Be(4_000m);
        snap.TotalInvestedAtSnapshot.Should().Be(3_000m);
        snap.UnrealizedPl.Should().Be(1_000m);
    }

    [Fact]
    public async Task Backfill_SellPreservesAverageCost_AndCreditsCash()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1");

        var d1 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var d2 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var d3 = DateOnly.FromDateTime(DateTime.UtcNow);
        db.SeedHistoricalPrices("AAPL", d1, days: 3, startPrice: 100m, endPrice: 150m);

        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id, Symbol = "AAPL", Type = TransactionType.Buy,
            Quantity = 10m, PricePerShare = 100m, TotalAmount = 1_000m,
            ExecutedAt = d1.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10),
        });
        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id, Symbol = "AAPL", Type = TransactionType.Sell,
            Quantity = 4m, PricePerShare = 125m, TotalAmount = 500m,
            RealizedPl = 100m, // (125 - 100) × 4
            ExecutedAt = d2.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10),
        });
        await db.Context.SaveChangesAsync();

        var svc = Build(db);
        await svc.BackfillAsync(user.Id);

        // d3 state: 6 shares held at avg $100, cash = 10000 - 1000 + 500 = 9500
        var snap = db.Context.DailyPortfolioSnapshots.Single(s => s.SnapshotDate == d3);
        snap.CashBalance.Should().Be(9_500m);
        snap.TotalInvestedAtSnapshot.Should().Be(600m); // 6 × $100 avg (unchanged on sell)
        snap.HoldingsValue.Should().Be(900m); // 6 × $150 d3 close
        snap.UnrealizedPl.Should().Be(300m);
    }

    [Fact]
    public async Task Backfill_HistoricCostsAreFromOriginalAvg_NotReducedByCurrentlyHeldQty()
    {
        // Pin the trickiest correctness rule: positions held on day D and later
        // sold MUST still show up in D's snapshot. We can't read the current Positions
        // table — we must replay transactions.
        using var db = new TestDb();
        var user = db.SeedUser("u1");

        var d1 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);
        var d2 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        var d3 = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var d4 = DateOnly.FromDateTime(DateTime.UtcNow);
        db.SeedHistoricalPrices("AAPL", d1, days: 4, startPrice: 100m, endPrice: 200m);

        // Buy on d1, sell all on d3. As of d4, no holdings remain.
        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id, Symbol = "AAPL", Type = TransactionType.Buy,
            Quantity = 10m, PricePerShare = 100m, TotalAmount = 1_000m,
            ExecutedAt = d1.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10),
        });
        db.Context.Transactions.Add(new Transaction
        {
            UserId = user.Id, Symbol = "AAPL", Type = TransactionType.Sell,
            Quantity = 10m, PricePerShare = 170m, TotalAmount = 1_700m,
            RealizedPl = 700m,
            ExecutedAt = d3.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(10),
        });
        await db.Context.SaveChangesAsync();

        var svc = Build(db);
        await svc.BackfillAsync(user.Id);

        // d2 snapshot — still holding 10 AAPL, valued at the d2 close.
        // (decimal(18,4) persistence truncates interpolated prices, so allow a tiny epsilon.)
        var d2Close = db.Context.HistoricalPrices.Single(p => p.Date == d2).Close;
        var snapD2 = db.Context.DailyPortfolioSnapshots.Single(s => s.SnapshotDate == d2);
        snapD2.HoldingsValue.Should().BeApproximately(10m * d2Close, precision: 0.01m);
        snapD2.TotalInvestedAtSnapshot.Should().Be(1_000m);

        // d4 snapshot — sold by then, only cash remains.
        var snapD4 = db.Context.DailyPortfolioSnapshots.Single(s => s.SnapshotDate == d4);
        snapD4.HoldingsValue.Should().Be(0m);
        snapD4.CashBalance.Should().Be(10_000m - 1_000m + 1_700m); // 10_700
        snapD4.TotalInvestedAtSnapshot.Should().Be(0m);
    }
}
