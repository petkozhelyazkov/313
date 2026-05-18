using System.Security.Claims;
using Trading313.Api.Dtos.Users;

namespace Trading313.Api.Services.Users;

public interface IUserService
{
    Task<UserSummaryDto?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<UserOperationResult> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserOperationResult> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public enum UserOperationOutcome
{
    Success,
    NotFound,
    ValidationFailed,
}

public record UserOperationResult(
    UserOperationOutcome Outcome,
    IReadOnlyDictionary<string, string[]>? Errors = null);
