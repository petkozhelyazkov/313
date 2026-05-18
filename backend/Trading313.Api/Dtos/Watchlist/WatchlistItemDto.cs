using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Dtos.Watchlist;

public record WatchlistItemDto(
    long Id,
    string Symbol,
    string? Notes,
    DateTime AddedAt,
    QuoteResponse? Quote,
    string? LogoUrl = null,
    string? Name = null,
    string ListName = "Default");

public record WatchlistSummaryDto(string ListName, int Count);
