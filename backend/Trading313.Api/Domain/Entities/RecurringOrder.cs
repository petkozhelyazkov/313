using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Domain.Entities;

/// <summary>
/// A Dollar-Cost-Averaging rule: spend a fixed cash amount on a symbol on a recurring schedule.
/// Background <c>RecurringOrderService</c> evaluates these every hour and fires market buys.
/// </summary>
public class RecurringOrder
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public decimal CashAmount { get; set; }
    public RecurringFrequency Frequency { get; set; }
    public DateTime NextRunAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int SuccessfulRuns { get; set; }
    public int FailedRuns { get; set; }
    public string? LastFailureReason { get; set; }
}
