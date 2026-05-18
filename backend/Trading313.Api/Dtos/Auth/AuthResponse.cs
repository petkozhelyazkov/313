using Trading313.Api.Dtos.Users;

namespace Trading313.Api.Dtos.Auth;

public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserSummaryDto User);
