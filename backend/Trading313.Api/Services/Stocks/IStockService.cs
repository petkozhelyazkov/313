using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Services.Stocks;

public interface IStockService
{
    Task<IReadOnlyList<StockSearchResult>> SearchAsync(string query, int limit = 10, CancellationToken cancellationToken = default);
    Task<StockSearchResult?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Cache the company logo URL on the Stock row (idempotent — skips if already cached).</summary>
    Task EnsureLogoCachedAsync(string symbol, CancellationToken cancellationToken = default);
}
