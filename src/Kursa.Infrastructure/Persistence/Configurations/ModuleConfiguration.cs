using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasIndex(m => m.MoodleModuleId);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(512);
        builder.Property(m => m.Description).HasMaxLength(4096);

        builder.HasMany(m => m.Contents)
            .WithOne(c => c.Module)
            .HasForeignKey(c => c.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
