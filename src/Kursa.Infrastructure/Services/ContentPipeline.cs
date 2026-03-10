using System.Text.RegularExpressions;
using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Infrastructure.Services;

public sealed partial class ContentPipeline(
    IAppDbContext dbContext,
    ILlmProvider llmProvider,
    IVectorStore vectorStore,
    ILogger<ContentPipeline> logger) : IContentPipeline
{
    private const string CollectionName = "content_chunks";
    private const int ChunkSize = 512;
    private const int ChunkOverlap = 64;

    public async Task IndexContentAsync(Guid contentId, Guid userId, CancellationToken cancellationToken = default)
    {
        Content? content = await dbContext.Contents
            .Include(c => c.Module)
            .FirstOrDefaultAsync(c => c.Id == contentId, cancellationToken);

        if (content is null)
        {
            logger.LogWarning("Content {ContentId} not found for indexing", contentId);
            return;
        }

        // Extract text from content
        string text = ExtractText(content);
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogInformation("No text extracted from content {ContentId} ({Title}), skipping", contentId, content.Title);
            return;
        }

        // Chunk the text
        IReadOnlyList<string> chunks = ChunkText(text, ChunkSize, ChunkOverlap);
        if (chunks.Count == 0) return;

        logger.LogInformation("Indexing content {ContentId} ({Title}): {ChunkCount} chunks", contentId, content.Title, chunks.Count);

        // Generate embedding for initial collection setup — get vector size from first embedding
        IReadOnlyList<float> firstEmbedding = await llmProvider.GenerateEmbeddingAsync(chunks[0], cancellationToken);
        await vectorStore.EnsureCollectionAsync(CollectionName, firstEmbedding.Count, cancellationToken);

        // Generate embeddings for all chunks (batch)
        IReadOnlyList<IReadOnlyList<float>> embeddings = await llmProvider.GenerateEmbeddingsAsync(
            chunks.ToList(), cancellationToken);

        // Build vector points
        var points = new List<VectorPoint>(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            points.Add(new VectorPoint
            {
                Id = Guid.NewGuid(),
                Vector = embeddings[i],
                ContentId = contentId,
                UserId = userId,
                ChunkText = chunks[i],
                ChunkIndex = i,
                ContentTitle = content.Title,
                ContentType = content.Type.ToString(),
                SourceUrl = content.Url,
            });
        }

        // Delete existing vectors for this content (re-index scenario)
        await vectorStore.DeleteByContentIdAsync(CollectionName, contentId, cancellationToken);

        // Upsert new vectors
        await vectorStore.UpsertAsync(CollectionName, points, cancellationToken);

        // Mark content as indexed
        PinnedContent? pinnedContent = await dbContext.PinnedContents
            .FirstOrDefaultAsync(p => p.ContentId == contentId && p.UserId == userId, cancellationToken);

        if (pinnedContent is not null)
        {
            pinnedContent.IsIndexed = true;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Successfully indexed content {ContentId} ({Title}) with {ChunkCount} chunks", contentId, content.Title, chunks.Count);
    }

    public async Task RemoveContentIndexAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        await vectorStore.DeleteByContentIdAsync(CollectionName, contentId, cancellationToken);
        logger.LogInformation("Removed index for content {ContentId}", contentId);
    }

    private static string ExtractText(Content content)
    {
        // For now, combine available text sources
        // Future: add PDF extraction via a dedicated service, OCR, etc.
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(content.Title))
            parts.Add(content.Title);

        if (!string.IsNullOrWhiteSpace(content.Description))
        {
            string cleaned = StripHtml(content.Description);
            parts.Add(cleaned);
        }

        return string.Join("\n\n", parts);
    }

    internal static string StripHtml(string html)
    {
        // Remove HTML tags
        string text = HtmlTagRegex().Replace(html, " ");
        // Decode common entities
        text = text.Replace("&amp;", "&")
                   .Replace("&lt;", "<")
                   .Replace("&gt;", ">")
                   .Replace("&quot;", "\"")
                   .Replace("&nbsp;", " ");
        // Collapse whitespace
        text = WhitespaceRegex().Replace(text, " ").Trim();
        return text;
    }

    internal static IReadOnlyList<string> ChunkText(string text, int chunkSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        // Split into words
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (words.Length <= chunkSize)
            return [string.Join(' ', words)];

        var chunks = new List<string>();
        int step = chunkSize - overlap;

        for (int i = 0; i < words.Length; i += step)
        {
            int end = Math.Min(i + chunkSize, words.Length);
            string chunk = string.Join(' ', words[i..end]);
            chunks.Add(chunk);

            if (end >= words.Length) break;
        }

        return chunks;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
