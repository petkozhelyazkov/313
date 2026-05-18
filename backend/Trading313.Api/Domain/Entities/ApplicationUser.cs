using Microsoft.AspNetCore.Identity;

namespace Trading313.Api.Domain.Entities;

/// <summary>
/// Application user. Extends IdentityUser with profile + paper-trading cash balance.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Display name shown in UI (1–100 chars).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Virtual cash balance for paper trading (USD).</summary>
    public decimal CashBalance { get; set; }

    /// <summary>If false, user cannot log in. Admins can disable accounts.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Account creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether the user receives the weekly digest. Defaults to true.</summary>
    public bool EmailDigestEnabled { get; set; } = true;
}
