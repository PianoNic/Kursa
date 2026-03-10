using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Course> Courses { get; }

    DbSet<Module> Modules { get; }

    DbSet<Content> Contents { get; }

    DbSet<PinnedContent> PinnedContents { get; }

    DbSet<UserSettings> UserSettings { get; }

    DbSet<ContentSummary> ContentSummaries { get; }

    DbSet<ChatThread> ChatThreads { get; }

    DbSet<ChatMessage> ChatMessages { get; }

    DbSet<Quiz> Quizzes { get; }

    DbSet<QuizQuestion> QuizQuestions { get; }

    DbSet<QuizAttempt> QuizAttempts { get; }

    DbSet<QuizAnswer> QuizAnswers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
