namespace Trading313.Api.Infrastructure.Seeding;

/// <summary>
/// Single source of truth for role names. Use these constants instead of magic strings
/// in <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute"/> values.
/// </summary>
public static class RoleNames
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = new[] { User, Admin };
}
