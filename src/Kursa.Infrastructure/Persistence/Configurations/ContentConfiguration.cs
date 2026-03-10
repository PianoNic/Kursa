using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class ContentConfiguration : IEntityTypeConfiguration<Content>
{
    public void Configure(EntityTypeBuilder<Content> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.MoodleContentId);

        builder.Property(c => c.Title).IsRequired().HasMaxLength(512);
        builder.Property(c => c.Description).HasMaxLength(4096);
        builder.Property(c => c.Url).HasMaxLength(2048);
        builder.Property(c => c.FilePath).HasMaxLength(1024);
    }
}
