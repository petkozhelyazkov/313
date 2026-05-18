namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Catalog row for a tradeable symbol. Populated lazily from Twelve Data.
/// </summary>
public class Stock
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Exchange { get; set; }
    public string Currency { get; set; } = "USD";
    public string? InstrumentType { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastMetadataRefreshAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>URL of the company logo image (from Twelve Data /logo).</summary>
    public string? LogoUrl { get; set; }

    // ─── Profile data (from /profile, refreshed weekly) ──────────────────────
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public int? Employees { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? Ceo { get; set; }

    // ─── Statistics (from /statistics, refreshed weekly) ─────────────────────
    public decimal? MarketCap { get; set; }
    public decimal? PeRatio { get; set; }
    public decimal? Eps { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? Beta { get; set; }
    public decimal? FiftyTwoWeekHigh { get; set; }
    public decimal? FiftyTwoWeekLow { get; set; }
}
