namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Latest-quote cache, one row per symbol. Survives process restarts.
/// </summary>
public class PriceCacheEntry
{
    /// <summary>Primary key is the symbol itself — one row per ticker.</summary>
    public string Symbol { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal? DayChange { get; set; }
    public decimal? DayChangePct { get; set; }
    public decimal? PreviousClose { get; set; }
    public long Volume { get; set; }
    public DateTime FetchedAt { get; set; }
    public bool IsStale { get; set; }
}
