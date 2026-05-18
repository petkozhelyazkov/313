namespace Trading313.Api.Infrastructure.Auth;

/// <summary>
/// Strongly-typed JWT configuration bound from the "Jwt" section of appsettings.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 240;
}
