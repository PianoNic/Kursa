using System.Text.Json.Serialization;

namespace Kursa.Application.Features.Graph.Models;

// -- OneNote DTOs --

public sealed record OneNoteNotebookDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime? LastModifiedAt { get; init; }
}

public sealed record OneNoteSectionDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime? LastModifiedAt { get; init; }
}

public sealed record OneNotePageDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedAt { get; init; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime? LastModifiedAt { get; init; }

    [JsonPropertyName("contentUrl")]
    public string? ContentUrl { get; init; }
}

// -- SharePoint DTOs --

public sealed record SharePointSiteDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

public sealed record SharePointDriveItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime? LastModifiedAt { get; init; }

    public bool IsFolder { get; init; }

    public string? MimeType { get; init; }
}

// -- Graph API wrapper DTOs for deserialization --

public sealed record GraphCollectionResponse<T>
{
    [JsonPropertyName("value")]
    public IReadOnlyList<T> Value { get; init; } = [];
}

public sealed record GraphDriveItemRaw
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; init; }

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTime? LastModifiedDateTime { get; init; }

    [JsonPropertyName("folder")]
    public GraphFolderFacet? Folder { get; init; }

    [JsonPropertyName("file")]
    public GraphFileFacet? File { get; init; }
}

public sealed record GraphFolderFacet
{
    [JsonPropertyName("childCount")]
    public int ChildCount { get; init; }
}

public sealed record GraphFileFacet
{
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }
}
