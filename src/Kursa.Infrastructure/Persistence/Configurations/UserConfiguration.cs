using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.ExternalId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.ExternalId).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.DisplayName).HasMaxLength(256);
        builder.Property(u => u.MoodleToken).HasMaxLength(512);
        builder.Property(u => u.MoodleUrl).HasMaxLength(512);
        builder.Property(u => u.AvatarUrl).HasMaxLength(1024);

        builder.HasMany(u => u.Courses)
            .WithMany(c => c.Users)
            .UsingEntity("UserCourses");
    }
}
