namespace Trading313.Api.Dtos.Stocks;

public record HistoryResponse(
    string Symbol,
    string Range,
    IReadOnlyList<HistoryPoint> Points);

public record HistoryPoint(
    DateOnly Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);
