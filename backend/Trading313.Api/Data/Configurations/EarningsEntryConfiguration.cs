using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class EarningsEntryConfiguration : IEntityTypeConfiguration<EarningsEntry>
{
    public void Configure(EntityTypeBuilder<EarningsEntry> b)
    {
        b.ToTable("EarningsEntries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Symbol).HasMaxLength(16).IsRequired();
        b.Property(e => e.Time).HasMaxLength(16);
        b.Property(e => e.EpsEstimate).HasColumnType("decimal(10,4)");
        b.Property(e => e.EpsActual).HasColumnType("decimal(10,4)");
        b.Property(e => e.SurprisePercent).HasColumnType("decimal(10,4)");
        b.Property(e => e.FetchedAt).HasColumnType("datetime(6)");
        b.HasIndex(e => new { e.Symbol, e.ReportDate }).IsUnique();
        b.HasIndex(e => e.ReportDate);
    }
}
