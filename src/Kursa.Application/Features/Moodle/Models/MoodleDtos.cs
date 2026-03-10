using System.Text.Json.Serialization;

namespace Kursa.Application.Features.Moodle.Models;

/// <summary>
/// Represents a Moodle course returned by core_enrol_get_users_courses.
/// </summary>
public sealed record MoodleCourseDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("shortname")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("fullname")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("startdate")]
    public long StartDate { get; init; }

    [JsonPropertyName("enddate")]
    public long EndDate { get; init; }

    [JsonPropertyName("visible")]
    public int Visible { get; init; } = 1;

    [JsonPropertyName("courseimage")]
    public string? CourseImage { get; init; }

    [JsonPropertyName("progress")]
    public double? Progress { get; init; }

    [JsonPropertyName("completed")]
    public bool? Completed { get; init; }

    [JsonPropertyName("category")]
    public int Category { get; init; }
}

/// <summary>
/// Represents a course section returned by core_course_get_contents.
/// </summary>
public sealed record MoodleCourseSectionDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("section")]
    public int Section { get; init; }

    [JsonPropertyName("visible")]
    public int Visible { get; init; } = 1;

    [JsonPropertyName("modules")]
    public IReadOnlyList<MoodleModuleDto> Modules { get; init; } = [];
}

/// <summary>
/// Represents a module/activity within a course section.
/// </summary>
public sealed record MoodleModuleDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("modname")]
    public string ModName { get; init; } = string.Empty;

    [JsonPropertyName("modplural")]
    public string? ModPlural { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("visible")]
    public int Visible { get; init; } = 1;

    [JsonPropertyName("contents")]
    public IReadOnlyList<MoodleContentDto>? Contents { get; init; }
}

/// <summary>
/// Represents a content file/resource within a module.
/// </summary>
public sealed record MoodleContentDto
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("filename")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("filepath")]
    public string? FilePath { get; init; }

    [JsonPropertyName("filesize")]
    public long FileSize { get; init; }

    [JsonPropertyName("fileurl")]
    public string? FileUrl { get; init; }

    [JsonPropertyName("timecreated")]
    public long TimeCreated { get; init; }

    [JsonPropertyName("timemodified")]
    public long TimeModified { get; init; }

    [JsonPropertyName("mimetype")]
    public string? MimeType { get; init; }
}

/// <summary>
/// Represents Moodle site info returned by core_webservice_get_site_info.
/// </summary>
public sealed record MoodleSiteInfoDto
{
    [JsonPropertyName("sitename")]
    public string SiteName { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("firstname")]
    public string FirstName { get; init; } = string.Empty;

    [JsonPropertyName("lastname")]
    public string LastName { get; init; } = string.Empty;

    [JsonPropertyName("fullname")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("userid")]
    public int UserId { get; init; }

    [JsonPropertyName("siteurl")]
    public string SiteUrl { get; init; } = string.Empty;

    [JsonPropertyName("userpictureurl")]
    public string? UserPictureUrl { get; init; }

    [JsonPropertyName("lang")]
    public string? Language { get; init; }
}
