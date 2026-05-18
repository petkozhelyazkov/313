using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data;

/// <summary>
/// EF Core context for the Trading313 API.
/// Inherits IdentityDbContext to add ASP.NET Identity tables, then layers our
/// domain entities on top.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<PriceCacheEntry> PriceCache => Set<PriceCacheEntry>();
    public DbSet<HistoricalPrice> HistoricalPrices => Set<HistoricalPrice>();
    public DbSet<ApiUsageLogEntry> ApiUsageLog => Set<ApiUsageLogEntry>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<DailyPortfolioSnapshot> DailyPortfolioSnapshots => Set<DailyPortfolioSnapshot>();
    public DbSet<EarningsEntry> EarningsEntries => Set<EarningsEntry>();
    public DbSet<PendingOrder> PendingOrders => Set<PendingOrder>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<DividendEvent> DividendEvents => Set<DividendEvent>();
    public DbSet<RecurringOrder> RecurringOrders => Set<RecurringOrder>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<StockSplit> StockSplits => Set<StockSplit>();
    public DbSet<AnalystRating> AnalystRatings => Set<AnalystRating>();
    public DbSet<InsiderTrade> InsiderTrades => Set<InsiderTrade>();
    public DbSet<EmailDigest> EmailDigests => Set<EmailDigest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.CashBalance)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(0m);

            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                .HasColumnType("datetime(6)");

            entity.Property(u => u.EmailDigestEnabled)
                .HasDefaultValue(true);
        });

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
