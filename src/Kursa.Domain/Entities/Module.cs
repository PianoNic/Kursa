namespace Kursa.Domain.Entities;

public class Module : BaseEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public int? MoodleModuleId { get; set; }

    public Guid CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public ICollection<Content> Contents { get; init; } = new List<Content>();
}
