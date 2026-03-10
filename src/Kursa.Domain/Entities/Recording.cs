namespace Kursa.Domain.Entities;

public class Recording : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public required string Title { get; set; }

    public string? Description { get; set; }

    public Guid? CourseId { get; set; }

    public Course? Course { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Object key in MinIO storage.
    /// </summary>
    public required string ObjectKey { get; set; }

    public RecordingStatus Status { get; set; } = RecordingStatus.Uploaded;

    public string? TranscriptText { get; set; }

    public DateTime? TranscribedAt { get; set; }

    public string? ErrorMessage { get; set; }
}

public enum RecordingStatus
{
    Uploaded,
    Transcribing,
    Transcribed,
    Indexing,
    Completed,
    Failed,
}
