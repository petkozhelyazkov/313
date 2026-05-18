using Trading313.Api.Dtos.Portfolio;

namespace Trading313.Api.Dtos.Stocks;

public record StockDetailResponse(
    StockSearchResult Stock,
    QuoteResponse? Quote,
    HistoryResponse? History,
    PositionDto? UserPosition,
    bool InWatchlist);
