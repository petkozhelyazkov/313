namespace Trading313.Api.Dtos.Stocks;

public record MoverItem(
    string Symbol,
    string? Name,
    string? Exchange,
    string? LogoUrl,
    decimal Price,
    decimal? Change,
    decimal? PercentChange);

public record MarketMoversResponse(
    IReadOnlyList<MoverItem> Gainers,
    IReadOnlyList<MoverItem> Losers,
    IReadOnlyList<MoverItem> Actives,
    DateTime FetchedAt);
