namespace Trading313.Api.Infrastructure.Seeding;

/// <summary>
/// Configuration for startup seeding (roles + default admin user + demo data).
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Master switch — keep false in production.</summary>
    public bool Enabled { get; set; }

    public string? DefaultAdminEmail { get; set; }
    public string? DefaultAdminPassword { get; set; }
}
