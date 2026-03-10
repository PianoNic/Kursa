namespace Kursa.Application.Common.Interfaces;

public interface ITranscriptionService
{
    Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream,
        string contentType,
        CancellationToken cancellationToken = default);
}

public sealed record TranscriptionResult(
    bool Success,
    string? Text,
    int? DurationSeconds,
    IReadOnlyList<TranscriptionSegment>? Segments,
    string? Error);

public sealed record TranscriptionSegment(
    double StartSeconds,
    double EndSeconds,
    string Text);
