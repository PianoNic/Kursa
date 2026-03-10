namespace Kursa.Domain.Entities;

public class QuizAnswer : BaseEntity
{
    public Guid AttemptId { get; set; }

    public QuizAttempt Attempt { get; set; } = null!;

    public Guid QuestionId { get; set; }

    public QuizQuestion Question { get; set; } = null!;

    public required string UserAnswer { get; set; }

    public bool IsCorrect { get; set; }
}
