using Kursa.Domain.Entities;

namespace Kursa.Application.Features.Recordings;

public sealed record RecordingDto(
    Guid Id,
    string Title,
    string? Description,
    Guid? CourseId,
    string? CourseTitle,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int? DurationSeconds,
    RecordingStatus Status,
    bool HasTranscript,
    DateTime CreatedAt);

public sealed record RecordingDetailDto(
    Guid Id,
    string Title,
    string? Description,
    Guid? CourseId,
    string? CourseTitle,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    int? DurationSeconds,
    RecordingStatus Status,
    string? TranscriptText,
    IReadOnlyList<TranscriptSegmentDto> Segments,
    DateTime? TranscribedAt,
    string? ErrorMessage,
    DateTime CreatedAt);

public sealed record TranscriptSegmentDto(
    Guid Id,
    int OrderIndex,
    double StartSeconds,
    double EndSeconds,
    string Text,
    string? Speaker);
