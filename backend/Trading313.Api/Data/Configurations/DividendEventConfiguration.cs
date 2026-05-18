using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class DividendEventConfiguration : IEntityTypeConfiguration<DividendEvent>
{
    public void Configure(EntityTypeBuilder<DividendEvent> b)
    {
        b.ToTable("DividendEvents");
        b.HasKey(d => d.Id);
        b.Property(d => d.Symbol).HasMaxLength(16).IsRequired();
        b.Property(d => d.Amount).HasColumnType("decimal(18,6)");
        b.Property(d => d.FetchedAt).HasColumnType("datetime(6)");

        b.HasIndex(d => new { d.Symbol, d.ExDate }).IsUnique();
        b.HasIndex(d => d.ExDate);
    }
}
