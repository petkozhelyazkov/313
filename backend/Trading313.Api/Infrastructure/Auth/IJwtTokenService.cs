using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Infrastructure.Auth;

public interface IJwtTokenService
{
    /// <summary>
    /// Issues a signed access token for the given user + role list.
    /// </summary>
    /// <returns>The encoded JWT string and the UTC expiry timestamp.</returns>
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles);
}
