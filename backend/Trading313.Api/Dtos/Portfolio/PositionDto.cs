namespace Trading313.Api.Dtos.Portfolio;

public record PositionDto(
    string Symbol,
    decimal Quantity,
    decimal AverageCost,
    decimal TotalInvested,
    decimal RealizedPlLifetime,
    decimal? CurrentPrice,
    decimal? CurrentValue,
    decimal? UnrealizedPl,
    decimal? UnrealizedPlPct,
    decimal? Weight,
    DateTime FirstPurchasedAt,
    DateTime LastTransactionAt,
    bool IsClosed,
    string? LogoUrl = null,
    string? Name = null,
    string? Notes = null,
    string? Tags = null);

public class UpdatePositionRequest
{
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
