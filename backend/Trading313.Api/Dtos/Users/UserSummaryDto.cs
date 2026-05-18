namespace Trading313.Api.Dtos.Users;

/// <summary>
/// Shallow user representation returned by /me and login responses.
/// </summary>
public record UserSummaryDto(
    string Id,
    string Email,
    string DisplayName,
    decimal CashBalance,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles,
    bool EmailDigestEnabled = true);

public class UpdatePreferencesRequest
{
    public bool? EmailDigestEnabled { get; set; }
}
