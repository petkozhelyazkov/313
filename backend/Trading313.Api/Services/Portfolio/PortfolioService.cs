using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.Portfolio;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Stocks;

namespace Trading313.Api.Services.Portfolio;

public class PortfolioService : IPortfolioService
{
    private const decimal DefaultFees = 0m;

    private readonly AppDbContext _db;
    private readonly IQuoteService _quotes;
    private readonly IStockService _stocks;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(AppDbContext db, IQuoteService quotes, IStockService stocks, ILogger<PortfolioService> logger)
    {
        _db = db;
        _quotes = quotes;
        _stocks = stocks;
        _logger = logger;
    }

    public async Task<TradeOutcome> BuyAsync(string userId, BuyRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            return TradeOutcome.Fail(TradeFailureKind.InvalidQuantity, "Quantity must be greater than 0.");

        var sym = request.Symbol.Trim().ToUpperInvariant();

        // 1. Make sure the symbol exists in our catalog (lazily upserts from Twelve Data).
        var stockMeta = await _stocks.GetBySymbolAsync(sym, cancellationToken);
        if (stockMeta is null)
            return TradeOutcome.Fail(TradeFailureKind.SymbolNotResolved, $"Unknown symbol '{sym}'.");

        // 2. Fetch current price server-side. Never trust the client.
        var quote = await _quotes.GetQuoteAsync(sym, cancellationToken);
        if (quote is null || quote.Price <= 0)
            return TradeOutcome.Fail(TradeFailureKind.PriceUnavailable, $"Could not fetch a current price for '{sym}'.");

        var price = quote.Price;
        var totalCost = request.Quantity * price + DefaultFees;

        await using var dbTx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var user = await _db.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return TradeOutcome.Fail(TradeFailureKind.UserNotFound, "User not found.");

        if (user.CashBalance < totalCost)
        {
            return TradeOutcome.Fail(TradeFailureKind.InsufficientCash,
                $"Insufficient cash. Need {totalCost:F2}, have {user.CashBalance:F2}.");
        }

        var now = DateTime.UtcNow;

        var txn = new Transaction
        {
            UserId = userId,
            Symbol = sym,
            Type = TransactionType.Buy,
            Quantity = request.Quantity,
            PricePerShare = price,
            Fees = DefaultFees,
            TotalAmount = totalCost,
            ExecutedAt = now,
            Notes = request.Notes,
        };
        _db.Transactions.Add(txn);

        var position = await _db.Positions.FirstOrDefaultAsync(
            p => p.UserId == userId && p.Symbol == sym, cancellationToken);

        if (position is null)
        {
            position = new Position
            {
                UserId = userId,
                Symbol = sym,
                Quantity = request.Quantity,
                AverageCost = price,
                TotalInvested = totalCost,
                RealizedPlLifetime = 0m,
                FirstPurchasedAt = now,
                LastTransactionAt = now,
                IsClosed = false,
            };
            _db.Positions.Add(position);
        }
        else
        {
            // Average-cost formula: new_avg = (old_qty * old_avg + buy_qty * buy_price + fees) / new_qty
            var newQty = position.Quantity + request.Quantity;
            var newAvg = ((position.Quantity * position.AverageCost) + (request.Quantity * price) + DefaultFees) / newQty;

            position.Quantity = newQty;
            position.AverageCost = newAvg;
            position.TotalInvested += totalCost;
            position.LastTransactionAt = now;
            position.IsClosed = false;
        }

        user.CashBalance -= totalCost;

        await _db.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);

        _logger.LogInformation("BUY {Symbol} qty={Qty} price={Price} totalCost={Total} cashAfter={Cash} userId={UserId}",
            sym, request.Quantity, price, totalCost, user.CashBalance, userId);

        var response = new TradeResponse(
            Transaction: MapTxn(txn),
            Position: MapPosition(position, quote.Price),
            CashBalance: user.CashBalance);
        return TradeOutcome.Ok(response);
    }

    public async Task<TradeOutcome> SellAsync(string userId, SellRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            return TradeOutcome.Fail(TradeFailureKind.InvalidQuantity, "Quantity must be greater than 0.");

        var sym = request.Symbol.Trim().ToUpperInvariant();

        var quote = await _quotes.GetQuoteAsync(sym, cancellationToken);
        if (quote is null || quote.Price <= 0)
            return TradeOutcome.Fail(TradeFailureKind.PriceUnavailable, $"Could not fetch a current price for '{sym}'.");

        var price = quote.Price;

        await using var dbTx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var user = await _db.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return TradeOutcome.Fail(TradeFailureKind.UserNotFound, "User not found.");

        var position = await _db.Positions.FirstOrDefaultAsync(
            p => p.UserId == userId && p.Symbol == sym && !p.IsClosed, cancellationToken);

        if (position is null || position.Quantity < request.Quantity)
        {
            var held = position?.Quantity ?? 0m;
            return TradeOutcome.Fail(TradeFailureKind.InsufficientShares,
                $"Cannot sell {request.Quantity} of {sym} — held {held}.");
        }

        var now = DateTime.UtcNow;
        var grossProceeds = request.Quantity * price;
        var netProceeds = grossProceeds - DefaultFees;
        var realizedPl = (price - position.AverageCost) * request.Quantity - DefaultFees;

        var txn = new Transaction
        {
            UserId = userId,
            Symbol = sym,
            Type = TransactionType.Sell,
            Quantity = request.Quantity,
            PricePerShare = price,
            Fees = DefaultFees,
            TotalAmount = netProceeds,
            ExecutedAt = now,
            RealizedPl = realizedPl,
            Notes = request.Notes,
        };
        _db.Transactions.Add(txn);

        position.Quantity -= request.Quantity;
        position.RealizedPlLifetime += realizedPl;
        position.LastTransactionAt = now;
        // Keep AverageCost unchanged for remaining shares (standard simplification).
        // Don't reduce TotalInvested — TotalInvested is the lifetime amount invested.
        if (position.Quantity == 0)
        {
            position.IsClosed = true;
        }

        user.CashBalance += netProceeds;

        await _db.SaveChangesAsync(cancellationToken);
        await dbTx.CommitAsync(cancellationToken);

        _logger.LogInformation("SELL {Symbol} qty={Qty} price={Price} realizedPl={Pl} cashAfter={Cash} userId={UserId}",
            sym, request.Quantity, price, realizedPl, user.CashBalance, userId);

        var response = new TradeResponse(
            Transaction: MapTxn(txn),
            Position: MapPosition(position, quote.Price),
            CashBalance: user.CashBalance);
        return TradeOutcome.Ok(response);
    }

    private static TransactionDto MapTxn(Transaction t) => new(
        Id: t.Id,
        Symbol: t.Symbol,
        Type: t.Type.ToString(),
        Quantity: t.Quantity,
        PricePerShare: t.PricePerShare,
        Fees: t.Fees,
        TotalAmount: t.TotalAmount,
        ExecutedAt: t.ExecutedAt,
        RealizedPl: t.RealizedPl,
        Notes: t.Notes,
        Tags: t.Tags);

    internal static PositionDto MapPosition(Position p, decimal? currentPrice, decimal? portfolioHoldingsValue = null, string? logoUrl = null, string? name = null)
    {
        decimal? currentValue = currentPrice is null ? null : p.Quantity * currentPrice;
        decimal? unrealized = currentPrice is null ? null : (currentPrice.Value - p.AverageCost) * p.Quantity;
        decimal? unrealizedPct = (currentPrice is null || p.AverageCost == 0)
            ? null
            : ((currentPrice.Value - p.AverageCost) / p.AverageCost) * 100m;
        decimal? weight = (portfolioHoldingsValue is null || portfolioHoldingsValue == 0 || currentValue is null)
            ? null
            : (currentValue / portfolioHoldingsValue) * 100m;

        return new PositionDto(
            Symbol: p.Symbol,
            Quantity: p.Quantity,
            AverageCost: p.AverageCost,
            TotalInvested: p.TotalInvested,
            RealizedPlLifetime: p.RealizedPlLifetime,
            CurrentPrice: currentPrice,
            CurrentValue: currentValue,
            UnrealizedPl: unrealized,
            UnrealizedPlPct: unrealizedPct,
            Weight: weight,
            FirstPurchasedAt: p.FirstPurchasedAt,
            LastTransactionAt: p.LastTransactionAt,
            IsClosed: p.IsClosed,
            LogoUrl: logoUrl,
            Name: name,
            Notes: p.Notes,
            Tags: p.Tags);
    }
}
