namespace Kursa.Application.Features.Chat;

public sealed record ChatThreadDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record ChatMessageDto(Guid Id, string Role, string Content, string? Citations, int TokensUsed, DateTime CreatedAt);

public sealed record ChatResponseDto(ChatMessageDto Message, IReadOnlyList<CitationDto> Sources);

public sealed record CitationDto(Guid ContentId, string ContentTitle, string ChunkText, float Score, string? SourceUrl);
