namespace Kursa.Domain.Entities;

public class TranscriptSegment : BaseEntity
{
    public Guid RecordingId { get; set; }

    public Recording Recording { get; set; } = null!;

    public int OrderIndex { get; set; }

    public double StartSeconds { get; set; }

    public double EndSeconds { get; set; }

    public required string Text { get; set; }

    public string? Speaker { get; set; }
}
