namespace Trading313.Api.Dtos.Users;

public record AchievementDto(
    string Code,
    string Name,
    string Description,
    string Icon,
    bool Earned,
    DateTime? EarnedAt,
    int? Progress,
    int? Target);
