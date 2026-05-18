namespace Trading313.Api.Domain.Enums;

public enum OrderSide
{
    LimitBuy = 1,
    LimitSell = 2,
    StopLoss = 3,
    TrailingStop = 4,
}

public enum OrderStatus
{
    Pending = 1,
    Filled = 2,
    Cancelled = 3,
    Expired = 4,
    FailedExecution = 5,
}
