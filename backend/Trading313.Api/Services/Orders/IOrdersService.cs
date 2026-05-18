using Trading313.Api.Dtos.Orders;

namespace Trading313.Api.Services.Orders;

public interface IOrdersService
{
    Task<OrderOutcome> PlaceAsync(string userId, PlaceOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderOutcome> CancelAsync(string userId, long orderId, CancellationToken cancellationToken = default);
    Task<OrderListResponse> ListAsync(string userId, CancellationToken cancellationToken = default);
}

public enum OrderFailureKind
{
    None,
    NotFound,
    SymbolNotResolved,
    InvalidQuantity,
    InvalidPrice,
    NotInPendingState,
}

public class OrderOutcome
{
    public bool Succeeded { get; private init; }
    public OrderFailureKind FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }
    public PendingOrderDto? Value { get; private init; }

    public static OrderOutcome Ok(PendingOrderDto v) => new() { Succeeded = true, Value = v };
    public static OrderOutcome Fail(OrderFailureKind kind, string msg)
        => new() { Succeeded = false, FailureKind = kind, ErrorMessage = msg };
}
