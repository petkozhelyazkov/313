namespace Trading313.Api.Dtos.Portfolio;

public record PortfolioSummary(
    decimal CashBalance,
    decimal HoldingsValue,
    decimal TotalValue,
    decimal TotalInvested,
    decimal UnrealizedPl,
    decimal UnrealizedPlPct,
    decimal RealizedPlLifetime,
    IReadOnlyList<PositionDto> Positions);
