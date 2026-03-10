namespace Kursa.Domain.Entities;

public class Quiz : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public string? Topic { get; set; }

    public Guid? ContentId { get; set; }

    public int QuestionCount { get; set; }

    public int DurationSeconds { get; set; }

    public ICollection<QuizQuestion> Questions { get; init; } = new List<QuizQuestion>();

    public ICollection<QuizAttempt> Attempts { get; init; } = new List<QuizAttempt>();
}
