using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Users;
using Trading313.Api.Services.Users;

namespace Trading313.Api.Controllers;

/// <summary>
/// Current-user profile + password endpoints. All require authentication.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IAchievementService _achievements;
    private readonly AppDbContext _db;

    public UsersController(IUserService users, IAchievementService achievements, AppDbContext db)
    {
        _users = users;
        _achievements = achievements;
        _db = db;
    }

    /// <summary>Update user-level preferences (currently: weekly digest opt-in).</summary>
    [HttpPut("me/preferences")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _db.Set<ApplicationUser>().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null) return NotFound();
        if (request.EmailDigestEnabled.HasValue) user.EmailDigestEnabled = request.EmailDigestEnabled.Value;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Returns the authenticated user's achievement badges with progress.</summary>
    [HttpGet("me/achievements")]
    [ProducesResponseType(typeof(IEnumerable<AchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAchievements(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var list = await _achievements.GetAchievementsAsync(userId, cancellationToken);
        return Ok(list);
    }

    /// <summary>Returns the authenticated user's profile, roles, and cash balance.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await _users.GetCurrentUserAsync(User, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    /// <summary>Updates the display name on the current user's profile.</summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var result = await _users.UpdateProfileAsync(User, request, cancellationToken);

        return result.Outcome switch
        {
            UserOperationOutcome.Success => NoContent(),
            UserOperationOutcome.NotFound => Unauthorized(),
            _ => ValidationProblem(new ValidationProblemDetails(ToErrors(result.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Profile update failed.",
            }),
        };
    }

    /// <summary>Changes the current user's password. Requires the current password.</summary>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _users.ChangePasswordAsync(User, request, cancellationToken);

        return result.Outcome switch
        {
            UserOperationOutcome.Success => NoContent(),
            UserOperationOutcome.NotFound => Unauthorized(),
            _ => ValidationProblem(new ValidationProblemDetails(ToErrors(result.Errors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Password change failed.",
            }),
        };
    }

    private static Dictionary<string, string[]> ToErrors(IReadOnlyDictionary<string, string[]>? errors)
        => errors is null
            ? new Dictionary<string, string[]>()
            : errors.ToDictionary(kv => kv.Key, kv => kv.Value);
}
