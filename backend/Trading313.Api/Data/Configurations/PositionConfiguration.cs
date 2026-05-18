using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> b)
    {
        b.ToTable("Positions");
        b.HasKey(p => p.Id);
        b.Property(p => p.UserId).HasMaxLength(255).IsRequired();
        b.Property(p => p.Symbol).HasMaxLength(16).IsRequired();
        b.Property(p => p.Quantity).HasColumnType("decimal(18,8)");
        b.Property(p => p.AverageCost).HasColumnType("decimal(18,4)");
        b.Property(p => p.TotalInvested).HasColumnType("decimal(18,4)");
        b.Property(p => p.RealizedPlLifetime).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        b.Property(p => p.FirstPurchasedAt).HasColumnType("datetime(6)");
        b.Property(p => p.LastTransactionAt).HasColumnType("datetime(6)");
        b.Property(p => p.Notes).HasMaxLength(1000);
        b.Property(p => p.Tags).HasMaxLength(200);

        b.HasIndex(p => new { p.UserId, p.Symbol }).IsUnique();
    }
}
