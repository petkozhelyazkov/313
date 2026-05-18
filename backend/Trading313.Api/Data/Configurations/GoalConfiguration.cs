using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> b)
    {
        b.ToTable("Goals");
        b.HasKey(g => g.Id);
        b.Property(g => g.UserId).HasMaxLength(255).IsRequired();
        b.Property(g => g.Type).HasConversion<int>();
        b.Property(g => g.TargetAmount).HasColumnType("decimal(18,4)");
        b.Property(g => g.Title).HasMaxLength(120);
        b.Property(g => g.CreatedAt).HasColumnType("datetime(6)");
        b.Property(g => g.CompletedAt).HasColumnType("datetime(6)");
        b.HasIndex(g => new { g.UserId, g.IsCompleted });
    }
}
