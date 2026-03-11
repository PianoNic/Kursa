using Kursa.Application.Common.Interfaces;
using Kursa.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

namespace Kursa.Infrastructure.Services;

public sealed class RecordingIndexingService(
    IAppDbContext dbContext,
    IChatCompletionService chatCompletionService,
    ITextEmbeddingGenerationService embeddingService,
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

#pragma warning disable SKEXP0001
        ReadOnlyMemory<float> firstEmbedding = await embeddingService.GenerateEmbeddingAsync(chunks[0], cancellationToken: cancellationToken);
        await vectorStore.EnsureCollectionAsync(CollectionName, firstEmbedding.Length, cancellationToken);

        IList<ReadOnlyMemory<float>> embeddings = await embeddingService.GenerateEmbeddingsAsync(
            chunks.ToList(), cancellationToken: cancellationToken);
#pragma warning restore SKEXP0001

        var points = new List<VectorPoint>(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            points.Add(new VectorPoint
            {
                Id = Guid.NewGuid(),
                Vector = embeddings[i].ToArray(),
                ContentId = recordingId,
                UserId = recording.UserId,
                ChunkText = chunks[i],
                ChunkIndex = i,
                ContentTitle = $"[Recording] {recording.Title}",
                ContentType = "Recording",
                SourceUrl = null,
            });
        }

        await vectorStore.DeleteByContentIdAsync(CollectionName, recordingId, cancellationToken);
        await vectorStore.UpsertAsync(CollectionName, points, cancellationToken);

        string summary = await GenerateSummaryAsync(text, cancellationToken);

        recording.Status = RecordingStatus.Completed;

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
            return string.Join("\n", recording.Segments.Select(s => s.Text));

        return recording.TranscriptText ?? string.Empty;
    }

    private async Task<string> GenerateSummaryAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            string truncated = text.Length > 8000 ? text[..8000] : text;

            var history = new ChatHistory(
                """
                You are a study assistant. Summarize the following lecture transcript concisely.
                Focus on key topics, main concepts, and important takeaways.
                Use bullet points for clarity. Keep it under 300 words.
                """);
            history.AddUserMessage(truncated);

#pragma warning disable SKEXP0001
            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.3f, MaxTokens = 500 };
#pragma warning restore SKEXP0001

            var response = await chatCompletionService.GetChatMessageContentAsync(history, settings, cancellationToken: cancellationToken);
            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate recording summary");
            return string.Empty;
        }
    }
}
