using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class DailyPortfolioSnapshotConfiguration : IEntityTypeConfiguration<DailyPortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<DailyPortfolioSnapshot> b)
    {
        b.ToTable("DailyPortfolioSnapshots");
        b.HasKey(s => s.Id);
        b.Property(s => s.UserId).HasMaxLength(255).IsRequired();
        b.Property(s => s.CashBalance).HasColumnType("decimal(18,4)");
        b.Property(s => s.HoldingsValue).HasColumnType("decimal(18,4)");
        b.Property(s => s.TotalValue).HasColumnType("decimal(18,4)");
        b.Property(s => s.TotalInvestedAtSnapshot).HasColumnType("decimal(18,4)");
        b.Property(s => s.UnrealizedPl).HasColumnType("decimal(18,4)");

        b.HasIndex(s => new { s.UserId, s.SnapshotDate }).IsUnique();
    }
}
