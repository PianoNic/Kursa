using System.Text.Json.Serialization;

namespace Kursa.Application.Features.Moodle.Models;

/// <summary>
/// Wrapper returned by mod_assign_get_assignments.
/// </summary>
public sealed record MoodleAssignmentsResponseDto
{
    [JsonPropertyName("courses")]
    public IReadOnlyList<MoodleAssignmentCourseDto> Courses { get; init; } = [];
}

/// <summary>
/// A course containing its assignments.
/// </summary>
public sealed record MoodleAssignmentCourseDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("fullname")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("shortname")]
    public string ShortName { get; init; } = string.Empty;

    [JsonPropertyName("assignments")]
    public IReadOnlyList<MoodleAssignmentDto> Assignments { get; init; } = [];
}

/// <summary>
/// A single Moodle assignment.
/// </summary>
public sealed record MoodleAssignmentDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("cmid")]
    public int CmId { get; init; }

    [JsonPropertyName("course")]
    public int CourseId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    public string? Intro { get; init; }

    [JsonPropertyName("duedate")]
    public long DueDate { get; init; }

    [JsonPropertyName("allowsubmissionsfromdate")]
    public long AllowSubmissionsFromDate { get; init; }

    [JsonPropertyName("cutoffdate")]
    public long CutoffDate { get; init; }

    [JsonPropertyName("timemodified")]
    public long TimeModified { get; init; }

    [JsonPropertyName("gradingduedate")]
    public long GradingDueDate { get; init; }

    [JsonPropertyName("nosubmissions")]
    public int NoSubmissions { get; init; }

    [JsonPropertyName("maxattempts")]
    public int MaxAttempts { get; init; }
}

/// <summary>
/// Flattened assignment DTO for the frontend, enriched with course info.
/// </summary>
public sealed record AssignmentViewDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string CourseName { get; init; } = string.Empty;
    public string CourseShortName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime? DueDate { get; init; }
    public DateTime? OpenDate { get; init; }
    public DateTime? CutoffDate { get; init; }
    public bool IsOverdue { get; init; }
    public bool IsSubmittable { get; init; }
}
