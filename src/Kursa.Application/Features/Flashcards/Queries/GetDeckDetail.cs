using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Flashcards.Queries;

public sealed record GetDeckDetailQuery(Guid DeckId) : IRequest<Result<FlashcardDeckDetailDto>>;

public sealed class GetDeckDetailHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<GetDeckDetailQuery, Result<FlashcardDeckDetailDto>>
{
    public async Task<Result<FlashcardDeckDetailDto>> Handle(GetDeckDetailQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<FlashcardDeckDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<FlashcardDeckDetailDto>.Failure("User not found.");

        var deck = await dbContext.FlashcardDecks
            .Include(d => d.Cards)
            .FirstOrDefaultAsync(d => d.Id == request.DeckId && d.UserId == user.Id, cancellationToken);

        if (deck is null)
            return Result<FlashcardDeckDetailDto>.Failure("Deck not found.");

        var cards = deck.Cards.Select(c => new FlashcardDto(
            c.Id, c.Front, c.Back, c.Type,
            c.Repetitions, c.EaseFactor, c.IntervalDays,
            c.NextReviewAt, c.LastReviewedAt)).ToList();

        return Result<FlashcardDeckDetailDto>.Success(new FlashcardDeckDetailDto(
            deck.Id, deck.Title, deck.Description, cards));
    }
}
