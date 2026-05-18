using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Users;

namespace Trading313.Api.Services.Users;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserSummaryDto?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return ToSummary(user, roles);
    }

    public async Task<UserOperationResult> UpdateProfileAsync(ClaimsPrincipal principal, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null) return new UserOperationResult(UserOperationOutcome.NotFound);

        user.DisplayName = request.DisplayName.Trim();
        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded
            ? new UserOperationResult(UserOperationOutcome.Success)
            : new UserOperationResult(UserOperationOutcome.ValidationFailed, ToErrorDict(result.Errors));
    }

    public async Task<UserOperationResult> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user is null) return new UserOperationResult(UserOperationOutcome.NotFound);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        return result.Succeeded
            ? new UserOperationResult(UserOperationOutcome.Success)
            : new UserOperationResult(UserOperationOutcome.ValidationFailed, ToErrorDict(result.Errors));
    }

    private static UserSummaryDto ToSummary(ApplicationUser user, IList<string> roles) => new(
        Id: user.Id,
        Email: user.Email ?? string.Empty,
        DisplayName: user.DisplayName,
        CashBalance: user.CashBalance,
        IsActive: user.IsActive,
        CreatedAt: user.CreatedAt,
        Roles: roles.ToList(),
        EmailDigestEnabled: user.EmailDigestEnabled);

    private static Dictionary<string, string[]> ToErrorDict(IEnumerable<IdentityError> errors) =>
        errors
            .GroupBy(e => string.IsNullOrEmpty(e.Code) ? "general" : e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
}
