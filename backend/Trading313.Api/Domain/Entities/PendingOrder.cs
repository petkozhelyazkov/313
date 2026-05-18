using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Domain.Entities;

/// <summary>
/// A user-placed limit / stop order that the background OrderExecutionService
/// will fire automatically when the price condition is met.
/// </summary>
public class PendingOrder
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public OrderSide Side { get; set; }
    public decimal LimitPrice { get; set; }
    public decimal Quantity { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FilledAt { get; set; }
    public decimal? FilledPrice { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }

    /// <summary>For TrailingStop orders: % distance below the highest-seen price (e.g. 5 = trail 5% below peak).</summary>
    public decimal? TrailingStopPercent { get; set; }

    /// <summary>For TrailingStop orders: highest price seen since the order was placed. Trigger = HighWaterMark * (1 - TrailingStopPercent/100).</summary>
    public decimal? HighWaterMark { get; set; }
}
