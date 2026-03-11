namespace Kursa.Application.Features.Moodle.Models;

/// <summary>
/// Represents a Moodle course returned by core_enrol_get_users_courses.
/// </summary>
public sealed record MoodleCourseDto
{
    public int Id { get; init; }
    public string ShortName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public long StartDate { get; init; }
    public long EndDate { get; init; }
    public int Visible { get; init; } = 1;
    public string? CourseImage { get; init; }
    public double? Progress { get; init; }
    public bool? Completed { get; init; }
    public int Category { get; init; }
}

/// <summary>
/// Represents a course section returned by core_course_get_contents.
/// </summary>
public sealed record MoodleCourseSectionDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public int Section { get; init; }
    public int Visible { get; init; } = 1;
    public IReadOnlyList<MoodleModuleDto> Modules { get; init; } = [];
}

/// <summary>
/// Represents a module/activity within a course section.
/// </summary>
public sealed record MoodleModuleDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ModName { get; init; } = string.Empty;
    public string? ModPlural { get; init; }
    public string? Description { get; init; }
    public string? Url { get; init; }
    public int Visible { get; init; } = 1;
    public IReadOnlyList<MoodleContentDto>? Contents { get; init; }
}

/// <summary>
/// Represents a content file/resource within a module.
/// </summary>
public sealed record MoodleContentDto
{
    public string Type { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string? FilePath { get; init; }
    public long FileSize { get; init; }
    public string? FileUrl { get; init; }
    public long TimeCreated { get; init; }
    public long TimeModified { get; init; }
    public string? MimeType { get; init; }
}

/// <summary>
/// Represents Moodle site info returned by core_webservice_get_site_info.
/// </summary>
public sealed record MoodleSiteInfoDto
{
    public string SiteName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string SiteUrl { get; init; } = string.Empty;
    public string? UserPictureUrl { get; init; }
    public string? Language { get; init; }
}
