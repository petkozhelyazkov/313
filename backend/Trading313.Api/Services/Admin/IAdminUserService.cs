using Trading313.Api.Dtos.Admin;

namespace Trading313.Api.Services.Admin;

public interface IAdminUserService
{
    Task<AdminUserListResponse> ListAsync(string actingUserId, int page, int pageSize, string? emailFilter, CancellationToken cancellationToken = default);
    Task<AdminUserDto?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<AdminOpResult> SetRoleAsync(string actingUserId, string targetUserId, string role, CancellationToken cancellationToken = default);
    Task<AdminOpResult> SetActiveAsync(string actingUserId, string targetUserId, bool isActive, CancellationToken cancellationToken = default);
}

public enum AdminFailureKind
{
    None,
    NotFound,
    InvalidRole,
    SelfDisableNotAllowed,
    LastAdminProtected,
}

public class AdminOpResult
{
    public bool Succeeded { get; private init; }
    public AdminFailureKind FailureKind { get; private init; }
    public string? ErrorMessage { get; private init; }
    public AdminUserDto? Value { get; private init; }

    public static AdminOpResult Ok(AdminUserDto value) => new() { Succeeded = true, Value = value };
    public static AdminOpResult Fail(AdminFailureKind kind, string message)
        => new() { Succeeded = false, FailureKind = kind, ErrorMessage = message };
}
