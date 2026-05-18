namespace Trading313.Api.Domain.Entities;

public enum CashTransactionType
{
    Deposit = 1,
    Withdraw = 2,
}

/// <summary>
/// Non-trade cash event — a virtual "deposit" or "withdraw" against the
/// user's paper cash balance. Lets users top up beyond the starting $10k.
/// </summary>
public class CashTransaction
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public CashTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
