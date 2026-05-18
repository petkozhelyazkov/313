using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Trading313.Api.Domain.Entities;

namespace Trading313.Api.Data.Configurations;

public class EmailDigestConfiguration : IEntityTypeConfiguration<EmailDigest>
{
    public void Configure(EntityTypeBuilder<EmailDigest> b)
    {
        b.ToTable("EmailDigests");
        b.HasKey(d => d.Id);
        b.Property(d => d.UserId).HasMaxLength(255).IsRequired();
        b.Property(d => d.Subject).HasMaxLength(200).IsRequired();
        b.Property(d => d.BodyHtml).HasColumnType("longtext").IsRequired();
        b.Property(d => d.BodyText).HasColumnType("longtext").IsRequired();
        b.Property(d => d.PeriodStart).HasColumnType("datetime(6)");
        b.Property(d => d.PeriodEnd).HasColumnType("datetime(6)");
        b.Property(d => d.GeneratedAt).HasColumnType("datetime(6)");
        b.Property(d => d.SentAt).HasColumnType("datetime(6)");
        b.Property(d => d.ReadAt).HasColumnType("datetime(6)");
        b.HasIndex(d => new { d.UserId, d.GeneratedAt });
    }
}
