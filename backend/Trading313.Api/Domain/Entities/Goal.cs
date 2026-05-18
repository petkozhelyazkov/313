using Trading313.Api.Domain.Enums;

namespace Trading313.Api.Domain.Entities;

/// <summary>
/// A user-defined financial target. Progress is recomputed on read from current
/// portfolio state — there's no separate "actual" column, just the target and
/// metadata.
/// </summary>
public class Goal
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public GoalType Type { get; set; }
    public decimal TargetAmount { get; set; }
    public string? Title { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}
