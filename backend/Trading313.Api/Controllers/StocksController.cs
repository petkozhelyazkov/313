using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Trading313.Api.Data;
using Trading313.Api.Dtos.Stocks;
using Trading313.Api.Infrastructure.MarketData;
using Trading313.Api.Services.MarketData;
using Trading313.Api.Services.Portfolio;
using Trading313.Api.Services.Stocks;
using Trading313.Api.Services.Watchlist;

namespace Trading313.Api.Controllers;

/// <summary>
/// Public stock catalog + market data. Anonymous-allowed.
/// </summary>
[ApiController]
[Route("api/stocks")]
[Produces("application/json")]
public class StocksController : ControllerBase
{
    private readonly IStockService _stocks;
    private readonly IQuoteService _quotes;
    private readonly IHistoryService _history;
    private readonly IPortfolioQueryService _portfolioQuery;
    private readonly IWatchlistService _watchlist;
    private readonly ICompanyProfileService _profile;
    private readonly IAnalystService _analyst;
    private readonly IInsiderService _insider;
    private readonly ITwelveDataClient _td;
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _db;

    public StocksController(
        IStockService stocks,
        IQuoteService quotes,
        IHistoryService history,
        IPortfolioQueryService portfolioQuery,
        IWatchlistService watchlist,
        ICompanyProfileService profile,
        IAnalystService analyst,
        IInsiderService insider,
        ITwelveDataClient td,
        IMemoryCache cache,
        AppDbContext db)
    {
        _stocks = stocks;
        _quotes = quotes;
        _history = history;
        _portfolioQuery = portfolioQuery;
        _watchlist = watchlist;
        _profile = profile;
        _analyst = analyst;
        _insider = insider;
        _td = td;
        _cache = cache;
        _db = db;
    }

    /// <summary>Wall-Street consensus + price targets for a symbol.</summary>
    [HttpGet("{symbol}/analyst")]
    [ProducesResponseType(typeof(AnalystConsensusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAnalyst(string symbol, CancellationToken cancellationToken)
    {
        var result = await _analyst.GetAsync(symbol, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    /// <summary>Recent insider transactions + 90-day summary for a symbol.</summary>
    [HttpGet("{symbol}/insiders")]
    [ProducesResponseType(typeof(InsiderSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInsiders(string symbol, CancellationToken cancellationToken)
    {
        var result = await _insider.GetAsync(symbol, cancellationToken);
        return Ok(result);
    }

    /// <summary>Top market movers — gainers, losers, most active. Cached 5 minutes.</summary>
    [HttpGet("movers")]
    [ProducesResponseType(typeof(MarketMoversResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Movers(CancellationToken cancellationToken)
    {
        const string cacheKey = "stocks.movers";
        if (_cache.TryGetValue(cacheKey, out MarketMoversResponse? cached) && cached is not null)
        {
            return Ok(cached);
        }

        var gainers = await _td.GetMarketMoversAsync("gainers", cancellationToken);
        var losers = await _td.GetMarketMoversAsync("losers", cancellationToken);
        var actives = await _td.GetMarketMoversAsync("actives", cancellationToken);

        var allSymbols = gainers.Concat(losers).Concat(actives).Select(m => m.Symbol).Distinct().ToList();
        var logos = await _db.Stocks
            .Where(s => allSymbols.Contains(s.Symbol))
            .Select(s => new { s.Symbol, s.LogoUrl })
            .ToDictionaryAsync(s => s.Symbol, s => s.LogoUrl, cancellationToken);

        MoverItem Map(TdMover m) => new(
            Symbol: m.Symbol,
            Name: m.Name,
            Exchange: m.Exchange,
            LogoUrl: logos.TryGetValue(m.Symbol, out var url) ? url : null,
            Price: m.Price,
            Change: m.Change,
            PercentChange: m.PercentChange);

        var response = new MarketMoversResponse(
            Gainers: gainers.Take(5).Select(Map).ToList(),
            Losers: losers.Take(5).Select(Map).ToList(),
            Actives: actives.Take(5).Select(Map).ToList(),
            FetchedAt: DateTime.UtcNow);

        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
        return Ok(response);
    }

    /// <summary>Company profile + statistics. 7-day cache; anonymous-allowed.</summary>
    [HttpGet("{symbol}/profile")]
    [ProducesResponseType(typeof(CompanyProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Profile(string symbol, CancellationToken cancellationToken)
    {
        var profile = await _profile.GetAsync(symbol, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>
    /// Batched sparkline data — last ~30 daily closes for each requested symbol.
    /// Reads only from cached HistoricalPrices — zero Twelve Data calls.
    /// </summary>
    [HttpGet("sparklines")]
    [ProducesResponseType(typeof(IDictionary<string, IEnumerable<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sparklines([FromQuery] string symbols, [FromQuery] int days = 30, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbols)) return Ok(new Dictionary<string, object[]>());
        if (days is < 7 or > 365) days = 30;

        var symList = symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => s.ToUpperInvariant())
            .Take(50)
            .ToList();
        if (symList.Count == 0) return Ok(new Dictionary<string, object[]>());

        var since = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
        var prices = await _db.HistoricalPrices
            .Where(h => symList.Contains(h.Symbol) && h.Date >= since)
            .OrderBy(h => h.Date)
            .Select(h => new { h.Symbol, h.Date, h.Close })
            .ToListAsync(cancellationToken);

        var grouped = prices
            .GroupBy(p => p.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => new { date = p.Date.ToString("yyyy-MM-dd"), close = p.Close }).ToArray());

        return Ok(grouped);
    }

    /// <summary>Composite stock detail: metadata + quote + 1Y history + (if signed in) user's position.</summary>
    [HttpGet("{symbol}")]
    [ProducesResponseType(typeof(StockDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail(string symbol, CancellationToken cancellationToken)
    {
        var stock = await _stocks.GetBySymbolAsync(symbol, cancellationToken);
        if (stock is null) return NotFound();

        // Lazy-fetch the company logo (1 Twelve Data call per new symbol, ever).
        await _stocks.EnsureLogoCachedAsync(symbol, cancellationToken);
        stock = await _stocks.GetBySymbolAsync(symbol, cancellationToken) ?? stock;

        var quoteTask = _quotes.GetQuoteAsync(symbol, cancellationToken);
        var historyTask = _history.GetHistoryAsync(symbol, "1Y", cancellationToken);
        await Task.WhenAll(quoteTask, historyTask);

        Dtos.Portfolio.PositionDto? userPosition = null;
        bool inWatchlist = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                userPosition = await _portfolioQuery.GetPositionAsync(userId, symbol, cancellationToken);
                inWatchlist = await _watchlist.ContainsAsync(userId, symbol, cancellationToken);
            }
        }

        var response = new StockDetailResponse(
            Stock: stock,
            Quote: quoteTask.Result,
            History: historyTask.Result,
            UserPosition: userPosition,
            InWatchlist: inWatchlist);

        return Ok(response);
    }

    /// <summary>Search the symbol catalog by symbol or name.</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<StockSearchResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10, CancellationToken cancellationToken = default)
    {
        var results = await _stocks.SearchAsync(q ?? string.Empty, limit, cancellationToken);
        return Ok(results);
    }

    /// <summary>Latest quote for a symbol. Two-tier cached; rate-limit-aware.</summary>
    [HttpGet("{symbol}/quote")]
    [ProducesResponseType(typeof(QuoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Quote(string symbol, CancellationToken cancellationToken)
    {
        var quote = await _quotes.GetQuoteAsync(symbol, cancellationToken);
        return quote is null ? NotFound() : Ok(quote);
    }

    /// <summary>Historical OHLC bars. Range presets: 1M, 3M, 6M, 1Y, 5Y, MAX.</summary>
    [HttpGet("{symbol}/history")]
    [ProducesResponseType(typeof(HistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> History(string symbol, [FromQuery] string range = "1Y", CancellationToken cancellationToken = default)
    {
        var history = await _history.GetHistoryAsync(symbol, range, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }
}
