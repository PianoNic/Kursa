using System.ComponentModel.DataAnnotations;

namespace Kursa.Infrastructure.Options;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    [Required]
    public required string Host { get; init; }

    public int Port { get; init; } = 6334;

    public string ApiKey { get; init; } = string.Empty;
}
