using Kursa.Domain.Entities;

namespace Kursa.Application.Features.Flashcards;

public sealed record FlashcardDeckDto(
    Guid Id,
    string Title,
    string? Description,
    int CardCount,
    int DueCount,
    DateTime CreatedAt);

public sealed record FlashcardDeckDetailDto(
    Guid Id,
    string Title,
    string? Description,
    IReadOnlyList<FlashcardDto> Cards);

public sealed record FlashcardDto(
    Guid Id,
    string Front,
    string Back,
    FlashcardType Type,
    int Repetitions,
    float EaseFactor,
    int IntervalDays,
    DateTime? NextReviewAt,
    DateTime? LastReviewedAt);

public sealed record ReviewResultDto(
    Guid CardId,
    int NewRepetitions,
    float NewEaseFactor,
    int NewIntervalDays,
    DateTime NextReviewAt);
