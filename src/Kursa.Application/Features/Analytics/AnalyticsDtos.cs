namespace Kursa.Application.Features.Analytics;

public sealed record AnalyticsDto(
    OverviewStats Overview,
    IReadOnlyList<QuizPerformanceDto> RecentQuizPerformance,
    FlashcardStatsDto FlashcardStats,
    IReadOnlyList<StudyActivityDto> WeeklyActivity,
    int CurrentStreak,
    int LongestStreak);

public sealed record OverviewStats(
    int TotalStudySessions,
    int TotalStudyTimeSeconds,
    int TotalQuizzesTaken,
    int TotalCardsReviewed,
    int TotalPinnedContents,
    int TotalPomodoros);

public sealed record QuizPerformanceDto(
    Guid QuizId,
    string QuizTitle,
    int Score,
    int TotalQuestions,
    DateTime CompletedAt);

public sealed record FlashcardStatsDto(
    int TotalCards,
    int DueToday,
    int MasteredCards,
    int LearningCards);

public sealed record StudyActivityDto(
    DateTime Date,
    int StudyTimeSeconds,
    int CardsReviewed,
    int QuizQuestions);
