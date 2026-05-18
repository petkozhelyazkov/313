namespace Trading313.Api.Dtos.Portfolio;

public record TransactionDto(
    long Id,
    string Symbol,
    string Type,
    decimal Quantity,
    decimal PricePerShare,
    decimal Fees,
    decimal TotalAmount,
    DateTime ExecutedAt,
    decimal? RealizedPl,
    string? Notes,
    string? Tags = null);

public class UpdateTransactionRequest
{
    public string? Notes { get; set; }
    public string? Tags { get; set; }
}
