using Microsoft.AspNetCore.Identity;
using Trading313.Api.Dtos.Auth;

namespace Trading313.Api.Services.Auth;

public interface IAuthService
{
    Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public enum AuthFailureKind
{
    None,
    ValidationFailed,
    InvalidCredentials,
    AccountLocked,
    AccountDisabled,
}

public class AuthResult<T> where T : class
{
    public bool Succeeded { get; private init; }
    public T? Value { get; private init; }
    public AuthFailureKind FailureKind { get; private init; }
    public IReadOnlyDictionary<string, string[]>? Errors { get; private init; }

    public static AuthResult<T> Ok(T value) => new() { Succeeded = true, Value = value };

    public static AuthResult<T> Fail(AuthFailureKind kind, IReadOnlyDictionary<string, string[]>? errors = null)
        => new() { Succeeded = false, FailureKind = kind, Errors = errors };

    public static AuthResult<T> FromIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var dict = errors
            .GroupBy(e => string.IsNullOrEmpty(e.Code) ? "general" : e.Code)
            .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
        return new AuthResult<T>
        {
            Succeeded = false,
            FailureKind = AuthFailureKind.ValidationFailed,
            Errors = dict,
        };
    }
}
