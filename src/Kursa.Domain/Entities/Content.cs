using Kursa.Domain.Enums;

namespace Kursa.Domain.Entities;

public class Content : BaseEntity
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public ContentType Type { get; set; }

    public string? Url { get; set; }

    public string? FilePath { get; set; }

    public int? MoodleContentId { get; set; }

    public int SortOrder { get; set; }

    public Guid ModuleId { get; set; }

    public Module Module { get; set; } = null!;

    public ICollection<PinnedContent> PinnedByUsers { get; init; } = new List<PinnedContent>();
}
