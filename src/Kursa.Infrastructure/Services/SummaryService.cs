using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Infrastructure.Services;

public sealed class SummaryService(
    IAppDbContext dbContext,
    ILlmProvider llmProvider,
    ILogger<SummaryService> logger) : ISummaryService
{
    private const string SystemPrompt =
        """
        You are a study assistant. Generate a concise, well-structured summary of the provided content.
        Focus on key concepts, important details, and main takeaways.
        Use bullet points and clear headings where appropriate.
        Keep the summary between 200 and 500 words.
        Write in the same language as the source content.
        """;

    public async Task<string> GenerateSummaryAsync(Guid contentId, Guid userId, CancellationToken cancellationToken = default)
    {
        Content? content = await dbContext.Contents
            .Include(c => c.Module)
            .FirstOrDefaultAsync(c => c.Id == contentId, cancellationToken);

        if (content is null)
        {
            logger.LogWarning("Content {ContentId} not found for summarization", contentId);
            return string.Empty;
        }

        string text = BuildContentText(content);
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogInformation("No text available for summarization of content {ContentId}", contentId);
            return string.Empty;
        }

        logger.LogInformation("Generating summary for content {ContentId} ({Title})", contentId, content.Title);

        LlmChatResponse response = await llmProvider.ChatAsync(new LlmChatRequest
        {
            SystemPrompt = SystemPrompt,
            Messages = [LlmMessage.User($"Please summarize the following content:\n\n{text}")],
            Temperature = 0.3f,
            MaxTokens = 1024,
        }, cancellationToken);

        // Upsert summary in database
        ContentSummary? existing = await dbContext.ContentSummaries
            .FirstOrDefaultAsync(s => s.ContentId == contentId && s.UserId == userId, cancellationToken);

        if (existing is not null)
        {
            existing.Summary = response.Content;
            existing.TokensUsed = response.TotalTokens;
        }
        else
        {
            dbContext.ContentSummaries.Add(new ContentSummary
            {
                ContentId = contentId,
                UserId = userId,
                Summary = response.Content,
                TokensUsed = response.TotalTokens,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Summary generated for content {ContentId}, {Tokens} tokens used", contentId, response.TotalTokens);

        return response.Content;
    }

    private static string BuildContentText(Content content)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Title))
            parts.Add($"Title: {content.Title}");

        if (!string.IsNullOrWhiteSpace(content.Description))
            parts.Add(content.Description);

        return string.Join("\n\n", parts);
    }
}
