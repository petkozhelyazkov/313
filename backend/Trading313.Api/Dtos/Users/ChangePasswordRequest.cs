using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Users;

public class ChangePasswordRequest
{
    [Required, MinLength(1), MaxLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string NewPassword { get; set; } = string.Empty;
}
