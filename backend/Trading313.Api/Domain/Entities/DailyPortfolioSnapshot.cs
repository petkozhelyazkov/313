namespace Trading313.Api.Domain.Entities;

/// <summary>
/// End-of-day snapshot of a user's portfolio value. One row per (user, date).
/// Used by the Analytics page performance chart.
/// </summary>
public class DailyPortfolioSnapshot
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateOnly SnapshotDate { get; set; }
    public decimal CashBalance { get; set; }
    public decimal HoldingsValue { get; set; }
    public decimal TotalValue { get; set; }
    public decimal TotalInvestedAtSnapshot { get; set; }
    public decimal UnrealizedPl { get; set; }
}
