namespace Trading313.Api.Domain.Entities;

/// <summary>
/// One reported or upcoming earnings event for a symbol. Past entries record
/// actual EPS; future entries hold the analyst estimate.
/// </summary>
public class EarningsEntry
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateOnly ReportDate { get; set; }
    /// <summary>"BMO" (Before Market Open), "AMC" (After Market Close), or "—".</summary>
    public string? Time { get; set; }
    public decimal? EpsEstimate { get; set; }
    public decimal? EpsActual { get; set; }
    public decimal? SurprisePercent { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
