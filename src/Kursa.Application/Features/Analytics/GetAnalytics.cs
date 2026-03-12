using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Analytics;

public sealed record GetAnalyticsQuery : IQuery<Result<AnalyticsDto>>;

public sealed class GetAnalyticsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetAnalyticsQuery, Result<AnalyticsDto>>
{
    public async ValueTask<Result<AnalyticsDto>> Handle(GetAnalyticsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<AnalyticsDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<AnalyticsDto>.Failure("User not found.");

        Guid userId = user.Id;

        // Overview stats
        int totalSessions = await dbContext.StudySessions
            .CountAsync(s => s.UserId == userId && s.Status == StudySessionStatus.Completed, cancellationToken);

        int totalStudyTime = await dbContext.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed)
            .SumAsync(s => s.TotalDurationSeconds, cancellationToken);

        int totalPomodoros = await dbContext.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed)
            .SumAsync(s => s.CompletedPomodoros, cancellationToken);

        int totalQuizzesTaken = await dbContext.QuizAttempts
            .CountAsync(a => a.UserId == userId, cancellationToken);

        int totalCardsReviewed = await dbContext.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed)
            .SumAsync(s => s.CardsReviewed, cancellationToken);

        int totalPinned = await dbContext.PinnedContents
            .CountAsync(p => p.UserId == userId, cancellationToken);

        var overview = new OverviewStats(
            totalSessions, totalStudyTime, totalQuizzesTaken,
            totalCardsReviewed, totalPinned, totalPomodoros);

        // Recent quiz performance (last 10 attempts)
        List<QuizPerformanceDto> recentQuizzes = await dbContext.QuizAttempts
            .Where(a => a.UserId == userId && a.CompletedAt != null)
            .OrderByDescending(a => a.CompletedAt)
            .Take(10)
            .Select(a => new QuizPerformanceDto(
                a.QuizId,
                a.Quiz.Title,
                a.Score,
                a.TotalQuestions,
                a.CompletedAt ?? a.CreatedAt))
            .ToListAsync(cancellationToken);

        // Flashcard stats
        DateTime now = DateTime.UtcNow;

        int totalCards = await dbContext.Flashcards
            .CountAsync(c => c.Deck.UserId == userId, cancellationToken);

        int dueToday = await dbContext.Flashcards
            .CountAsync(c => c.Deck.UserId == userId && (c.NextReviewAt == null || c.NextReviewAt <= now), cancellationToken);

        int masteredCards = await dbContext.Flashcards
            .CountAsync(c => c.Deck.UserId == userId && c.Repetitions >= 5 && c.EaseFactor >= 2.5f, cancellationToken);

        int learningCards = totalCards - masteredCards;

        var flashcardStats = new FlashcardStatsDto(totalCards, dueToday, masteredCards, learningCards);

        // Weekly activity (last 7 days)
        DateTime weekAgo = now.Date.AddDays(-6);
        var recentSessions = await dbContext.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed && s.CompletedAt != null && s.CompletedAt >= weekAgo)
            .Select(s => new { s.CompletedAt, s.TotalDurationSeconds, s.CardsReviewed, s.QuizQuestionsAnswered })
            .ToListAsync(cancellationToken);

        List<StudyActivityDto> weeklyActivity = recentSessions
            .GroupBy(s => s.CompletedAt!.Value.Date)
            .Select(g => new StudyActivityDto(
                g.Key,
                g.Sum(s => s.TotalDurationSeconds),
                g.Sum(s => s.CardsReviewed),
                g.Sum(s => s.QuizQuestionsAnswered)))
            .OrderBy(a => a.Date)
            .ToList();

        // Fill in missing days
        var filledActivity = new List<StudyActivityDto>();
        for (int i = 0; i < 7; i++)
        {
            DateTime date = weekAgo.AddDays(i);
            StudyActivityDto? existing = weeklyActivity.FirstOrDefault(a => a.Date.Date == date.Date);
            filledActivity.Add(existing ?? new StudyActivityDto(date, 0, 0, 0));
        }

        // Study streak calculation
        int currentStreak = 0;
        int longestStreak = 0;

        List<DateTime> studyDates = await dbContext.StudySessions
            .Where(s => s.UserId == userId && s.Status == StudySessionStatus.Completed && s.CompletedAt != null)
            .Select(s => s.CompletedAt!.Value.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(cancellationToken);

        // Calculate current streak
        DateTime checkDate = now.Date;
        foreach (DateTime date in studyDates)
        {
            if (date == checkDate || date == checkDate.AddDays(-1))
            {
                currentStreak++;
                checkDate = date.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        // Calculate longest streak
        if (studyDates.Count > 0)
        {
            int tempStreak = 1;
            for (int i = 1; i < studyDates.Count; i++)
            {
                if (studyDates[i - 1].AddDays(-1) == studyDates[i])
                {
                    tempStreak++;
                }
                else
                {
                    longestStreak = Math.Max(longestStreak, tempStreak);
                    tempStreak = 1;
                }
            }
            longestStreak = Math.Max(longestStreak, tempStreak);
        }
        longestStreak = Math.Max(longestStreak, currentStreak);

        return Result<AnalyticsDto>.Success(new AnalyticsDto(
            overview, recentQuizzes, flashcardStats, filledActivity,
            currentStreak, longestStreak));
    }
}
