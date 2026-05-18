using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Domain.Enums;
using Trading313.Api.Dtos.Portfolio;
using Trading313.Api.Services.Portfolio;

namespace Trading313.Api.Background;

/// <summary>
/// Evaluates every Pending order against the latest PriceCache every 60 seconds.
/// Triggered orders are fired through the existing PortfolioService.BuyAsync /
/// SellAsync — same code path as manual trades, same DB transaction semantics.
/// </summary>
public class OrderExecutionService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExecutionService> _logger;

    public OrderExecutionService(IServiceScopeFactory scopeFactory, ILogger<OrderExecutionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExecutionService started; tick every {Seconds}s", TickInterval.TotalSeconds);

        // Stagger from QuoteRefreshService — start 30s after that one's first tick.
        try { await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(TickInterval);
        while (await SafeWaitAsync(timer, stoppingToken))
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderExecutionService tick threw");
            }
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try { return await timer.WaitForNextTickAsync(cancellationToken); }
        catch (OperationCanceledException) { return false; }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var portfolio = scope.ServiceProvider.GetRequiredService<IPortfolioService>();

        var pending = await db.PendingOrders
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync(cancellationToken);
        if (pending.Count == 0) return;

        var symbols = pending.Select(o => o.Symbol).Distinct().ToList();
        var prices = await db.PriceCache
            .Where(p => symbols.Contains(p.Symbol))
            .ToDictionaryAsync(p => p.Symbol, p => p.Price, cancellationToken);

        int fired = 0;
        foreach (var order in pending)
        {
            if (!prices.TryGetValue(order.Symbol, out var current)) continue;

            // For trailing stops: update the high-water mark if price advanced, then
            // recompute the trigger as HWM * (1 - pct/100). The LimitPrice column is
            // overloaded to store the current trigger for trailing orders.
            if (order.Side == OrderSide.TrailingStop && order.TrailingStopPercent is { } pct)
            {
                var hwm = order.HighWaterMark ?? current;
                if (current > hwm) hwm = current;
                order.HighWaterMark = hwm;
                order.LimitPrice = Math.Round(hwm * (1m - pct / 100m), 4);
            }

            var shouldFire = order.Side switch
            {
                OrderSide.LimitBuy => current <= order.LimitPrice,
                OrderSide.LimitSell => current >= order.LimitPrice,
                OrderSide.StopLoss => current <= order.LimitPrice,
                OrderSide.TrailingStop => current <= order.LimitPrice,
                _ => false,
            };
            if (!shouldFire) continue;

            try
            {
                bool isBuy = order.Side == OrderSide.LimitBuy;
                var result = isBuy
                    ? await portfolio.BuyAsync(order.UserId, new BuyRequest { Symbol = order.Symbol, Quantity = order.Quantity, Notes = order.Notes }, cancellationToken)
                    : await portfolio.SellAsync(order.UserId, new SellRequest { Symbol = order.Symbol, Quantity = order.Quantity, Notes = order.Notes }, cancellationToken);

                _ = result; // result is consumed below

                if (result.Succeeded)
                {
                    order.Status = OrderStatus.Filled;
                    order.FilledAt = DateTime.UtcNow;
                    order.FilledPrice = result.Value!.Transaction.PricePerShare;
                    fired++;
                }
                else
                {
                    order.Status = OrderStatus.FailedExecution;
                    order.FilledAt = DateTime.UtcNow;
                    order.FailureReason = result.ErrorMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order {OrderId} execution threw", order.Id);
                order.Status = OrderStatus.FailedExecution;
                order.FilledAt = DateTime.UtcNow;
                order.FailureReason = ex.Message;
            }
        }

        if (fired > 0 || pending.Any(o => o.Status != OrderStatus.Pending))
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        if (fired > 0)
        {
            _logger.LogInformation("Order execution: {Fired}/{Total} orders filled this tick", fired, pending.Count);
        }
    }
}
