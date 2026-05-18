using System.ComponentModel.DataAnnotations;

namespace Trading313.Api.Dtos.Users;

public class UpdateProfileRequest
{
    [Required, MinLength(1), MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}
