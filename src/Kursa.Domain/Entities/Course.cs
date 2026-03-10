namespace Kursa.Domain.Entities;

public class Course : BaseEntity
{
    public required string Name { get; set; }

    public string? ShortName { get; set; }

    public string? Summary { get; set; }

    public int? MoodleCourseId { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsVisible { get; set; } = true;

    public ICollection<Module> Modules { get; init; } = new List<Module>();

    public ICollection<User> Users { get; init; } = new List<User>();
}
