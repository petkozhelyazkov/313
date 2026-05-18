using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions");
        b.HasKey(t => t.Id);
        b.Property(t => t.UserId).HasMaxLength(255).IsRequired();
        b.Property(t => t.Symbol).HasMaxLength(16).IsRequired();
        b.Property(t => t.Type).IsRequired();
        b.Property(t => t.Quantity).HasColumnType("decimal(18,8)");
        b.Property(t => t.PricePerShare).HasColumnType("decimal(18,4)");
        b.Property(t => t.Fees).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        b.Property(t => t.TotalAmount).HasColumnType("decimal(18,4)");
        b.Property(t => t.RealizedPl).HasColumnType("decimal(18,4)");
        b.Property(t => t.Notes).HasMaxLength(500);
        b.Property(t => t.Tags).HasMaxLength(200);
        b.Property(t => t.ExecutedAt).HasColumnType("datetime(6)");

        b.HasIndex(t => new { t.UserId, t.ExecutedAt });
        b.HasIndex(t => new { t.UserId, t.Symbol });
    }
}
