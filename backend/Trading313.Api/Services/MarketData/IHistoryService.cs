using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Services.MarketData;

public interface IHistoryService
{
    Task<HistoryResponse?> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default);
}
