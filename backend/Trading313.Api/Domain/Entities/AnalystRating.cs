namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Aggregated Wall-Street consensus per symbol. Refreshed on a 7-day TTL.
/// </summary>
public class AnalystRating
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; }
    public int NumAnalysts { get; set; }
    public decimal? RecommendationMean { get; set; }
    public int StrongBuy { get; set; }
    public int Buy { get; set; }
    public int Hold { get; set; }
    public int Sell { get; set; }
    public int StrongSell { get; set; }
    public decimal? TargetLow { get; set; }
    public decimal? TargetMean { get; set; }
    public decimal? TargetHigh { get; set; }
}
