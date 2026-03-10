namespace Kursa.Infrastructure.Options;

public sealed class AudioProcessingOptions
{
    public const string SectionName = "AudioProcessing";

    public string WhisperUrl { get; init; } = "http://localhost:8001";

    public string PyannoteUrl { get; init; } = "http://localhost:8002";
}
