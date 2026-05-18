using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class StockSplitConfiguration : IEntityTypeConfiguration<StockSplit>
{
    public void Configure(EntityTypeBuilder<StockSplit> b)
    {
        b.ToTable("StockSplits");
        b.HasKey(s => s.Id);
        b.Property(s => s.Symbol).HasMaxLength(16).IsRequired();
        b.Property(s => s.FromFactor).HasColumnType("decimal(18,6)");
        b.Property(s => s.ToFactor).HasColumnType("decimal(18,6)");
        b.Property(s => s.FetchedAt).HasColumnType("datetime(6)");
        b.HasIndex(s => new { s.Symbol, s.Date }).IsUnique();
    }
}
