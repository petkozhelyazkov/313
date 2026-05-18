namespace Trading313.Api.Dtos.Portfolio;

public record TradeResponse(
    TransactionDto Transaction,
    PositionDto Position,
    decimal CashBalance);
