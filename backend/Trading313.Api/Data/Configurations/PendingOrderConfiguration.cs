using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class PendingOrderConfiguration : IEntityTypeConfiguration<PendingOrder>
{
    public void Configure(EntityTypeBuilder<PendingOrder> b)
    {
        b.ToTable("PendingOrders");
        b.HasKey(o => o.Id);
        b.Property(o => o.UserId).HasMaxLength(255).IsRequired();
        b.Property(o => o.Symbol).HasMaxLength(16).IsRequired();
        b.Property(o => o.LimitPrice).HasColumnType("decimal(18,4)");
        b.Property(o => o.Quantity).HasColumnType("decimal(18,8)");
        b.Property(o => o.FilledPrice).HasColumnType("decimal(18,4)");
        b.Property(o => o.FailureReason).HasMaxLength(500);
        b.Property(o => o.Notes).HasMaxLength(500);
        b.Property(o => o.CreatedAt).HasColumnType("datetime(6)");
        b.Property(o => o.FilledAt).HasColumnType("datetime(6)");
        b.Property(o => o.TrailingStopPercent).HasColumnType("decimal(8,4)");
        b.Property(o => o.HighWaterMark).HasColumnType("decimal(18,4)");
        b.HasIndex(o => new { o.UserId, o.Status });
        b.HasIndex(o => new { o.Status, o.Symbol });
    }
}
