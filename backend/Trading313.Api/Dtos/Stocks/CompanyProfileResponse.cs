namespace Trading313.Api.Dtos.Stocks;

public record CompanyProfileResponse(
    string Symbol,
    string Name,
    string? LogoUrl,
    string? Sector,
    string? Industry,
    int? Employees,
    string? Website,
    string? Description,
    string? Ceo,
    decimal? MarketCap,
    decimal? PeRatio,
    decimal? Eps,
    decimal? DividendYield,
    decimal? Beta,
    decimal? FiftyTwoWeekHigh,
    decimal? FiftyTwoWeekLow);
