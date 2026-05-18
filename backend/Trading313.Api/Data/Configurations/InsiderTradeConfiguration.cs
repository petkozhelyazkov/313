using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class InsiderTradeConfiguration : IEntityTypeConfiguration<InsiderTrade>
{
    public void Configure(EntityTypeBuilder<InsiderTrade> b)
    {
        b.ToTable("InsiderTrades");
        b.HasKey(t => t.Id);
        b.Property(t => t.Symbol).HasMaxLength(16).IsRequired();
        b.Property(t => t.PersonName).HasMaxLength(200).IsRequired();
        b.Property(t => t.Role).HasMaxLength(100);
        b.Property(t => t.TransactionType).HasMaxLength(20).IsRequired();
        b.Property(t => t.TransactionDate).HasColumnType("datetime(6)");
        b.Property(t => t.FetchedAt).HasColumnType("datetime(6)");
        b.Property(t => t.Shares).HasColumnType("decimal(18,4)");
        b.Property(t => t.PricePerShare).HasColumnType("decimal(18,4)");
        b.Property(t => t.Value).HasColumnType("decimal(18,2)");
        b.HasIndex(t => new { t.Symbol, t.TransactionDate });
    }
}
