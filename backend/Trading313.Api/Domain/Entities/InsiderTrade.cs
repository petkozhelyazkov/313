namespace Trading313.Api.Domain.Entities;

public class InsiderTrade
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty; // Buy / Sell / Award / Option
    public decimal Shares { get; set; }
    public decimal? PricePerShare { get; set; }
    public decimal? Value { get; set; }
    public DateTime FetchedAt { get; set; }
}
