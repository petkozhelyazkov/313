using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> b)
    {
        b.ToTable("PriceAlerts");
        b.HasKey(a => a.Id);
        b.Property(a => a.UserId).HasMaxLength(255).IsRequired();
        b.Property(a => a.Symbol).HasMaxLength(16).IsRequired();
        b.Property(a => a.TriggerPrice).HasColumnType("decimal(18,4)");
        b.Property(a => a.TriggeredPrice).HasColumnType("decimal(18,4)");
        b.Property(a => a.CreatedAt).HasColumnType("datetime(6)");
        b.Property(a => a.TriggeredAt).HasColumnType("datetime(6)");
        b.Property(a => a.Notes).HasMaxLength(500);
        b.HasIndex(a => new { a.UserId, a.Status });
        b.HasIndex(a => new { a.Status, a.Symbol });
    }
}
