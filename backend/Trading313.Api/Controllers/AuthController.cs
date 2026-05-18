using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Trading313.Api.Dtos.Auth;
using Trading313.Api.Services.Auth;

namespace Trading313.Api.Controllers;

/// <summary>
/// Registration and login. No refresh tokens in v1; access tokens are 4 hours by default.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    /// <summary>Register a new user. Successful registration creates a User-role account with $10,000 paper cash.</summary>
    /// <response code="201">Account created. Sign in via /api/auth/login to obtain a token.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.RegisterAsync(request, cancellationToken);
        if (result.Succeeded)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ValidationProblem(new ValidationProblemDetails(
            ConvertErrors(result.Errors))
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Registration failed.",
        });
    }

    /// <summary>Sign in and receive a JWT access token.</summary>
    /// <response code="200">Authentication succeeded.</response>
    /// <response code="401">Invalid credentials, account locked, or account disabled.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _auth.LoginAsync(request, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        var (title, detail) = result.FailureKind switch
        {
            AuthFailureKind.AccountLocked => ("Account locked.", "Too many failed sign-in attempts. Try again in 15 minutes."),
            AuthFailureKind.AccountDisabled => ("Account disabled.", "This account has been disabled by an administrator."),
            _ => ("Invalid credentials.", "Email or password is incorrect.")
        };

        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: title,
            detail: detail);
    }

    private static Dictionary<string, string[]> ConvertErrors(IReadOnlyDictionary<string, string[]>? errors)
        => errors is null
            ? new Dictionary<string, string[]>()
            : errors.ToDictionary(kv => kv.Key, kv => kv.Value);
}
