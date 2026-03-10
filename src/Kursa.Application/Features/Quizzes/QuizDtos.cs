using Kursa.Domain.Entities;

namespace Kursa.Application.Features.Quizzes;

public sealed record QuizDto(
    Guid Id,
    string Title,
    string? Topic,
    int QuestionCount,
    int DurationSeconds,
    int AttemptCount,
    int? BestScore,
    DateTime CreatedAt);

public sealed record QuizDetailDto(
    Guid Id,
    string Title,
    string? Topic,
    int DurationSeconds,
    IReadOnlyList<QuizQuestionDto> Questions);

public sealed record QuizQuestionDto(
    Guid Id,
    string QuestionText,
    QuestionType Type,
    IReadOnlyList<string>? Options,
    int OrderIndex);

public sealed record QuizQuestionWithAnswerDto(
    Guid Id,
    string QuestionText,
    QuestionType Type,
    IReadOnlyList<string>? Options,
    string CorrectAnswer,
    string? Explanation,
    int OrderIndex);

public sealed record QuizAttemptDto(
    Guid Id,
    Guid QuizId,
    int Score,
    int TotalQuestions,
    int DurationSeconds,
    DateTime CompletedAt);

public sealed record QuizAttemptDetailDto(
    Guid Id,
    Guid QuizId,
    int Score,
    int TotalQuestions,
    int DurationSeconds,
    DateTime CompletedAt,
    IReadOnlyList<QuizAnswerDto> Answers);

public sealed record QuizAnswerDto(
    Guid QuestionId,
    string QuestionText,
    QuestionType Type,
    string UserAnswer,
    string CorrectAnswer,
    string? Explanation,
    bool IsCorrect);
