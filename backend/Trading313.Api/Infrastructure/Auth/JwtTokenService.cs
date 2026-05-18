using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Infrastructure.Auth;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Key) || _options.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be configured with at least 32 characters. Use `dotnet user-secrets set Jwt:Key <value>` for dev.");
        }
        if (string.IsNullOrWhiteSpace(_options.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }
        if (string.IsNullOrWhiteSpace(_options.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }
    }

    public (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.NameIdentifier, user.Id),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expiresAt);
    }
}
