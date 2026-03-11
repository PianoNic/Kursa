using System.ComponentModel.DataAnnotations;

namespace Kursa.Infrastructure.Options;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    [Required]
    public required string Provider { get; init; }

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "anthropic/claude-haiku-4-5";

    /// <summary>Embedding model served by Ollama (used as embedding backend for all providers).</summary>
    public string EmbeddingModel { get; init; } = "nomic-embed-text";

    public string OpenRouterBaseUrl { get; init; } = "https://openrouter.ai/api/v1";

    /// <summary>Must include the /v1 path — e.g. http://localhost:11434/v1 or https://your-ollama/v1</summary>
    public string OllamaHost { get; init; } = "http://localhost:11434/v1";

    public int MaxRetries { get; init; } = 3;

    public int TimeoutSeconds { get; init; } = 60;
}
