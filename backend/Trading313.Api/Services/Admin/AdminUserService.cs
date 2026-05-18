using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;
using Trading313.Api.Dtos.Admin;
using Trading313.Api.Infrastructure.Seeding;

namespace Trading313.Api.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserService(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<AdminUserListResponse> ListAsync(string actingUserId, int page, int pageSize, string? emailFilter, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(emailFilter))
        {
            var like = $"%{emailFilter.Trim()}%";
            query = query.Where(u => EF.Functions.Like(u.Email!, like) || EF.Functions.Like(u.DisplayName, like));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserDto>(users.Count);
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            items.Add(ToDto(u, roles));
        }

        return new AdminUserListResponse(page, pageSize, total, items);
    }

    public async Task<AdminUserDto?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var u = await _userManager.FindByIdAsync(id);
        if (u is null) return null;
        var roles = await _userManager.GetRolesAsync(u);
        return ToDto(u, roles);
    }

    public async Task<AdminOpResult> SetRoleAsync(string actingUserId, string targetUserId, string role, CancellationToken cancellationToken = default)
    {
        if (role != RoleNames.User && role != RoleNames.Admin)
            return AdminOpResult.Fail(AdminFailureKind.InvalidRole, $"Role must be '{RoleNames.User}' or '{RoleNames.Admin}'.");

        var u = await _userManager.FindByIdAsync(targetUserId);
        if (u is null) return AdminOpResult.Fail(AdminFailureKind.NotFound, "User not found.");

        var currentRoles = await _userManager.GetRolesAsync(u);
        var isCurrentlyAdmin = currentRoles.Contains(RoleNames.Admin);
        var willBeAdmin = role == RoleNames.Admin;

        if (isCurrentlyAdmin && !willBeAdmin)
        {
            // Don't allow demoting the last admin.
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            if (admins.Count <= 1)
                return AdminOpResult.Fail(AdminFailureKind.LastAdminProtected,
                    "Cannot demote the last admin — promote another user first.");
        }

        // Reconcile roles: ensure exactly one of User/Admin is assigned per the target.
        if (willBeAdmin && !isCurrentlyAdmin)
        {
            await _userManager.AddToRoleAsync(u, RoleNames.Admin);
        }
        if (!willBeAdmin && isCurrentlyAdmin)
        {
            await _userManager.RemoveFromRoleAsync(u, RoleNames.Admin);
        }
        if (!currentRoles.Contains(RoleNames.User))
        {
            await _userManager.AddToRoleAsync(u, RoleNames.User);
        }

        var updatedRoles = await _userManager.GetRolesAsync(u);
        return AdminOpResult.Ok(ToDto(u, updatedRoles));
    }

    public async Task<AdminOpResult> SetActiveAsync(string actingUserId, string targetUserId, bool isActive, CancellationToken cancellationToken = default)
    {
        if (actingUserId == targetUserId && !isActive)
            return AdminOpResult.Fail(AdminFailureKind.SelfDisableNotAllowed, "You cannot disable your own account.");

        var u = await _userManager.FindByIdAsync(targetUserId);
        if (u is null) return AdminOpResult.Fail(AdminFailureKind.NotFound, "User not found.");

        // If disabling an admin, ensure they're not the last one.
        if (!isActive)
        {
            var roles = await _userManager.GetRolesAsync(u);
            if (roles.Contains(RoleNames.Admin))
            {
                var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                var stillActiveAdmins = admins.Count(a => a.IsActive && a.Id != targetUserId);
                if (stillActiveAdmins == 0)
                    return AdminOpResult.Fail(AdminFailureKind.LastAdminProtected,
                        "Cannot disable the last active admin.");
            }
        }

        u.IsActive = isActive;
        await _userManager.UpdateAsync(u);
        var updatedRoles = await _userManager.GetRolesAsync(u);
        return AdminOpResult.Ok(ToDto(u, updatedRoles));
    }

    private static AdminUserDto ToDto(ApplicationUser u, IList<string> roles) => new(
        Id: u.Id,
        Email: u.Email ?? string.Empty,
        DisplayName: u.DisplayName,
        CashBalance: u.CashBalance,
        IsActive: u.IsActive,
        CreatedAt: u.CreatedAt,
        Roles: roles.ToList());
}
