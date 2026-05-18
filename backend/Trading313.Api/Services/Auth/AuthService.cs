using Microsoft.AspNetCore.Identity;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Auth;
using Trading313.Api.Dtos.Users;
using Trading313.Api.Infrastructure.Auth;
using Trading313.Api.Infrastructure.Seeding;

namespace Trading313.Api.Services.Auth;

public class AuthService : IAuthService
{
    private const decimal StartingCashBalance = 10_000m;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtTokenService _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtTokenService jwt,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return AuthResult<RegisterResponse>.Fail(
                AuthFailureKind.ValidationFailed,
                new Dictionary<string, string[]>
                {
                    ["email"] = new[] { "An account with that email already exists." }
                });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName.Trim(),
            CashBalance = StartingCashBalance,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            return AuthResult<RegisterResponse>.FromIdentityErrors(create.Errors);
        }

        var addRole = await _userManager.AddToRoleAsync(user, RoleNames.User);
        if (!addRole.Succeeded)
        {
            _logger.LogError("Created user {Email} but failed to add User role: {Errors}",
                user.Email, string.Join("; ", addRole.Errors.Select(e => e.Description)));
            return AuthResult<RegisterResponse>.FromIdentityErrors(addRole.Errors);
        }

        return AuthResult<RegisterResponse>.Ok(new RegisterResponse(user.Id, user.Email!, user.DisplayName));
    }

    public async Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthResult<AuthResponse>.Fail(AuthFailureKind.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return AuthResult<AuthResponse>.Fail(AuthFailureKind.AccountDisabled);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return AuthResult<AuthResponse>.Fail(AuthFailureKind.AccountLocked);
        }
        if (!result.Succeeded)
        {
            return AuthResult<AuthResponse>.Fail(AuthFailureKind.InvalidCredentials);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _jwt.GenerateAccessToken(user, roles);

        var summary = new UserSummaryDto(
            Id: user.Id,
            Email: user.Email ?? string.Empty,
            DisplayName: user.DisplayName,
            CashBalance: user.CashBalance,
            IsActive: user.IsActive,
            CreatedAt: user.CreatedAt,
            Roles: roles.ToList(),
            EmailDigestEnabled: user.EmailDigestEnabled);

        return AuthResult<AuthResponse>.Ok(new AuthResponse(token, expiresAt, summary));
    }
}
