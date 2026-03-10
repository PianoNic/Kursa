using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Infrastructure.Services;

public sealed class RecordingIndexingService(
    IAppDbContext dbContext,
    ILlmProvider llmProvider,
    IVectorStore vectorStore,
    ILogger<RecordingIndexingService> logger) : IRecordingIndexingService
{
    private const string CollectionName = "content_chunks";
    private const int ChunkSize = 512;
    private const int ChunkOverlap = 64;

    public async Task IndexRecordingAsync(Guid recordingId, CancellationToken cancellationToken = default)
    {
        Recording? recording = await dbContext.Recordings
            .Include(r => r.Segments.OrderBy(s => s.OrderIndex))
            .FirstOrDefaultAsync(r => r.Id == recordingId, cancellationToken);

        if (recording is null)
        {
            logger.LogWarning("Recording {RecordingId} not found for indexing", recordingId);
            return;
        }

        string text = BuildTranscriptText(recording);
        if (string.IsNullOrWhiteSpace(text))
        {
            logger.LogInformation("No transcript text for recording {RecordingId}, skipping indexing", recordingId);
            return;
        }

        IReadOnlyList<string> chunks = ContentPipeline.ChunkText(text, ChunkSize, ChunkOverlap);
        if (chunks.Count == 0) return;

        logger.LogInformation("Indexing recording {RecordingId} ({Title}): {ChunkCount} chunks",
            recordingId, recording.Title, chunks.Count);

        IReadOnlyList<float> firstEmbedding = await llmProvider.GenerateEmbeddingAsync(chunks[0], cancellationToken);
        await vectorStore.EnsureCollectionAsync(CollectionName, firstEmbedding.Count, cancellationToken);

        IReadOnlyList<IReadOnlyList<float>> embeddings = await llmProvider.GenerateEmbeddingsAsync(
            chunks.ToList(), cancellationToken);

        var points = new List<VectorPoint>(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            points.Add(new VectorPoint
            {
                Id = Guid.NewGuid(),
                Vector = embeddings[i],
                ContentId = recordingId, // Use recording ID as content ID for vector store
                UserId = recording.UserId,
                ChunkText = chunks[i],
                ChunkIndex = i,
                ContentTitle = $"[Recording] {recording.Title}",
                ContentType = "Recording",
                SourceUrl = null,
            });
        }

        // Delete existing vectors for this recording (re-index scenario)
        await vectorStore.DeleteByContentIdAsync(CollectionName, recordingId, cancellationToken);

        await vectorStore.UpsertAsync(CollectionName, points, cancellationToken);

        // Generate summary
        string summary = await GenerateSummaryAsync(text, cancellationToken);

        recording.Status = RecordingStatus.Completed;

        // Store summary in the description if it was empty, otherwise append
        if (!string.IsNullOrWhiteSpace(summary))
        {
            recording.Description = string.IsNullOrWhiteSpace(recording.Description)
                ? summary
                : $"{recording.Description}\n\n---\n**AI Summary:**\n{summary}";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully indexed recording {RecordingId} ({Title}) with {ChunkCount} chunks",
            recordingId, recording.Title, chunks.Count);
    }

    public async Task RemoveRecordingIndexAsync(Guid recordingId, CancellationToken cancellationToken = default)
    {
        await vectorStore.DeleteByContentIdAsync(CollectionName, recordingId, cancellationToken);
        logger.LogInformation("Removed index for recording {RecordingId}", recordingId);
    }

    private static string BuildTranscriptText(Recording recording)
    {
        if (recording.Segments.Count > 0)
        {
            return string.Join("\n", recording.Segments.Select(s => s.Text));
        }

        return recording.TranscriptText ?? string.Empty;
    }

    private async Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            // Truncate for summary to avoid token limits
            string truncated = text.Length > 8000 ? text[..8000] : text;

            string systemPrompt = """
                You are a study assistant. Summarize the following lecture transcript concisely.
                Focus on key topics, main concepts, and important takeaways.
                Use bullet points for clarity. Keep it under 300 words.
                """;

            var request = new LlmChatRequest
            {
                SystemPrompt = systemPrompt,
                Messages = [LlmMessage.User(truncated)],
                Temperature = 0.3f,
                MaxTokens = 500,
            };

            LlmChatResponse response = await llmProvider.ChatAsync(request, cancellationToken);
            return response.Content;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate recording summary");
            return string.Empty;
        }
    }
}
