namespace Trading313.Api.Dtos.Dividends;

public record DividendHistoryItem(
    string Symbol,
    DateOnly ExDate,
    DateOnly? PaymentDate,
    decimal AmountPerShare);

public record UpcomingDividendItem(
    string Symbol,
    string? Name,
    string? LogoUrl,
    DateOnly ExDate,
    DateOnly? PaymentDate,
    decimal AmountPerShare,
    decimal CurrentQuantity,
    decimal EstimatedPayment);

public record ReceivedDividendItem(
    string Symbol,
    DateOnly ExDate,
    DateOnly? PaymentDate,
    decimal AmountPerShare,
    decimal QuantityHeld,
    decimal TotalReceived);

public record DividendSummary(
    decimal LifetimeReceived,
    decimal Upcoming30Days,
    decimal Last12Months,
    int UniqueSymbols);
