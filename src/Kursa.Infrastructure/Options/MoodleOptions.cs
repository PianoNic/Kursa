namespace Kursa.Infrastructure.Options;

public sealed class MoodleOptions
{
    public const string SectionName = "Moodle";

    /// <summary>Base URL of the MoodlewareAPI bridge (e.g. http://localhost:8000).</summary>
    public string BridgeUrl { get; init; } = string.Empty;

    /// <summary>Base URL of the actual Moodle site (e.g. https://moodle.example.com).</summary>
    public string SiteUrl { get; init; } = string.Empty;
}
