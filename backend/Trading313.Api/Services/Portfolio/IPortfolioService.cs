using Trading313.Api.Dtos.Portfolio;

namespace Trading313.Api.Services.Portfolio;

public interface IPortfolioService
{
    Task<TradeOutcome> BuyAsync(string userId, BuyRequest request, CancellationToken cancellationToken = default);
    Task<TradeOutcome> SellAsync(string userId, SellRequest request, CancellationToken cancellationToken = default);
}

public enum TradeFailureKind
{
    None,
    SymbolNotResolved,
    InvalidQuantity,
    InsufficientCash,
    InsufficientShares,
    PriceUnavailable,
    UserNotFound,
}

public class TradeOutcome
{
    public bool Succeeded { get; private init; }
    public TradeFailureKind FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }
    public TradeResponse? Value { get; private init; }

    public static TradeOutcome Ok(TradeResponse value)
        => new() { Succeeded = true, Value = value };

    public static TradeOutcome Fail(TradeFailureKind kind, string message)
        => new() { Succeeded = false, FailureKind = kind, ErrorMessage = message };
}
