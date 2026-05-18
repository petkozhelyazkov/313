using System.ComponentModel.DataAnnotations;
using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Dtos.RecurringOrders;

public record RecurringOrderDto(
    long Id,
    string Symbol,
    decimal CashAmount,
    string Frequency,
    DateTime NextRunAt,
    DateTime? LastRunAt,
    bool IsActive,
    int SuccessfulRuns,
    int FailedRuns,
    string? LastFailureReason);

public class CreateRecurringOrderRequest
{
    [Required, MaxLength(16)]
    public string Symbol { get; set; } = string.Empty;

    [Range(typeof(decimal), "1", "1000000")]
    public decimal CashAmount { get; set; }

    [Required]
    public string Frequency { get; set; } = "Weekly";

    public DateTime? StartAt { get; set; }
}

public class UpdateRecurringOrderRequest
{
    [Range(typeof(decimal), "1", "1000000")]
    public decimal? CashAmount { get; set; }

    public string? Frequency { get; set; }
    public bool? IsActive { get; set; }
}

public static class RecurringFrequencyParser
{
    public static bool TryParse(string? text, out RecurringFrequency value)
        => Enum.TryParse<RecurringFrequency>(text, ignoreCase: true, out value);
}
