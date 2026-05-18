using System.ComponentModel.DataAnnotations;
using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Dtos.Orders;

public class PlaceOrderRequest
{
    [Required, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    [Required]
    public OrderSide Side { get; set; }

    [Range(0.00000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public decimal Quantity { get; set; }

    /// <summary>For Limit/Stop orders: the static trigger price. Ignored for TrailingStop.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "Limit price cannot be negative.")]
    public decimal LimitPrice { get; set; }

    /// <summary>For TrailingStop orders: distance below the peak in % (e.g. 5 = 5%). Required if Side is TrailingStop.</summary>
    [Range(0.01, 99.99)]
    public decimal? TrailingStopPercent { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public record PendingOrderDto(
    long Id,
    string Symbol,
    string? Name,
    string? LogoUrl,
    string Side,
    string Status,
    decimal Quantity,
    decimal LimitPrice,
    decimal? FilledPrice,
    DateTime CreatedAt,
    DateTime? FilledAt,
    string? FailureReason,
    string? Notes,
    decimal? CurrentPrice,
    decimal? TrailingStopPercent = null,
    decimal? HighWaterMark = null,
    decimal? CurrentTrigger = null);

public record OrderListResponse(
    IReadOnlyList<PendingOrderDto> Open,
    IReadOnlyList<PendingOrderDto> History);
