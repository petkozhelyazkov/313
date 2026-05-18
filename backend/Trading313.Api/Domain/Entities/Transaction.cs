using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Source-of-truth record for a Buy or Sell. Positions are materialized from
/// these. Currency is implicit (USD v1).
/// </summary>
public class Transaction
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal PricePerShare { get; set; }
    public decimal Fees { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime ExecutedAt { get; set; }

    /// <summary>Set on Sell rows only — gain/loss vs. average cost at time of sell.</summary>
    public decimal? RealizedPl { get; set; }

    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
