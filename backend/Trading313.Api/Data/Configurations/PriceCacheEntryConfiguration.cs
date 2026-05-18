using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class PriceCacheEntryConfiguration : IEntityTypeConfiguration<PriceCacheEntry>
{
    public void Configure(EntityTypeBuilder<PriceCacheEntry> b)
    {
        b.ToTable("PriceCache");
        b.HasKey(p => p.Symbol);
        b.Property(p => p.Symbol).HasMaxLength(16);
        b.Property(p => p.Price).HasColumnType("decimal(18,4)");
        b.Property(p => p.DayChange).HasColumnType("decimal(18,4)");
        b.Property(p => p.DayChangePct).HasColumnType("decimal(8,4)");
        b.Property(p => p.PreviousClose).HasColumnType("decimal(18,4)");
        b.Property(p => p.FetchedAt).HasColumnType("datetime(6)");
    }
}
