using Trading313.Api.Dtos.Portfolio;

namespace Trading313.Api.Services.Portfolio;

public interface IPortfolioQueryService
{
    Task<PortfolioSummary> GetSummaryAsync(string userId, bool includeClosed, CancellationToken cancellationToken = default);
    Task<TransactionListResponse> GetTransactionsAsync(string userId, int page, int pageSize, string? symbolFilter, string? tagFilter = null, CancellationToken cancellationToken = default);
    Task<PositionDto?> GetPositionAsync(string userId, string symbol, CancellationToken cancellationToken = default);
}
