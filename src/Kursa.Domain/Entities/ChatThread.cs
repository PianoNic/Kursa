namespace Kursa.Domain.Entities;

public class ChatThread : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public ICollection<ChatMessage> Messages { get; init; } = new List<ChatMessage>();
}
