namespace Kursa.Infrastructure.Options;

public sealed class MicrosoftGraphOptions
{
    public const string SectionName = "MicrosoftGraph";

    public string ClientId { get; init; } = string.Empty;

    public string TenantId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;
}
