using System.Text.Json.Serialization;

namespace Kursa.Application.Features.Moodle.Models;

/// <summary>
/// A Moodle forum instance returned by mod_forum_get_forums_by_courses.
/// </summary>
public sealed record MoodleForumDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("course")]
    public int CourseId { get; init; }

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("intro")]
    public string? Intro { get; init; }

    [JsonPropertyName("cmid")]
    public int CmId { get; init; }

    [JsonPropertyName("numdiscussions")]
    public int NumDiscussions { get; init; }

    [JsonPropertyName("timemodified")]
    public long TimeModified { get; init; }
}

/// <summary>
/// Wrapper for forum discussions response.
/// </summary>
public sealed record MoodleForumDiscussionsResponseDto
{
    [JsonPropertyName("discussions")]
    public IReadOnlyList<MoodleDiscussionDto> Discussions { get; init; } = [];
}

/// <summary>
/// A single forum discussion.
/// </summary>
public sealed record MoodleDiscussionDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("userfullname")]
    public string UserFullName { get; init; } = string.Empty;

    [JsonPropertyName("userpictureurl")]
    public string? UserPictureUrl { get; init; }

    [JsonPropertyName("created")]
    public long Created { get; init; }

    [JsonPropertyName("modified")]
    public long Modified { get; init; }

    [JsonPropertyName("numreplies")]
    public int NumReplies { get; init; }

    [JsonPropertyName("pinned")]
    public bool Pinned { get; init; }

    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("timemodified")]
    public long TimeModified { get; init; }
}

/// <summary>
/// Flattened forum view for the frontend.
/// </summary>
public sealed record ForumViewDto
{
    public int Id { get; init; }
    public int CourseId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Type { get; init; } = string.Empty;
    public int DiscussionCount { get; init; }
    public DateTime? LastModified { get; init; }
}

/// <summary>
/// Discussion view for the frontend.
/// </summary>
public sealed record DiscussionViewDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Message { get; init; }
    public string Author { get; init; } = string.Empty;
    public string? AuthorAvatar { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public int ReplyCount { get; init; }
    public bool IsPinned { get; init; }
}
