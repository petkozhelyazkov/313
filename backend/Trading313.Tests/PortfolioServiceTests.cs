using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.Portfolio;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Portfolio;
using Trading313.Api.Services.Stocks;

namespace Trading313.Tests;

/// <summary>
/// Verifies the Buy/Sell flows on PortfolioService: averaging, realized P/L,
/// cash accounting, and the two main rejection paths (insufficient cash, oversell).
/// </summary>
public class PortfolioServiceTests
{
    private static PortfolioService Build(TestDb db, decimal currentPrice)
    {
        var quotes = new Mock<IQuoteService>();
        quotes.Setup(q => q.GetQuoteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuoteResponse(
                Symbol: "AAPL",
                Price: currentPrice,
                DayChange: 0m,
                DayChangePct: 0m,
                PreviousClose: currentPrice,
                Volume: 1_000_000,
                FetchedAt: DateTime.UtcNow,
                IsStale: false));

        var stocks = new Mock<IStockService>();
        stocks.Setup(s => s.GetBySymbolAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockSearchResult("AAPL", "Apple Inc.", "NASDAQ", "USD", "United States", "Common Stock", null));

        return new PortfolioService(db.Context, quotes.Object, stocks.Object, NullLogger<PortfolioService>.Instance);
    }

    [Fact]
    public async Task Buy_FirstPurchase_CreatesPositionAndDeductsCash()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 10_000m);
        var svc = Build(db, currentPrice: 100m);

        var result = await svc.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 5m });

        result.Succeeded.Should().BeTrue();
        result.Value!.CashBalance.Should().Be(9_500m); // 10_000 - 5 × 100
        result.Value.Position.Quantity.Should().Be(5m);
        result.Value.Position.AverageCost.Should().Be(100m);

        var pos = await db.Context.Positions.SingleAsync();
        pos.Quantity.Should().Be(5m);
        pos.AverageCost.Should().Be(100m);
    }

    [Fact]
    public async Task Buy_SecondPurchase_RecomputesAverageCost()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 10_000m);

        // First buy at $100.
        var svc1 = Build(db, currentPrice: 100m);
        await svc1.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 10m });

        // Second buy at $200 (same number of shares).
        var svc2 = Build(db, currentPrice: 200m);
        var result = await svc2.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 10m });

        result.Succeeded.Should().BeTrue();
        // Avg = (10×100 + 10×200) / 20 = 150
        result.Value!.Position.AverageCost.Should().Be(150m);
        result.Value.Position.Quantity.Should().Be(20m);
        result.Value.CashBalance.Should().Be(10_000m - 1_000m - 2_000m); // 7_000
    }

    [Fact]
    public async Task Buy_InsufficientCash_Fails()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 500m);
        var svc = Build(db, currentPrice: 100m);

        var result = await svc.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 10m });

        result.Succeeded.Should().BeFalse();
        result.FailureKind.Should().Be(TradeFailureKind.InsufficientCash);

        // Nothing should have been written.
        var hasTx = await db.Context.Transactions.AnyAsync();
        hasTx.Should().BeFalse();
        var stillHasCash = await db.Context.Users.Where(u => u.Id == user.Id).Select(u => u.CashBalance).SingleAsync();
        stillHasCash.Should().Be(500m);
    }

    [Fact]
    public async Task Sell_ComputesRealizedPlFromAverageCost()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 10_000m);

        var buy = Build(db, currentPrice: 100m);
        await buy.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 10m });

        var sell = Build(db, currentPrice: 150m);
        var result = await sell.SellAsync(user.Id, new SellRequest { Symbol = "AAPL", Quantity = 4m });

        result.Succeeded.Should().BeTrue();
        // Realized P/L = (150 - 100) × 4 = 200
        result.Value!.Transaction.RealizedPl.Should().Be(200m);
        result.Value.Position.Quantity.Should().Be(6m);
        result.Value.Position.AverageCost.Should().Be(100m); // unchanged on sell
        // Cash = 10_000 - 1_000 (buy) + 600 (sell at 150 × 4) = 9_600
        result.Value.CashBalance.Should().Be(9_600m);
    }

    [Fact]
    public async Task Sell_OverHeldQuantity_Fails()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 10_000m);

        var buy = Build(db, currentPrice: 100m);
        await buy.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 3m });

        var sell = Build(db, currentPrice: 150m);
        var result = await sell.SellAsync(user.Id, new SellRequest { Symbol = "AAPL", Quantity = 10m });

        result.Succeeded.Should().BeFalse();
        result.FailureKind.Should().Be(TradeFailureKind.InsufficientShares);
    }

    [Fact]
    public async Task Sell_AllShares_MarksPositionClosed()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 10_000m);

        var buy = Build(db, currentPrice: 100m);
        await buy.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 10m });

        var sell = Build(db, currentPrice: 120m);
        var result = await sell.SellAsync(user.Id, new SellRequest { Symbol = "AAPL", Quantity = 10m });

        result.Succeeded.Should().BeTrue();
        result.Value!.Position.Quantity.Should().Be(0m);
        result.Value.Position.IsClosed.Should().BeTrue();
    }

    [Fact]
    public async Task Buy_FractionalShares_PersistsExactQuantity()
    {
        using var db = new TestDb();
        var user = db.SeedUser("u1", cashBalance: 10_000m);
        var svc = Build(db, currentPrice: 100m);

        await svc.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 0.5m });
        await svc.BuyAsync(user.Id, new BuyRequest { Symbol = "AAPL", Quantity = 0.3m });
        var sellSvc = Build(db, currentPrice: 100m);
        var sell = await sellSvc.SellAsync(user.Id, new SellRequest { Symbol = "AAPL", Quantity = 0.8m });

        sell.Succeeded.Should().BeTrue();
        sell.Value!.Position.Quantity.Should().Be(0m);
        sell.Value.Position.IsClosed.Should().BeTrue();
    }
}
