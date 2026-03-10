namespace Kursa.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public Guid ThreadId { get; set; }

    public ChatThread Thread { get; set; } = null!;

    public required string Role { get; set; }

    public required string Content { get; set; }

    public string? Citations { get; set; }

    public int TokensUsed { get; set; }
}
