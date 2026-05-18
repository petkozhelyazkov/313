namespace Trading313.Api.Infrastructure.MarketData.Models;

// ─── Typed domain records (used by services) ─────────────────────────────────

/// <summary>Latest snapshot quote for a single symbol.</summary>
public record TdQuote(
    string Symbol,
    string Name,
    string Exchange,
    string Currency,
    DateTime QuoteTime,
    decimal Price,
    decimal? Open,
    decimal? High,
    decimal? Low,
    decimal? PreviousClose,
    decimal? Change,
    decimal? PercentChange,
    long Volume);

/// <summary>A row from a symbol search.</summary>
public record TdSymbolMatch(
    string Symbol,
    string Name,
    string? Exchange,
    string? Currency,
    string? Country,
    string? InstrumentType);

/// <summary>OHLC time series result.</summary>
public record TdTimeSeries(
    string Symbol,
    string Interval,
    IReadOnlyList<TdOhlcPoint> Points);

public record TdOhlcPoint(
    DateTime Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);

// ─── Raw JSON DTOs that map directly to Twelve Data responses ────────────────
// All numeric fields come back as strings — we parse in the client.

internal class TdQuoteRaw
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public string? Exchange { get; set; }
    public string? Currency { get; set; }
    public string? Datetime { get; set; }
    public long? Timestamp { get; set; }
    public string? Open { get; set; }
    public string? High { get; set; }
    public string? Low { get; set; }
    public string? Close { get; set; }
    public string? Volume { get; set; }
    public string? PreviousClose { get; set; }
    public string? Change { get; set; }
    public string? PercentChange { get; set; }

    // Error envelope (when status="error")
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdSymbolSearchRaw
{
    public List<TdSymbolSearchItem>? Data { get; set; }
    public string? Status { get; set; }

    public int? Code { get; set; }
    public string? Message { get; set; }
}

internal class TdSymbolSearchItem
{
    public string? Symbol { get; set; }
    public string? InstrumentName { get; set; }
    public string? Exchange { get; set; }
    public string? Currency { get; set; }
    public string? Country { get; set; }
    public string? InstrumentType { get; set; }
}

internal class TdTimeSeriesRaw
{
    public TdTimeSeriesMeta? Meta { get; set; }
    public List<TdTimeSeriesValue>? Values { get; set; }
    public string? Status { get; set; }

    public int? Code { get; set; }
    public string? Message { get; set; }
}

internal class TdLogoRaw
{
    public string? Url { get; set; }
    public string? LogoBase { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdEarningsRaw
{
    public List<TdEarningsItem>? Earnings { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdEarningsItem
{
    public string? Date { get; set; }
    public string? Time { get; set; }
    public decimal? EpsEstimate { get; set; }
    public decimal? EpsActual { get; set; }
    public decimal? Difference { get; set; }
    public decimal? SurprisePrc { get; set; }
}

internal class TdMarketMoversRaw
{
    public List<TdMoverItem>? Values { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdMoverItem
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public string? Exchange { get; set; }
    public decimal? Last { get; set; }
    public decimal? Change { get; set; }
    public decimal? PercentChange { get; set; }
    public long? Volume { get; set; }
}

internal class TdProfileRaw
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
    public string? Exchange { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public int? Employees { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }
    public string? Ceo { get; set; }
    public string? Country { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdDividendsRaw
{
    public List<TdDividendItem>? Dividends { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdDividendItem
{
    public string? ExDate { get; set; }
    public string? PaymentDate { get; set; }
    public decimal? Amount { get; set; }
}

internal class TdSplitsRaw
{
    public List<TdSplitItem>? Splits { get; set; }
    public int? Code { get; set; }
    public string? Message { get; set; }
    public string? Status { get; set; }
}

internal class TdSplitItem
{
    public string? Date { get; set; }
    public decimal? FromFactor { get; set; }
    public decimal? ToFactor { get; set; }
}

internal class TdTimeSeriesMeta
{
    public string? Symbol { get; set; }
    public string? Interval { get; set; }
    public string? Currency { get; set; }
    public string? Exchange { get; set; }
    public string? Type { get; set; }
}

internal class TdTimeSeriesValue
{
    public string? Datetime { get; set; }
    public string? Open { get; set; }
    public string? High { get; set; }
    public string? Low { get; set; }
    public string? Close { get; set; }
    public string? Volume { get; set; }
}
