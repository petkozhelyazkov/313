using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Trading313.Api.Data;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Infrastructure.Seeding;

/// <summary>
/// Applies pending migrations, then ensures core roles exist and (optionally) seeds
/// a default admin user.
/// </summary>
public class IdentitySeeder
{
    private readonly IServiceProvider _services;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(IServiceProvider services, ILogger<IdentitySeeder> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var seedOptions = sp.GetRequiredService<IOptions<SeedOptions>>().Value;

        _logger.LogInformation("Applying pending migrations…");
        await db.Database.MigrateAsync(cancellationToken);

        foreach (var role in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
                }
                _logger.LogInformation("Created role {Role}", role);
            }
        }

        if (!seedOptions.Enabled)
        {
            _logger.LogInformation("Seed:Enabled is false — skipping default admin creation.");
            return;
        }

        if (string.IsNullOrWhiteSpace(seedOptions.DefaultAdminEmail) ||
            string.IsNullOrWhiteSpace(seedOptions.DefaultAdminPassword))
        {
            _logger.LogWarning("Seed:Enabled=true but DefaultAdminEmail/Password missing. No default admin created.");
            return;
        }

        var anyAdmin = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
        if (anyAdmin.Count > 0)
        {
            _logger.LogInformation("At least one admin user already exists — skipping default admin creation.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = seedOptions.DefaultAdminEmail,
            Email = seedOptions.DefaultAdminEmail,
            EmailConfirmed = true,
            DisplayName = "Administrator",
            CashBalance = 10_000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var create = await userManager.CreateAsync(admin, seedOptions.DefaultAdminPassword);
        if (!create.Succeeded)
        {
            var errors = string.Join("; ", create.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create default admin: {errors}");
        }

        var addRole = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        if (!addRole.Succeeded)
        {
            var errors = string.Join("; ", addRole.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign Admin role to default admin: {errors}");
        }

        _logger.LogWarning(
            "Default admin created: {Email}. This is a dev convenience — disable in production by setting Seed:Enabled to false.",
            seedOptions.DefaultAdminEmail);
    }
}
