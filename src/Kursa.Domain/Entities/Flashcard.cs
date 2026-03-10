namespace Kursa.Domain.Entities;

public class Flashcard : BaseEntity
{
    public Guid DeckId { get; set; }

    public FlashcardDeck Deck { get; set; } = null!;

    public required string Front { get; set; }

    public required string Back { get; set; }

    public FlashcardType Type { get; set; } = FlashcardType.Basic;

    // SM-2 algorithm fields
    public int Repetitions { get; set; }

    public float EaseFactor { get; set; } = 2.5f;

    public int IntervalDays { get; set; }

    public DateTime? NextReviewAt { get; set; }

    public DateTime? LastReviewedAt { get; set; }
}

public enum FlashcardType
{
    Basic,
    Cloze,
    Reversible,
}
