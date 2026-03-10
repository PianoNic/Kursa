using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class ContentSummaryConfiguration : IEntityTypeConfiguration<ContentSummary>
{
    public void Configure(EntityTypeBuilder<ContentSummary> builder)
    {
        builder.HasKey(cs => cs.Id);

        builder.HasIndex(cs => new { cs.UserId, cs.ContentId }).IsUnique();

        builder.Property(cs => cs.Summary).HasMaxLength(8192);

        builder.HasOne(cs => cs.Content)
            .WithMany()
            .HasForeignKey(cs => cs.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cs => cs.User)
            .WithMany()
            .HasForeignKey(cs => cs.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
