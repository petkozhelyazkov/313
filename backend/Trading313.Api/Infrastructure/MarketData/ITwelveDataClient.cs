using Trading313.Api.Infrastructure.MarketData.Models;

namespace Trading313.Api.Infrastructure.MarketData;

public interface ITwelveDataClient
{
    Task<TdQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Batched quote fetch (the only way to stay under 8 req/min with many symbols).</summary>
    Task<IReadOnlyDictionary<string, TdQuote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default);

    Task<TdTimeSeries?> GetTimeSeriesAsync(
        string symbol,
        string interval,
        DateOnly? startDate,
        DateOnly? endDate,
        int? outputSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TdSymbolMatch>> SearchSymbolsAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Fetch the company-logo URL for a symbol. Returns null if Twelve Data has no logo for it.</summary>
    Task<string?> GetLogoUrlAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Fetch the /profile payload (sector, industry, employees, website, description, ceo).</summary>
    Task<TdCompanyProfile?> GetProfileAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Fetch the /statistics payload (market cap, P/E, EPS, dividend yield, beta, 52w high/low).</summary>
    Task<TdCompanyStatistics?> GetStatisticsAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Fetch past + upcoming earnings entries for a symbol.</summary>
    Task<IReadOnlyList<TdEarningsEntry>> GetEarningsAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Top market movers — gainers, losers, or active stocks.</summary>
    Task<IReadOnlyList<TdMover>> GetMarketMoversAsync(string direction, CancellationToken cancellationToken = default);

    /// <summary>Fetch dividend events (past + upcoming) for a symbol over the given date range.</summary>
    Task<IReadOnlyList<TdDividendEntry>> GetDividendsAsync(string symbol, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);

    /// <summary>Fetch stock-split events for a symbol over the given date range.</summary>
    Task<IReadOnlyList<TdSplitEntry>> GetSplitsAsync(string symbol, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);
}

public record TdDividendEntry(
    DateOnly ExDate,
    DateOnly? PaymentDate,
    decimal Amount);

public record TdSplitEntry(
    DateOnly Date,
    decimal FromFactor,
    decimal ToFactor);

public record TdMover(
    string Symbol,
    string? Name,
    string? Exchange,
    decimal Price,
    decimal? Change,
    decimal? PercentChange,
    long? Volume);

public record TdEarningsEntry(
    DateOnly ReportDate,
    string? Time,
    decimal? EpsEstimate,
    decimal? EpsActual,
    decimal? SurprisePercent);

public record TdCompanyProfile(
    string Symbol,
    string? Sector,
    string? Industry,
    int? Employees,
    string? Website,
    string? Description,
    string? Ceo);

public record TdCompanyStatistics(
    string Symbol,
    decimal? MarketCap,
    decimal? PeRatio,
    decimal? Eps,
    decimal? DividendYield,
    decimal? Beta,
    decimal? FiftyTwoWeekHigh,
    decimal? FiftyTwoWeekLow);
