namespace Kursa.Domain.Entities;

public class FlashcardDeck : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public string? Description { get; set; }

    public Guid? ContentId { get; set; }

    public ICollection<Flashcard> Cards { get; init; } = new List<Flashcard>();
}
