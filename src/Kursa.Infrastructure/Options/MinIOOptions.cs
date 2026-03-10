using System.ComponentModel.DataAnnotations;

namespace Kursa.Infrastructure.Options;

public sealed class MinIOOptions
{
    public const string SectionName = "MinIO";

    [Required]
    public required string Endpoint { get; init; }

    [Required]
    public required string AccessKey { get; init; }

    [Required]
    public required string SecretKey { get; init; }

    public bool UseSsl { get; init; }

    public string BucketName { get; init; } = "kursa";
}
