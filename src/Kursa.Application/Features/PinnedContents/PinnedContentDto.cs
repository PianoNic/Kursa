namespace Kursa.Application.Features.PinnedContents;

public sealed record PinnedContentDto(
    Guid Id,
    Guid ContentId,
    string ContentTitle,
    string? ContentDescription,
    string ContentType,
    string? Url,
    bool IsStarred,
    bool IsIndexed,
    string? Notes,
    DateTime PinnedAt);
