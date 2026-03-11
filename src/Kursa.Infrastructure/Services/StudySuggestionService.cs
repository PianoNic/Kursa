using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Suggestions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Kursa.Infrastructure.Services;

public sealed class StudySuggestionService(
    IAppDbContext dbContext,
    IChatCompletionService chatCompletionService,
    ILogger<StudySuggestionService> logger) : IStudySuggestionService
{
    public async Task<IReadOnlyList<StudySuggestionDto>> GenerateSuggestionsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        string context = await BuildStudyContextAsync(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(context))
        {
            return [
                new StudySuggestionDto
                {
                    Type = "getting-started",
                    Title = "Get started with Kursa",
                    Description = "Link your Moodle account, pin some content, and start studying!",
                    ActionUrl = "/courses",
                    Priority = "high",
                }
            ];
        }

        try
        {
            return await GenerateWithLlmAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate AI suggestions, falling back to rule-based");
            return GenerateRuleBasedSuggestions(context);
        }
    }

    private async Task<string> BuildStudyContextAsync(Guid userId, CancellationToken cancellationToken)
    {
        var parts = new List<string>();

        // Flashcards due for review
        int flashcardsDue = await dbContext.Flashcards
            .CountAsync(f => f.Deck.UserId == userId && f.NextReviewAt <= DateTime.UtcNow, cancellationToken);

        if (flashcardsDue > 0)
            parts.Add($"Flashcards due for review: {flashcardsDue}");

        // Recent quiz performance
        var recentQuizzes = await dbContext.QuizAttempts
            .Where(a => a.Quiz.UserId == userId)
            .OrderByDescending(a => a.CompletedAt)
            .Take(5)
            .Select(a => new { a.Quiz.Title, a.Score, a.TotalQuestions })
            .ToListAsync(cancellationToken);

        if (recentQuizzes.Count > 0)
        {
            double avgScore = recentQuizzes.Average(q => q.TotalQuestions > 0 ? (double)q.Score / q.TotalQuestions * 100 : 0);
            parts.Add($"Recent quiz average: {avgScore:F0}% across {recentQuizzes.Count} attempts");
            foreach (var q in recentQuizzes)
                parts.Add($"  - {q.Title}: {q.Score}/{q.TotalQuestions}");
        }

        // Study sessions this week
        DateTime weekAgo = DateTime.UtcNow.AddDays(-7);
        int sessionsThisWeek = await dbContext.StudySessions
            .CountAsync(s => s.UserId == userId && s.CreatedAt >= weekAgo, cancellationToken);

        int totalMinutes = await dbContext.StudySessions
            .Where(s => s.UserId == userId && s.CreatedAt >= weekAgo)
            .SumAsync(s => s.TotalDurationSeconds / 60, cancellationToken);

        parts.Add($"Study sessions this week: {sessionsThisWeek} ({totalMinutes} minutes)");

        // Flashcard decks
        int deckCount = await dbContext.FlashcardDecks
            .CountAsync(d => d.UserId == userId, cancellationToken);
        int totalCards = await dbContext.Flashcards
            .CountAsync(f => f.Deck.UserId == userId, cancellationToken);
        parts.Add($"Flashcard decks: {deckCount} ({totalCards} cards total)");

        // Recordings
        int recordingCount = await dbContext.Recordings
            .CountAsync(r => r.UserId == userId, cancellationToken);
        if (recordingCount > 0)
            parts.Add($"Recordings: {recordingCount}");

        // Pinned content
        int pinnedCount = await dbContext.PinnedContents
            .CountAsync(p => p.UserId == userId, cancellationToken);
        parts.Add($"Pinned content items: {pinnedCount}");

        return string.Join("\n", parts);
    }

    private async Task<IReadOnlyList<StudySuggestionDto>> GenerateWithLlmAsync(
        string context, CancellationToken cancellationToken)
    {
        string systemPrompt = """
            You are a study advisor AI. Based on the student's study data below, generate 3-5 actionable study suggestions.
            Each suggestion must be a JSON object with: type, title, description, actionUrl, priority.

            Types: "review" (flashcard review), "quiz" (take a quiz), "study" (study session), "record" (record a lecture), "explore" (explore content)
            Priorities: "high", "medium", "low"
            Action URLs: /flashcards, /quizzes, /study, /recordings, /courses, /chat, /pinned

            Return ONLY a JSON array of suggestion objects, no other text.
            Be specific and actionable. Reference actual numbers from the data.
            """;

        var history = new ChatHistory(systemPrompt);
        history.AddUserMessage(context);

#pragma warning disable SKEXP0001
        var settings = new OpenAIPromptExecutionSettings { Temperature = 0.7f, MaxTokens = 800 };
#pragma warning restore SKEXP0001

        var response = await chatCompletionService.GetChatMessageContentAsync(history, settings, cancellationToken: cancellationToken);

        // Parse JSON response
        string content = (response.Content ?? string.Empty).Trim();
        // Strip markdown code block if present
        if (content.StartsWith("```"))
        {
            int firstNewline = content.IndexOf('\n');
            int lastBackticks = content.LastIndexOf("```");
            if (firstNewline > 0 && lastBackticks > firstNewline)
                content = content[(firstNewline + 1)..lastBackticks].Trim();
        }

        var suggestions = JsonSerializer.Deserialize<List<StudySuggestionDto>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        return suggestions ?? [];
    }

    private static IReadOnlyList<StudySuggestionDto> GenerateRuleBasedSuggestions(string context)
    {
        var suggestions = new List<StudySuggestionDto>();

        if (context.Contains("Flashcards due for review:"))
        {
            suggestions.Add(new StudySuggestionDto
            {
                Type = "review",
                Title = "Review your flashcards",
                Description = "You have flashcards due for review. Keeping up with spaced repetition improves long-term retention.",
                ActionUrl = "/flashcards",
                Priority = "high",
            });
        }

        if (context.Contains("Recent quiz average:"))
        {
            suggestions.Add(new StudySuggestionDto
            {
                Type = "quiz",
                Title = "Take a practice quiz",
                Description = "Regular quizzing strengthens recall. Try generating a quiz from your pinned materials.",
                ActionUrl = "/quizzes",
                Priority = "medium",
            });
        }

        if (context.Contains("0 minutes"))
        {
            suggestions.Add(new StudySuggestionDto
            {
                Type = "study",
                Title = "Start a study session",
                Description = "You haven't studied this week. Even a short 25-minute Pomodoro session can make a difference.",
                ActionUrl = "/study",
                Priority = "high",
            });
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new StudySuggestionDto
            {
                Type = "explore",
                Title = "Explore your courses",
                Description = "Browse your course materials and pin content you want to study with AI assistance.",
                ActionUrl = "/courses",
                Priority = "medium",
            });
        }

        return suggestions;
    }
}
