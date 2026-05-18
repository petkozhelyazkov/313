namespace Trading313.Api.Dtos.Stocks;

public record StockSearchResult(
    string Symbol,
    string Name,
    string? Exchange,
    string Currency,
    string? Country,
    string? InstrumentType,
    string? LogoUrl);
