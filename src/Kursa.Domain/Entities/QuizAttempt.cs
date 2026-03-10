namespace Kursa.Domain.Entities;

public class QuizAttempt : BaseEntity
{
    public Guid QuizId { get; set; }

    public Quiz Quiz { get; set; } = null!;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public int Score { get; set; }

    public int TotalQuestions { get; set; }

    public int DurationSeconds { get; set; }

    public DateTime? CompletedAt { get; set; }

    public ICollection<QuizAnswer> Answers { get; init; } = new List<QuizAnswer>();
}
