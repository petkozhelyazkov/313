using System.ComponentModel.DataAnnotations;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Dtos.Portfolio;

public class CashAdjustmentRequest
{
    [Required]
    public CashTransactionType Type { get; set; }

    [Range(0.01, 1_000_000d, ErrorMessage = "Amount must be between 0.01 and 1,000,000.")]
    public decimal Amount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}

public record CashTransactionDto(
    long Id,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    DateTime ExecutedAt,
    string? Notes);

public record CashAdjustmentResponse(CashTransactionDto Transaction, decimal CashBalance);
