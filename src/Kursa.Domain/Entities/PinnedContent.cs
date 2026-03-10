namespace Kursa.Domain.Entities;

public class PinnedContent : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ContentId { get; set; }

    public Content Content { get; set; } = null!;

    public bool IsStarred { get; set; }

    public bool IsIndexed { get; set; }

    public string? Notes { get; set; }
}
