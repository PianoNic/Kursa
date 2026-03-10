using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class RecordingConfiguration : IEntityTypeConfiguration<Recording>
{
    public void Configure(EntityTypeBuilder<Recording> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.FileName).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(r => r.ObjectKey).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.ErrorMessage).HasMaxLength(2000);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => new { r.UserId, r.Status });

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Course)
            .WithMany()
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
