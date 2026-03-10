using System.ComponentModel.DataAnnotations;

namespace Kursa.Infrastructure.Options;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    [Required]
    public required string Provider { get; init; }

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gpt-4o-mini";

    public string EmbeddingModel { get; init; } = "text-embedding-3-small";

    public string OllamaHost { get; init; } = "http://localhost:11434";

    public int MaxRetries { get; init; } = 3;

    public int TimeoutSeconds { get; init; } = 60;
}
