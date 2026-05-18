using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class ApiUsageLogEntryConfiguration : IEntityTypeConfiguration<ApiUsageLogEntry>
{
    public void Configure(EntityTypeBuilder<ApiUsageLogEntry> b)
    {
        b.ToTable("ApiUsageLog");
        b.HasKey(e => e.Id);
        b.Property(e => e.Endpoint).HasMaxLength(64).IsRequired();
        b.Property(e => e.Symbols).HasMaxLength(500);
        b.Property(e => e.RequestedAt).HasColumnType("datetime(6)");
        b.HasIndex(e => e.RequestedAt);
    }
}
