using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kursa.Infrastructure.Persistence.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role).HasMaxLength(32);
        builder.Property(m => m.Content).HasMaxLength(16384);
        builder.Property(m => m.Citations).HasMaxLength(8192);

        builder.HasIndex(m => m.ThreadId);

        builder.HasOne(m => m.Thread)
            .WithMany(t => t.Messages)
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
