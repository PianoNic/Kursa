using System.Text.Json.Serialization;

namespace Kursa.Application.Features.Moodle.Models;

/// <summary>
/// Wrapper for core_calendar_get_calendar_events response.
/// </summary>
public sealed record MoodleCalendarEventsResponseDto
{
    [JsonPropertyName("events")]
    public IReadOnlyList<MoodleCalendarEventDto> Events { get; init; } = [];
}

/// <summary>
/// A single Moodle calendar event.
/// </summary>
public sealed record MoodleCalendarEventDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("courseid")]
    public int CourseId { get; init; }

    [JsonPropertyName("timestart")]
    public long TimeStart { get; init; }

    [JsonPropertyName("timeduration")]
    public long TimeDuration { get; init; }

    [JsonPropertyName("eventtype")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("modulename")]
    public string? ModuleName { get; init; }

    [JsonPropertyName("instance")]
    public int? Instance { get; init; }
}

/// <summary>
/// Calendar event view for the frontend.
/// </summary>
public sealed record CalendarEventViewDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CourseId { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public int DurationMinutes { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? ModuleName { get; init; }
}
