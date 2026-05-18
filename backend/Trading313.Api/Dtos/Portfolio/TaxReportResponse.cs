namespace Trading313.Api.Dtos.Portfolio;

public record TaxSellRow(
    string Symbol,
    DateTime AcquiredAt,
    DateTime SoldAt,
    decimal Quantity,
    decimal CostBasis,
    decimal Proceeds,
    decimal Gain,
    bool IsLongTerm);

public record DividendRow(
    string Symbol,
    DateOnly ExDate,
    decimal AmountPerShare,
    decimal QuantityAtExDate,
    decimal TotalReceived);

public record TaxReportResponse(
    int Year,
    decimal ShortTermGains,
    decimal ShortTermLosses,
    decimal ShortTermNet,
    decimal LongTermGains,
    decimal LongTermLosses,
    decimal LongTermNet,
    decimal DividendsReceived,
    decimal FeesPaid,
    decimal NetTotal,
    IReadOnlyList<TaxSellRow> SellRows,
    IReadOnlyList<DividendRow> DividendRows);
