namespace Trading313.Api.Domain.Entities;

public enum AlertDirection
{
    Above = 1,
    Below = 2,
}

public enum AlertStatus
{
    Active = 1,
    Triggered = 2,
    Cancelled = 3,
}

/// <summary>
/// User-set notification — fires when the current price crosses a configured trigger.
/// Evaluated by AlertEvaluationService background after every QuoteRefresh tick.
/// </summary>
public class PriceAlert
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public AlertDirection Direction { get; set; }
    public decimal TriggerPrice { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TriggeredAt { get; set; }
    public decimal? TriggeredPrice { get; set; }
    public bool Acknowledged { get; set; }
    public string? Notes { get; set; }
}
