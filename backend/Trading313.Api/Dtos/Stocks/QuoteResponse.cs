namespace Trading313.Api.Dtos.Stocks;

public record QuoteResponse(
    string Symbol,
    decimal Price,
    decimal? DayChange,
    decimal? DayChangePct,
    decimal? PreviousClose,
    long Volume,
    DateTime FetchedAt,
    bool IsStale);
