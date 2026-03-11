using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Flashcards.Queries;

public sealed record GetDueFlashcardsQuery(Guid DeckId) : IQuery<Result<IReadOnlyList<FlashcardDto>>>;

public sealed class GetDueFlashcardsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetDueFlashcardsQuery, Result<IReadOnlyList<FlashcardDto>>>
{
    public async ValueTask<Result<IReadOnlyList<FlashcardDto>>> Handle(GetDueFlashcardsQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<FlashcardDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<FlashcardDto>>.Failure("User not found.");

        bool deckExists = await dbContext.FlashcardDecks
            .AnyAsync(d => d.Id == request.DeckId && d.UserId == user.Id, cancellationToken);

        if (!deckExists)
            return Result<IReadOnlyList<FlashcardDto>>.Failure("Deck not found.");

        DateTime now = DateTime.UtcNow;

        List<FlashcardDto> dueCards = await dbContext.Flashcards
            .Where(c => c.DeckId == request.DeckId && (c.NextReviewAt == null || c.NextReviewAt <= now))
            .OrderBy(c => c.NextReviewAt)
            .Select(c => new FlashcardDto(
                c.Id, c.Front, c.Back, c.Type,
                c.Repetitions, c.EaseFactor, c.IntervalDays,
                c.NextReviewAt, c.LastReviewedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FlashcardDto>>.Success(dueCards);
    }
}
