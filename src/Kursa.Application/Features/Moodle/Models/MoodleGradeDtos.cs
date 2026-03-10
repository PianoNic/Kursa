using System.Text.Json.Serialization;

namespace Kursa.Application.Features.Moodle.Models;

/// <summary>
/// Wrapper returned by gradereport_user_get_grade_items.
/// </summary>
public sealed record MoodleGradeReportDto
{
    [JsonPropertyName("usergrades")]
    public IReadOnlyList<MoodleUserGradeDto> UserGrades { get; init; } = [];
}

/// <summary>
/// Per-course grade report for a user.
/// </summary>
public sealed record MoodleUserGradeDto
{
    [JsonPropertyName("courseid")]
    public int CourseId { get; init; }

    [JsonPropertyName("courseidnumber")]
    public string? CourseIdNumber { get; init; }

    [JsonPropertyName("userfullname")]
    public string UserFullName { get; init; } = string.Empty;

    [JsonPropertyName("maxdepth")]
    public int MaxDepth { get; init; }

    [JsonPropertyName("gradeitems")]
    public IReadOnlyList<MoodleGradeItemDto> GradeItems { get; init; } = [];
}

/// <summary>
/// A single grade item (assignment, quiz, course total, etc.).
/// </summary>
public sealed record MoodleGradeItemDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("itemname")]
    public string? ItemName { get; init; }

    [JsonPropertyName("itemtype")]
    public string ItemType { get; init; } = string.Empty;

    [JsonPropertyName("itemmodule")]
    public string? ItemModule { get; init; }

    [JsonPropertyName("iteminstance")]
    public int? ItemInstance { get; init; }

    [JsonPropertyName("categoryid")]
    public int? CategoryId { get; init; }

    [JsonPropertyName("graderaw")]
    public double? GradeRaw { get; init; }

    [JsonPropertyName("gradeformatted")]
    public string? GradeFormatted { get; init; }

    [JsonPropertyName("grademin")]
    public double GradeMin { get; init; }

    [JsonPropertyName("grademax")]
    public double GradeMax { get; init; }

    [JsonPropertyName("percentageformatted")]
    public string? PercentageFormatted { get; init; }

    [JsonPropertyName("feedback")]
    public string? Feedback { get; init; }

    [JsonPropertyName("cmid")]
    public int? CmId { get; init; }

    [JsonPropertyName("weightraw")]
    public double? WeightRaw { get; init; }

    [JsonPropertyName("weightformatted")]
    public string? WeightFormatted { get; init; }
}

/// <summary>
/// Flattened grade view DTO for the frontend.
/// </summary>
public sealed record GradeViewDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string? ItemName { get; init; }
    public string ItemType { get; init; } = string.Empty;
    public string? ItemModule { get; init; }
    public double? Grade { get; init; }
    public string? GradeFormatted { get; init; }
    public double GradeMin { get; init; }
    public double GradeMax { get; init; }
    public string? Percentage { get; init; }
    public string? Feedback { get; init; }
    public string? Weight { get; init; }
}

/// <summary>
/// Grade summary per course for the overview.
/// </summary>
public sealed record CourseGradeSummaryDto
{
    public int CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string? CourseTotal { get; init; }
    public double? CourseTotalRaw { get; init; }
    public double CourseTotalMax { get; init; }
    public string? Percentage { get; init; }
    public int GradedItemCount { get; init; }
    public int TotalItemCount { get; init; }
    public IReadOnlyList<GradeViewDto> Items { get; init; } = [];
}
