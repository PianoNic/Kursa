using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class TranscriptSegmentConfiguration : IEntityTypeConfiguration<TranscriptSegment>
{
    public void Configure(EntityTypeBuilder<TranscriptSegment> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Text).IsRequired();
        builder.Property(s => s.Speaker).HasMaxLength(200);

        builder.HasIndex(s => s.RecordingId);
        builder.HasIndex(s => new { s.RecordingId, s.OrderIndex });

        builder.HasOne(s => s.Recording)
            .WithMany(r => r.Segments)
            .HasForeignKey(s => s.RecordingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
