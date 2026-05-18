using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Goals;

public record GoalDto(
    long Id,
    string Type,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal ProgressPct,
    string? Title,
    DateOnly? DueDate,
    DateTime CreatedAt,
    bool IsCompleted,
    DateTime? CompletedAt);

public class CreateGoalRequest
{
    [Required]
    public string Type { get; set; } = "PortfolioValue";

    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal TargetAmount { get; set; }

    [MaxLength(120)]
    public string? Title { get; set; }

    public DateOnly? DueDate { get; set; }
}

public class UpdateGoalRequest
{
    [Range(typeof(decimal), "0.01", "1000000000")]
    public decimal? TargetAmount { get; set; }

    [MaxLength(120)]
    public string? Title { get; set; }

    public DateOnly? DueDate { get; set; }
    public bool? IsCompleted { get; set; }
}
