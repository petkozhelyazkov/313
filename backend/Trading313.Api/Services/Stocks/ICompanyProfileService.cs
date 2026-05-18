using Trading313.Api.Dtos.Stocks;

namespace Trading313.Api.Services.Stocks;

public interface ICompanyProfileService
{
    /// <summary>
    /// Returns the cached profile + statistics for a symbol, refreshing from Twelve Data
    /// if the cached data is older than 7 days (or has never been fetched).
    /// </summary>
    Task<CompanyProfileResponse?> GetAsync(string symbol, CancellationToken cancellationToken = default);
}
