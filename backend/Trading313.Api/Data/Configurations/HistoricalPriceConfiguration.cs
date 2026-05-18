using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class HistoricalPriceConfiguration : IEntityTypeConfiguration<HistoricalPrice>
{
    public void Configure(EntityTypeBuilder<HistoricalPrice> b)
    {
        b.ToTable("HistoricalPrices");
        b.HasKey(h => h.Id);
        b.Property(h => h.Symbol).HasMaxLength(16).IsRequired();
        b.Property(h => h.Open).HasColumnType("decimal(18,4)");
        b.Property(h => h.High).HasColumnType("decimal(18,4)");
        b.Property(h => h.Low).HasColumnType("decimal(18,4)");
        b.Property(h => h.Close).HasColumnType("decimal(18,4)");
        b.HasIndex(h => new { h.Symbol, h.Date }).IsUnique();
    }
}
