using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> b)
    {
        b.ToTable("CashTransactions");
        b.HasKey(c => c.Id);
        b.Property(c => c.UserId).HasMaxLength(255).IsRequired();
        b.Property(c => c.Amount).HasColumnType("decimal(18,4)");
        b.Property(c => c.BalanceAfter).HasColumnType("decimal(18,4)");
        b.Property(c => c.ExecutedAt).HasColumnType("datetime(6)");
        b.Property(c => c.Notes).HasMaxLength(500);
        b.HasIndex(c => new { c.UserId, c.ExecutedAt });
    }
}
