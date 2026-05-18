using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.Orders;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Stocks;

namespace Trading313.Api.Services.Orders;

public class OrdersService : IOrdersService
{
    private readonly AppDbContext _db;
    private readonly IStockService _stocks;
    private readonly IQuoteService _quotes;

    public OrdersService(AppDbContext db, IStockService stocks, IQuoteService quotes)
    {
        _db = db;
        _stocks = stocks;
        _quotes = quotes;
    }

    public async Task<OrderOutcome> PlaceAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            return OrderOutcome.Fail(OrderFailureKind.InvalidQuantity, "Quantity must be greater than 0.");

        var isTrailing = request.Side == OrderSide.TrailingStop;
        if (!isTrailing && request.LimitPrice <= 0)
            return OrderOutcome.Fail(OrderFailureKind.InvalidPrice, "Limit price must be greater than 0.");
        if (isTrailing && (request.TrailingStopPercent is null or <= 0 or >= 100))
            return OrderOutcome.Fail(OrderFailureKind.InvalidPrice, "Trailing stop % must be between 0 and 100.");

        var sym = request.Symbol.Trim().ToUpperInvariant();
        var stock = await _stocks.GetBySymbolAsync(sym, cancellationToken);
        if (stock is null)
            return OrderOutcome.Fail(OrderFailureKind.SymbolNotResolved, $"Unknown symbol '{sym}'.");

        decimal? highWater = null;
        if (isTrailing)
        {
            var q = await _quotes.GetQuoteAsync(sym, cancellationToken);
            highWater = q?.Price;
            if (highWater is null or <= 0)
                return OrderOutcome.Fail(OrderFailureKind.InvalidPrice, "Current price unavailable; cannot initialize trailing stop.");
        }

        var order = new PendingOrder
        {
            UserId = userId,
            Symbol = sym,
            Side = request.Side,
            LimitPrice = isTrailing ? (highWater!.Value * (1m - request.TrailingStopPercent!.Value / 100m)) : request.LimitPrice,
            Quantity = request.Quantity,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Notes = request.Notes,
            TrailingStopPercent = isTrailing ? request.TrailingStopPercent : null,
            HighWaterMark = highWater,
        };
        _db.PendingOrders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        return OrderOutcome.Ok(await MapAsync(order, cancellationToken));
    }

    public async Task<OrderOutcome> CancelAsync(string userId, long orderId, CancellationToken cancellationToken = default)
    {
        var order = await _db.PendingOrders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);
        if (order is null)
            return OrderOutcome.Fail(OrderFailureKind.NotFound, "Order not found.");
        if (order.Status != OrderStatus.Pending)
            return OrderOutcome.Fail(OrderFailureKind.NotInPendingState, $"Order is {order.Status}, cannot be cancelled.");

        order.Status = OrderStatus.Cancelled;
        order.FilledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return OrderOutcome.Ok(await MapAsync(order, cancellationToken));
    }

    public async Task<OrderListResponse> ListAsync(string userId, CancellationToken cancellationToken = default)
    {
        var orders = await _db.PendingOrders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var symbols = orders.Select(o => o.Symbol).Distinct().ToList();
        var meta = await _db.Stocks
            .Where(s => symbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.LogoUrl, s.Name })
            .ToDictionaryAsync(x => x.Symbol, cancellationToken);

        var prices = new Dictionary<string, decimal?>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in symbols)
        {
            var q = await _quotes.GetQuoteAsync(s, cancellationToken);
            prices[s] = q?.Price;
        }

        var dtos = orders.Select(o =>
        {
            meta.TryGetValue(o.Symbol, out var m);
            prices.TryGetValue(o.Symbol, out var px);
            return new PendingOrderDto(
                Id: o.Id,
                Symbol: o.Symbol,
                Name: m?.Name,
                LogoUrl: m?.LogoUrl,
                Side: o.Side.ToString(),
                Status: o.Status.ToString(),
                Quantity: o.Quantity,
                LimitPrice: o.LimitPrice,
                FilledPrice: o.FilledPrice,
                CreatedAt: o.CreatedAt,
                FilledAt: o.FilledAt,
                FailureReason: o.FailureReason,
                Notes: o.Notes,
                CurrentPrice: px,
                TrailingStopPercent: o.TrailingStopPercent,
                HighWaterMark: o.HighWaterMark,
                CurrentTrigger: o.Side == OrderSide.TrailingStop ? o.LimitPrice : null);
        }).ToList();

        var open = dtos.Where(d => d.Status == nameof(OrderStatus.Pending)).ToList();
        var history = dtos.Where(d => d.Status != nameof(OrderStatus.Pending)).ToList();
        return new OrderListResponse(open, history);
    }

    private async Task<PendingOrderDto> MapAsync(PendingOrder o, CancellationToken cancellationToken)
    {
        var m = await _db.Stocks
            .Where(s => s.Symbol == o.Symbol)
            .Select(s => new { s.LogoUrl, s.Name })
            .FirstOrDefaultAsync(cancellationToken);
        var quote = await _quotes.GetQuoteAsync(o.Symbol, cancellationToken);
        return new PendingOrderDto(
            Id: o.Id,
            Symbol: o.Symbol,
            Name: m?.Name,
            LogoUrl: m?.LogoUrl,
            Side: o.Side.ToString(),
            Status: o.Status.ToString(),
            Quantity: o.Quantity,
            LimitPrice: o.LimitPrice,
            FilledPrice: o.FilledPrice,
            CreatedAt: o.CreatedAt,
            FilledAt: o.FilledAt,
            FailureReason: o.FailureReason,
            Notes: o.Notes,
            CurrentPrice: quote?.Price,
            TrailingStopPercent: o.TrailingStopPercent,
            HighWaterMark: o.HighWaterMark,
            CurrentTrigger: o.Side == OrderSide.TrailingStop ? o.LimitPrice : null);
    }
}
