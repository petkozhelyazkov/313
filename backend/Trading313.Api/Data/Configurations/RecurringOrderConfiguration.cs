using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class RecurringOrderConfiguration : IEntityTypeConfiguration<RecurringOrder>
{
    public void Configure(EntityTypeBuilder<RecurringOrder> b)
    {
        b.ToTable("RecurringOrders");
        b.HasKey(r => r.Id);
        b.Property(r => r.UserId).HasMaxLength(255).IsRequired();
        b.Property(r => r.Symbol).HasMaxLength(16).IsRequired();
        b.Property(r => r.CashAmount).HasColumnType("decimal(18,4)");
        b.Property(r => r.Frequency).HasConversion<int>();
        b.Property(r => r.NextRunAt).HasColumnType("datetime(6)");
        b.Property(r => r.LastRunAt).HasColumnType("datetime(6)");
        b.Property(r => r.CreatedAt).HasColumnType("datetime(6)");
        b.Property(r => r.LastFailureReason).HasMaxLength(500);

        b.HasIndex(r => new { r.UserId, r.IsActive });
        b.HasIndex(r => new { r.IsActive, r.NextRunAt });
    }
}
