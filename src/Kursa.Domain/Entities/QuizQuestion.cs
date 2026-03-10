using System.Text.Json;

namespace Kursa.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public Guid QuizId { get; set; }

    public Quiz Quiz { get; set; } = null!;

    public required string QuestionText { get; set; }

    public QuestionType Type { get; set; }

    /// <summary>
    /// JSON array of answer options (for multiple choice / true-false).
    /// </summary>
    public string? Options { get; set; }

    public required string CorrectAnswer { get; set; }

    public string? Explanation { get; set; }

    public int OrderIndex { get; set; }
}

public enum QuestionType
{
    MultipleChoice,
    TrueFalse,
    FillInTheBlank,
    OpenEnded,
}
