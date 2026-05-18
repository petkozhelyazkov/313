namespace Trading313.Api.Dtos.Admin;

public record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    decimal CashBalance,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<string> Roles);

public record AdminUserListResponse(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<AdminUserDto> Items);

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

public class SetActiveRequest
{
    public bool IsActive { get; set; }
}
