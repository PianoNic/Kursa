namespace Kursa.Application.Features.PinnedContents;

public sealed record PinContentResponse(Guid Id);

public sealed record ToggleStarResponse(bool IsStarred);
