namespace Trading313.Api.Dtos.Stocks;

public record AnalystConsensusResponse(
    string Symbol,
    int NumAnalysts,
    decimal? RecommendationMean,
    string VerdictLabel,
    int StrongBuy,
    int Buy,
    int Hold,
    int Sell,
    int StrongSell,
    decimal? TargetLow,
    decimal? TargetMean,
    decimal? TargetHigh,
    decimal? CurrentPrice,
    decimal? UpsidePct,
    DateTime FetchedAt);

public record InsiderTradeDto(
    long Id,
    string Symbol,
    string PersonName,
    string? Role,
    DateTime TransactionDate,
    string TransactionType,
    decimal Shares,
    decimal? PricePerShare,
    decimal? Value);

public record InsiderSummaryResponse(
    string Symbol,
    int Last90DaysBuyCount,
    int Last90DaysSellCount,
    decimal Last90DaysBuyValue,
    decimal Last90DaysSellValue,
    IReadOnlyList<InsiderTradeDto> RecentTrades);
