using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class PinnedContentConfiguration : IEntityTypeConfiguration<PinnedContent>
{
    public void Configure(EntityTypeBuilder<PinnedContent> builder)
    {
        builder.HasKey(pc => pc.Id);

        builder.HasIndex(pc => new { pc.UserId, pc.ContentId }).IsUnique();

        builder.Property(pc => pc.Notes).HasMaxLength(4096);

        builder.HasOne(pc => pc.User)
            .WithMany(u => u.PinnedContents)
            .HasForeignKey(pc => pc.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Content)
            .WithMany(c => c.PinnedByUsers)
            .HasForeignKey(pc => pc.ContentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
