using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> b)
    {
        b.ToTable("Stocks");
        b.HasKey(s => s.Id);
        b.Property(s => s.Symbol).HasMaxLength(16).IsRequired();
        b.HasIndex(s => s.Symbol).IsUnique();
        b.Property(s => s.Name).HasMaxLength(200).IsRequired();
        b.Property(s => s.Exchange).HasMaxLength(64);
        b.Property(s => s.Currency).HasMaxLength(8).IsRequired();
        b.Property(s => s.InstrumentType).HasMaxLength(64);
        b.Property(s => s.Country).HasMaxLength(64);
        b.Property(s => s.IsActive).HasDefaultValue(true);
        b.Property(s => s.CreatedAt).HasColumnType("datetime(6)");
        b.Property(s => s.LastMetadataRefreshAt).HasColumnType("datetime(6)");
        b.Property(s => s.LogoUrl).HasMaxLength(500);
        b.Property(s => s.Sector).HasMaxLength(100);
        b.Property(s => s.Industry).HasMaxLength(100);
        b.Property(s => s.Website).HasMaxLength(300);
        b.Property(s => s.Description).HasMaxLength(2000);
        b.Property(s => s.Ceo).HasMaxLength(150);
        b.Property(s => s.MarketCap).HasColumnType("decimal(28,2)");
        b.Property(s => s.PeRatio).HasColumnType("decimal(18,4)");
        b.Property(s => s.Eps).HasColumnType("decimal(18,4)");
        b.Property(s => s.DividendYield).HasColumnType("decimal(8,4)");
        b.Property(s => s.Beta).HasColumnType("decimal(8,4)");
        b.Property(s => s.FiftyTwoWeekHigh).HasColumnType("decimal(18,4)");
        b.Property(s => s.FiftyTwoWeekLow).HasColumnType("decimal(18,4)");
        b.HasIndex(s => s.Name);
        b.HasIndex(s => s.Sector);
    }
}
