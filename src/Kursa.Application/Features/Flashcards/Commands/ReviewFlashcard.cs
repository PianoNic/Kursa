using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Flashcards.Commands;

/// <summary>
/// SM-2 quality rating: 0 = complete blackout, 5 = perfect response.
/// </summary>
public sealed record ReviewFlashcardCommand(Guid CardId, int Quality) : ICommand<Result<ReviewResultDto>>;

public sealed class ReviewFlashcardValidator : AbstractValidator<ReviewFlashcardCommand>
{
    public ReviewFlashcardValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Quality).InclusiveBetween(0, 5);
    }
}

public sealed class ReviewFlashcardHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : ICommandHandler<ReviewFlashcardCommand, Result<ReviewResultDto>>
{
    public async ValueTask<Result<ReviewResultDto>> Handle(ReviewFlashcardCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<ReviewResultDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<ReviewResultDto>.Failure("User not found.");

        var card = await dbContext.Flashcards
            .Include(c => c.Deck)
            .FirstOrDefaultAsync(c => c.Id == request.CardId && c.Deck.UserId == user.Id, cancellationToken);

        if (card is null)
            return Result<ReviewResultDto>.Failure("Flashcard not found.");

        // SM-2 Algorithm
        ApplySm2(card, request.Quality);
        card.LastReviewedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<ReviewResultDto>.Success(new ReviewResultDto(
            card.Id,
            card.Repetitions,
            card.EaseFactor,
            card.IntervalDays,
            card.NextReviewAt ?? DateTime.UtcNow));
    }

    /// <summary>
    /// Implements the SM-2 spaced repetition algorithm.
    /// quality: 0-5 where 0 = complete blackout, 5 = perfect response
    /// </summary>
    private static void ApplySm2(Flashcard card, int quality)
    {
        // Update ease factor
        float newEf = card.EaseFactor + (0.1f - (5 - quality) * (0.08f + (5 - quality) * 0.02f));
        card.EaseFactor = Math.Max(1.3f, newEf);

        if (quality < 3)
        {
            // Failed: reset repetitions
            card.Repetitions = 0;
            card.IntervalDays = 1;
        }
        else
        {
            card.Repetitions++;

            card.IntervalDays = card.Repetitions switch
            {
                1 => 1,
                2 => 6,
                _ => (int)Math.Round(card.IntervalDays * card.EaseFactor),
            };
        }

        card.NextReviewAt = DateTime.UtcNow.AddDays(card.IntervalDays);
    }
}
