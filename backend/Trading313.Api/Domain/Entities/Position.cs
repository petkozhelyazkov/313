namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Materialized current position for a (user, symbol). Recomputed on every transaction
/// inside the same DB transaction. Closed positions are preserved with IsClosed=true.
/// </summary>
public class Position
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal RealizedPlLifetime { get; set; }
    public DateTime FirstPurchasedAt { get; set; }
    public DateTime LastTransactionAt { get; set; }
    public bool IsClosed { get; set; }
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
