using Kursa.Domain.Entities;

namespace Kursa.Application.Features.StudySessions;

public sealed record StudySessionDto(
    Guid Id,
    string Title,
    StudySessionStatus Status,
    int WorkMinutes,
    int BreakMinutes,
    int CompletedPomodoros,
    int TotalDurationSeconds,
    int CardsReviewed,
    int QuizQuestionsAnswered,
    int QuizCorrectAnswers,
    string? Summary,
    DateTime CreatedAt,
    DateTime? CompletedAt);
