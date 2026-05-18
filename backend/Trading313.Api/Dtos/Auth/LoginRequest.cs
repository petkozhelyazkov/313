using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Auth;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(1), MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
