using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Infrastructure.MarketData;

namespace Trading313.Api.Services.Stocks;

public class CompanyProfileService : ICompanyProfileService
{
    private static readonly TimeSpan ProfileTtl = TimeSpan.FromDays(7);

    private readonly AppDbContext _db;
    private readonly IStockService _stockService;
    private readonly ITwelveDataClient _td;
    private readonly ILogger<CompanyProfileService> _logger;

    public CompanyProfileService(
        AppDbContext db,
        IStockService stockService,
        ITwelveDataClient td,
        ILogger<CompanyProfileService> logger)
    {
        _db = db;
        _stockService = stockService;
        _td = td;
        _logger = logger;
    }

    public async Task<CompanyProfileResponse?> GetAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var sym = symbol.Trim().ToUpperInvariant();

        // Make sure the symbol is in our catalog (lazy upsert from /symbol_search).
        var stockSearch = await _stockService.GetBySymbolAsync(sym, cancellationToken);
        if (stockSearch is null) return null;

        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.Symbol == sym, cancellationToken);
        if (stock is null) return null;

        var stale = stock.LastMetadataRefreshAt is null
                    || (DateTime.UtcNow - stock.LastMetadataRefreshAt.Value) > ProfileTtl
                    || string.IsNullOrEmpty(stock.Sector); // never enriched yet
        if (stale)
        {
            await RefreshAsync(stock, cancellationToken);
        }

        return new CompanyProfileResponse(
            Symbol: stock.Symbol,
            Name: stock.Name,
            LogoUrl: stock.LogoUrl,
            Sector: stock.Sector,
            Industry: stock.Industry,
            Employees: stock.Employees,
            Website: stock.Website,
            Description: stock.Description,
            Ceo: stock.Ceo,
            MarketCap: stock.MarketCap,
            PeRatio: stock.PeRatio,
            Eps: stock.Eps,
            DividendYield: stock.DividendYield,
            Beta: stock.Beta,
            FiftyTwoWeekHigh: stock.FiftyTwoWeekHigh,
            FiftyTwoWeekLow: stock.FiftyTwoWeekLow);
    }

    private async Task RefreshAsync(Domain.Entities.Stock stock, CancellationToken cancellationToken)
    {
        try
        {
            var profile = await _td.GetProfileAsync(stock.Symbol, cancellationToken);
            if (profile is not null)
            {
                stock.Sector = profile.Sector ?? stock.Sector;
                stock.Industry = profile.Industry ?? stock.Industry;
                stock.Employees = profile.Employees ?? stock.Employees;
                stock.Website = profile.Website ?? stock.Website;
                stock.Description = profile.Description ?? stock.Description;
                stock.Ceo = profile.Ceo ?? stock.Ceo;
            }

            var stats = await _td.GetStatisticsAsync(stock.Symbol, cancellationToken);
            if (stats is not null)
            {
                stock.MarketCap = stats.MarketCap ?? stock.MarketCap;
                stock.PeRatio = stats.PeRatio ?? stock.PeRatio;
                stock.Eps = stats.Eps ?? stock.Eps;
                stock.DividendYield = stats.DividendYield ?? stock.DividendYield;
                stock.Beta = stats.Beta ?? stock.Beta;
                stock.FiftyTwoWeekHigh = stats.FiftyTwoWeekHigh ?? stock.FiftyTwoWeekHigh;
                stock.FiftyTwoWeekLow = stats.FiftyTwoWeekLow ?? stock.FiftyTwoWeekLow;
            }

            stock.LastMetadataRefreshAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Profile refresh failed for {Symbol}", stock.Symbol);
        }
    }
}
