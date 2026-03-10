namespace Kursa.Domain.Entities;

public class StudySession : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public StudySessionStatus Status { get; set; } = StudySessionStatus.Active;

    /// <summary>
    /// Pomodoro work duration in minutes.
    /// </summary>
    public int WorkMinutes { get; set; } = 25;

    /// <summary>
    /// Pomodoro break duration in minutes.
    /// </summary>
    public int BreakMinutes { get; set; } = 5;

    public int CompletedPomodoros { get; set; }

    public int TotalDurationSeconds { get; set; }

    public int CardsReviewed { get; set; }

    public int QuizQuestionsAnswered { get; set; }

    public int QuizCorrectAnswers { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? Summary { get; set; }
}

public enum StudySessionStatus
{
    Active,
    Completed,
    Abandoned,
}
