using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.MoodleCourseId);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(512);
        builder.Property(c => c.ShortName).HasMaxLength(128);
        builder.Property(c => c.Summary).HasMaxLength(4096);
        builder.Property(c => c.ImageUrl).HasMaxLength(1024);

        builder.HasMany(c => c.Modules)
            .WithOne(m => m.Course)
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
