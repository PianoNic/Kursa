using Kursa.Domain.Enums;

namespace Kursa.Domain.Entities;

public class User : BaseEntity
{
    public required string ExternalId { get; init; }

    public required string Email { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;

    public string? MoodleToken { get; set; }

    public string? MoodleUrl { get; set; }

    public bool OnboardingCompleted { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<Course> Courses { get; init; } = new List<Course>();

    public ICollection<PinnedContent> PinnedContents { get; init; } = new List<PinnedContent>();

    public UserSettings? Settings { get; set; }

    public string? AvatarUrl { get; set; }
}
