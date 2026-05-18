using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Services.MarketData;

public interface IQuoteService
{
    Task<QuoteResponse?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);
}
