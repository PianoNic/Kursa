using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Flashcards.Queries;

public sealed record GetDecksQuery : IQuery<Result<IReadOnlyList<FlashcardDeckDto>>>;

public sealed class GetDecksHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IQueryHandler<GetDecksQuery, Result<IReadOnlyList<FlashcardDeckDto>>>
{
    public async ValueTask<Result<IReadOnlyList<FlashcardDeckDto>>> Handle(GetDecksQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<IReadOnlyList<FlashcardDeckDto>>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<IReadOnlyList<FlashcardDeckDto>>.Failure("User not found.");

        DateTime now = DateTime.UtcNow;

        List<FlashcardDeckDto> decks = await dbContext.FlashcardDecks
            .Where(d => d.UserId == user.Id)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new FlashcardDeckDto(
                d.Id,
                d.Title,
                d.Description,
                d.Cards.Count,
                d.Cards.Count(c => c.NextReviewAt == null || c.NextReviewAt <= now),
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<FlashcardDeckDto>>.Success(decks);
    }
}
