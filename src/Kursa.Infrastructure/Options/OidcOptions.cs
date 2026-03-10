using System.ComponentModel.DataAnnotations;

namespace Kursa.Infrastructure.Options;

public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    [Required]
    public required string Authority { get; init; }

    [Required]
    public required string ClientId { get; init; }

    public string ClientSecret { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;
}
