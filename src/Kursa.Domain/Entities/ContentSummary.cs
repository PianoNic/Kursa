namespace Kursa.Domain.Entities;

public class ContentSummary : BaseEntity
{
    public Guid ContentId { get; set; }

    public Content Content { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Summary { get; set; }

    public int TokensUsed { get; set; }
}
