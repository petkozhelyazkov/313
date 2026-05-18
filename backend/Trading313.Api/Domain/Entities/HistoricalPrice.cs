namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Daily OHLC bar for a symbol. Past closes are immutable — cache forever.
/// </summary>
public class HistoricalPrice
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
