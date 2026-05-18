using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> b)
    {
        b.ToTable("WatchlistItems");
        b.HasKey(w => w.Id);
        b.Property(w => w.UserId).HasMaxLength(255).IsRequired();
        b.Property(w => w.Symbol).HasMaxLength(16).IsRequired();
        b.Property(w => w.ListName).HasMaxLength(50).IsRequired().HasDefaultValue("Default");
        b.Property(w => w.AddedAt).HasColumnType("datetime(6)");
        b.Property(w => w.Notes).HasMaxLength(500);

        b.HasIndex(w => new { w.UserId, w.ListName, w.Symbol }).IsUnique();
        b.HasIndex(w => new { w.UserId, w.ListName });
        b.HasIndex(w => w.UserId);
    }
}
