namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Cached dividend event (past or upcoming) from Twelve Data. Keyed by (Symbol, ExDate).
/// </summary>
public class DividendEvent
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateOnly ExDate { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
