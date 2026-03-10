using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Module> Modules => Set<Module>();

    public DbSet<Content> Contents => Set<Content>();

    public DbSet<PinnedContent> PinnedContents => Set<PinnedContent>();

    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<ContentSummary> ContentSummaries => Set<ContentSummary>();

    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();

    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    public DbSet<Quiz> Quizzes => Set<Quiz>();

    public DbSet<QuizQuestion> QuizQuestions => Set<QuizQuestion>();

    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();

    public DbSet<QuizAnswer> QuizAnswers => Set<QuizAnswer>();

    public DbSet<FlashcardDeck> FlashcardDecks => Set<FlashcardDeck>();

    public DbSet<Flashcard> Flashcards => Set<Flashcard>();

    public DbSet<StudySession> StudySessions => Set<StudySession>();

    public DbSet<Recording> Recordings => Set<Recording>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
