namespace Kursa.Infrastructure.Options;

public sealed class MoodleOptions
{
    public const string SectionName = "Moodle";

    public string ApiBaseUrl { get; init; } = string.Empty;
}
