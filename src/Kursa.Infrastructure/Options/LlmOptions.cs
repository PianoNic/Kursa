using System.ComponentModel.DataAnnotations;

namespace Kursa.Infrastructure.Options;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    [Required]
    public required string Provider { get; init; }

    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "gpt-4o-mini";

    public string OllamaHost { get; init; } = "http://localhost:11434";
}
