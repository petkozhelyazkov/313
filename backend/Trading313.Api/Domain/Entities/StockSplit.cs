namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Cached stock-split event from Twelve Data. Keyed by (Symbol, Date).
/// A 4-for-1 split is FromFactor=1, ToFactor=4 (one old share becomes four).
/// </summary>
public class StockSplit
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal FromFactor { get; set; }
    public decimal ToFactor { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
