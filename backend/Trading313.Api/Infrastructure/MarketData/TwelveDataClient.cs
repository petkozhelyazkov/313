using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Trading313.Api.Infrastructure.MarketData.Models;

namespace Trading313.Api.Infrastructure.MarketData;

public class TwelveDataClient : ITwelveDataClient
{
    public const string HttpClientName = "TwelveData";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly TwelveDataOptions _options;
    private readonly TwelveDataRateLimiter _limiter;
    private readonly ILogger<TwelveDataClient> _logger;

    public TwelveDataClient(
        IHttpClientFactory httpClientFactory,
        IOptions<TwelveDataOptions> options,
        TwelveDataRateLimiter limiter,
        ILogger<TwelveDataClient> logger)
    {
        _http = httpClientFactory.CreateClient(HttpClientName);
        _options = options.Value;
        _limiter = limiter;
        _logger = logger;
    }

    public async Task<TdQuote?> GetQuoteAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var url = $"/quote?symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";
        using var doc = await SendAsJsonDocAsync(url, "/quote", symbol, cancellationToken);
        var raw = doc.RootElement.Deserialize<TdQuoteRaw>(JsonOptions);
        EnsureNotError(raw?.Status, raw?.Code, raw?.Message, "/quote");
        return raw is null ? null : MapQuote(raw);
    }

    public async Task<IReadOnlyDictionary<string, TdQuote>> GetQuotesAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var list = symbols.Select(s => s.Trim().ToUpperInvariant()).Where(s => s.Length > 0).Distinct().ToArray();
        if (list.Length == 0) return new Dictionary<string, TdQuote>();

        var joined = string.Join(",", list);
        var url = $"/quote?symbol={Uri.EscapeDataString(joined)}&apikey={_options.ApiKey}";

        using var doc = await SendAsJsonDocAsync(url, "/quote", joined, cancellationToken);
        var result = new Dictionary<string, TdQuote>(StringComparer.OrdinalIgnoreCase);

        // When >1 symbol, response is a dict keyed by symbol: { "AAPL": {...}, "MSFT": {...} }.
        // When 1 symbol, response is the quote object directly.
        if (list.Length == 1)
        {
            var raw = doc.RootElement.Deserialize<TdQuoteRaw>(JsonOptions);
            EnsureNotError(raw?.Status, raw?.Code, raw?.Message, "/quote (batch)");
            if (raw is not null && !string.IsNullOrEmpty(raw.Symbol))
            {
                result[raw.Symbol] = MapQuote(raw);
            }
            return result;
        }

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var raw = prop.Value.Deserialize<TdQuoteRaw>(JsonOptions);
            if (raw is null) continue;
            if (string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Twelve Data quote error for {Symbol}: code={Code} message={Message}",
                    prop.Name, raw.Code, raw.Message);
                continue;
            }
            if (string.IsNullOrEmpty(raw.Symbol)) continue;
            result[raw.Symbol] = MapQuote(raw);
        }
        return result;
    }

    public async Task<TdTimeSeries?> GetTimeSeriesAsync(
        string symbol,
        string interval,
        DateOnly? startDate,
        DateOnly? endDate,
        int? outputSize,
        CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var qp = new List<string>
        {
            $"symbol={Uri.EscapeDataString(symbol)}",
            $"interval={Uri.EscapeDataString(interval)}",
            $"apikey={_options.ApiKey}",
        };
        if (startDate is { } sd) qp.Add($"start_date={sd:yyyy-MM-dd}");
        if (endDate is { } ed) qp.Add($"end_date={ed:yyyy-MM-dd}");
        if (outputSize is { } os) qp.Add($"outputsize={os}");

        var url = "/time_series?" + string.Join("&", qp);

        using var doc = await SendAsJsonDocAsync(url, "/time_series", symbol, cancellationToken);
        var raw = doc.RootElement.Deserialize<TdTimeSeriesRaw>(JsonOptions);
        EnsureNotError(raw?.Status, raw?.Code, raw?.Message, "/time_series");
        if (raw?.Meta is null || raw.Values is null) return null;

        var points = raw.Values
            .Select(MapPoint)
            .Where(p => p is not null)
            .Select(p => p!)
            .OrderBy(p => p.Date)
            .ToList();

        return new TdTimeSeries(raw.Meta.Symbol ?? symbol, raw.Meta.Interval ?? interval, points);
    }

    public async Task<IReadOnlyList<TdMover>> GetMarketMoversAsync(string direction, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var d = direction.ToLowerInvariant() switch
        {
            "gainers" or "losers" or "actives" => direction.ToLowerInvariant(),
            _ => "gainers",
        };
        var url = $"/market_movers/stocks?direction={d}&country=United States&apikey={_options.ApiKey}";
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/market_movers", d, cancellationToken);
            var raw = doc.RootElement.Deserialize<TdMarketMoversRaw>(JsonOptions);
            if (raw?.Values is null || string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<TdMover>();
            return raw.Values
                .Where(v => !string.IsNullOrEmpty(v.Symbol) && v.Last is > 0)
                .Select(v => new TdMover(
                    Symbol: v.Symbol!,
                    Name: v.Name,
                    Exchange: v.Exchange,
                    Price: v.Last!.Value,
                    Change: v.Change,
                    PercentChange: v.PercentChange,
                    Volume: v.Volume))
                .Take(10)
                .ToList();
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Market movers fetch failed for {Direction}", d);
            return Array.Empty<TdMover>();
        }
    }

    public async Task<IReadOnlyList<TdEarningsEntry>> GetEarningsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var url = $"/earnings?symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/earnings", symbol, cancellationToken);
            var raw = doc.RootElement.Deserialize<TdEarningsRaw>(JsonOptions);
            if (raw?.Earnings is null || string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<TdEarningsEntry>();

            var list = new List<TdEarningsEntry>();
            foreach (var e in raw.Earnings)
            {
                if (string.IsNullOrEmpty(e.Date)) continue;
                if (!DateOnly.TryParseExact(e.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date))
                    continue;
                list.Add(new TdEarningsEntry(date, e.Time, e.EpsEstimate, e.EpsActual, e.SurprisePrc));
            }
            return list;
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Earnings fetch failed for {Symbol}", symbol);
            return Array.Empty<TdEarningsEntry>();
        }
    }

    public async Task<IReadOnlyList<TdDividendEntry>> GetDividendsAsync(string symbol, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var qp = new List<string>
        {
            $"symbol={Uri.EscapeDataString(symbol)}",
            $"apikey={_options.ApiKey}",
        };
        if (startDate is { } sd) qp.Add($"start_date={sd:yyyy-MM-dd}");
        if (endDate is { } ed) qp.Add($"end_date={ed:yyyy-MM-dd}");
        var url = "/dividends?" + string.Join("&", qp);
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/dividends", symbol, cancellationToken);
            var raw = doc.RootElement.Deserialize<TdDividendsRaw>(JsonOptions);
            if (raw?.Dividends is null || string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<TdDividendEntry>();

            var list = new List<TdDividendEntry>();
            foreach (var d in raw.Dividends)
            {
                if (string.IsNullOrEmpty(d.ExDate) || d.Amount is null) continue;
                if (!DateOnly.TryParseExact(d.ExDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ex)) continue;
                DateOnly? pay = null;
                if (!string.IsNullOrEmpty(d.PaymentDate) && DateOnly.TryParseExact(d.PaymentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var pd))
                    pay = pd;
                list.Add(new TdDividendEntry(ex, pay, d.Amount.Value));
            }
            return list;
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Dividends fetch failed for {Symbol}", symbol);
            return Array.Empty<TdDividendEntry>();
        }
    }

    public async Task<IReadOnlyList<TdSplitEntry>> GetSplitsAsync(string symbol, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var qp = new List<string>
        {
            $"symbol={Uri.EscapeDataString(symbol)}",
            $"apikey={_options.ApiKey}",
        };
        if (startDate is { } sd) qp.Add($"start_date={sd:yyyy-MM-dd}");
        if (endDate is { } ed) qp.Add($"end_date={ed:yyyy-MM-dd}");
        var url = "/splits?" + string.Join("&", qp);
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/splits", symbol, cancellationToken);
            var raw = doc.RootElement.Deserialize<TdSplitsRaw>(JsonOptions);
            if (raw?.Splits is null || string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase))
                return Array.Empty<TdSplitEntry>();

            var list = new List<TdSplitEntry>();
            foreach (var s in raw.Splits)
            {
                if (string.IsNullOrEmpty(s.Date) || s.FromFactor is null || s.ToFactor is null) continue;
                if (!DateOnly.TryParseExact(s.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) continue;
                list.Add(new TdSplitEntry(date, s.FromFactor.Value, s.ToFactor.Value));
            }
            return list;
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Splits fetch failed for {Symbol}", symbol);
            return Array.Empty<TdSplitEntry>();
        }
    }

    public async Task<TdCompanyProfile?> GetProfileAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var url = $"/profile?symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/profile", symbol, cancellationToken);
            var raw = doc.RootElement.Deserialize<TdProfileRaw>(JsonOptions);
            if (raw is null || string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase)) return null;
            return new TdCompanyProfile(
                Symbol: raw.Symbol ?? symbol,
                Sector: raw.Sector,
                Industry: raw.Industry,
                Employees: raw.Employees,
                Website: raw.Website,
                Description: raw.Description,
                Ceo: raw.Ceo);
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Profile fetch failed for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<TdCompanyStatistics?> GetStatisticsAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var url = $"/statistics?symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/statistics", symbol, cancellationToken);
            if (!doc.RootElement.TryGetProperty("statistics", out var stats)) return null;

            decimal? GetDec(JsonElement parent, string key) =>
                parent.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null
                    ? (v.ValueKind == JsonValueKind.Number
                        ? v.GetDecimal()
                        : (decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null))
                    : null;

            decimal? marketCap = null, pe = null, eps = null, divYield = null, beta = null, high52 = null, low52 = null;

            if (stats.TryGetProperty("stock_statistics", out var ss))
                marketCap = GetDec(ss, "market_capitalization");
            if (stats.TryGetProperty("valuations_metrics", out var vm))
                pe = GetDec(vm, "trailing_pe") ?? GetDec(vm, "forward_pe");
            if (stats.TryGetProperty("financials", out var fin))
                eps = GetDec(fin, "diluted_eps_ttm") ?? GetDec(fin, "basic_eps_ttm");
            if (stats.TryGetProperty("dividends_and_splits", out var ds))
                divYield = GetDec(ds, "forward_annual_dividend_yield") ?? GetDec(ds, "trailing_annual_dividend_yield");
            if (stats.TryGetProperty("stock_price_summary", out var ps))
            {
                beta = GetDec(ps, "beta");
                high52 = GetDec(ps, "fifty_two_week_high");
                low52 = GetDec(ps, "fifty_two_week_low");
                if (ps.TryGetProperty("fifty_two_week", out var ftw))
                {
                    high52 ??= GetDec(ftw, "high");
                    low52 ??= GetDec(ftw, "low");
                }
            }
            beta ??= GetDec(stats, "beta");

            return new TdCompanyStatistics(symbol, marketCap, pe, eps, divYield, beta, high52, low52);
        }
        catch (TwelveDataException ex)
        {
            _logger.LogWarning(ex, "Statistics fetch failed for {Symbol}", symbol);
            return null;
        }
    }

    public async Task<string?> GetLogoUrlAsync(string symbol, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        var url = $"/logo?symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";
        try
        {
            using var doc = await SendAsJsonDocAsync(url, "/logo", symbol, cancellationToken);
            var raw = doc.RootElement.Deserialize<TdLogoRaw>(JsonOptions);
            if (raw is null || string.Equals(raw.Status, "error", StringComparison.OrdinalIgnoreCase)) return null;
            return string.IsNullOrWhiteSpace(raw.Url) ? null : raw.Url;
        }
        catch (TwelveDataException)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TdSymbolMatch>> SearchSymbolsAsync(string query, CancellationToken cancellationToken = default)
    {
        EnsureKey();
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<TdSymbolMatch>();

        var url = $"/symbol_search?symbol={Uri.EscapeDataString(query)}&apikey={_options.ApiKey}";
        using var doc = await SendAsJsonDocAsync(url, "/symbol_search", query, cancellationToken);
        var raw = doc.RootElement.Deserialize<TdSymbolSearchRaw>(JsonOptions);
        EnsureNotError(raw?.Status, raw?.Code, raw?.Message, "/symbol_search");
        if (raw?.Data is null) return Array.Empty<TdSymbolMatch>();

        return raw.Data
            .Where(d => !string.IsNullOrEmpty(d.Symbol))
            .Select(d => new TdSymbolMatch(
                Symbol: d.Symbol!,
                Name: d.InstrumentName ?? d.Symbol!,
                Exchange: d.Exchange,
                Currency: d.Currency,
                Country: d.Country,
                InstrumentType: d.InstrumentType))
            .ToList();
    }

    private async Task<JsonDocument> SendAsJsonDocAsync(string url, string endpointLabel, string? symbols, CancellationToken cancellationToken)
    {
        await _limiter.AcquireOrThrowAsync(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        int statusCode = 0;
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            statusCode = (int)response.StatusCode;

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new TwelveDataRateLimitException("Twelve Data rate limit exceeded.", url);
            }
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new TwelveDataException(
                    $"Twelve Data HTTP {(int)response.StatusCode}: {body}",
                    code: (int)response.StatusCode,
                    endpoint: url);
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(stream, default, cancellationToken);
        }
        finally
        {
            stopwatch.Stop();
            await _limiter.RecordCallAsync(endpointLabel, symbols, statusCode, stopwatch.ElapsedMilliseconds, CancellationToken.None);
        }
    }

    private void EnsureKey()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new TwelveDataException(
                "TwelveData:ApiKey is not configured. Set it via `dotnet user-secrets set TwelveData:ApiKey <value>`.");
        }
    }

    private static void EnsureNotError(string? status, int? code, string? message, string endpoint)
    {
        if (string.Equals(status, "error", StringComparison.OrdinalIgnoreCase) || code is >= 400)
        {
            if (code == 429)
            {
                throw new TwelveDataRateLimitException(message ?? "Twelve Data rate limit exceeded.", endpoint);
            }
            throw new TwelveDataException(message ?? "Twelve Data error.", code, endpoint);
        }
    }

    private static TdQuote MapQuote(TdQuoteRaw raw)
    {
        var quoteTime = ParseDateTime(raw.Datetime, raw.Timestamp) ?? DateTime.UtcNow;
        var close = ParseDecimal(raw.Close) ?? 0m;
        return new TdQuote(
            Symbol: raw.Symbol ?? string.Empty,
            Name: raw.Name ?? raw.Symbol ?? string.Empty,
            Exchange: raw.Exchange ?? string.Empty,
            Currency: raw.Currency ?? "USD",
            QuoteTime: quoteTime,
            Price: close,
            Open: ParseDecimal(raw.Open),
            High: ParseDecimal(raw.High),
            Low: ParseDecimal(raw.Low),
            PreviousClose: ParseDecimal(raw.PreviousClose),
            Change: ParseDecimal(raw.Change),
            PercentChange: ParseDecimal(raw.PercentChange),
            Volume: ParseLong(raw.Volume) ?? 0L);
    }

    private static TdOhlcPoint? MapPoint(TdTimeSeriesValue v)
    {
        var date = ParseDateTime(v.Datetime, null);
        var open = ParseDecimal(v.Open);
        var high = ParseDecimal(v.High);
        var low = ParseDecimal(v.Low);
        var close = ParseDecimal(v.Close);
        if (date is null || open is null || high is null || low is null || close is null) return null;
        return new TdOhlcPoint(date.Value, open.Value, high.Value, low.Value, close.Value, ParseLong(v.Volume) ?? 0L);
    }

    private static decimal? ParseDecimal(string? s)
        => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static long? ParseLong(string? s)
        => long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var l) ? l : null;

    private static DateTime? ParseDateTime(string? datetime, long? unix)
    {
        if (unix is > 0) return DateTimeOffset.FromUnixTimeSeconds(unix.Value).UtcDateTime;
        if (string.IsNullOrEmpty(datetime)) return null;

        // Daily bars: "2026-05-17". Intraday: "2026-05-17 15:30:00".
        if (DateTime.TryParseExact(datetime, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d))
            return d;
        if (DateTime.TryParseExact(datetime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out d))
            return d;
        if (DateTime.TryParse(datetime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out d))
            return d;
        return null;
    }
}
