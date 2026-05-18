using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class AnalystRatingConfiguration : IEntityTypeConfiguration<AnalystRating>
{
    public void Configure(EntityTypeBuilder<AnalystRating> b)
    {
        b.ToTable("AnalystRatings");
        b.HasKey(a => a.Symbol);
        b.Property(a => a.Symbol).HasMaxLength(16);
        b.Property(a => a.FetchedAt).HasColumnType("datetime(6)");
        b.Property(a => a.RecommendationMean).HasColumnType("decimal(4,2)");
        b.Property(a => a.TargetLow).HasColumnType("decimal(18,4)");
        b.Property(a => a.TargetMean).HasColumnType("decimal(18,4)");
        b.Property(a => a.TargetHigh).HasColumnType("decimal(18,4)");
    }
}
