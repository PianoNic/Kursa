namespace Kursa.Application.Common.Interfaces;

public interface ILlmProvider
{
    Task<LlmChatResponse> ChatAsync(
        LlmChatRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}

public sealed record LlmChatRequest
{
    public required string SystemPrompt { get; init; }
    public required IReadOnlyList<LlmMessage> Messages { get; init; }
    public float Temperature { get; init; } = 0.7f;
    public int? MaxTokens { get; init; }
}

public sealed record LlmMessage(string Role, string Content)
{
    public static LlmMessage User(string content) => new("user", content);
    public static LlmMessage Assistant(string content) => new("assistant", content);
}

public sealed record LlmChatResponse
{
    public required string Content { get; init; }
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}
