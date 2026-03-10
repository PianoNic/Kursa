using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kursa.Application.Features.Flashcards.Commands;

public sealed record CreateFlashcardCommand(
    Guid DeckId,
    string Front,
    string Back,
    FlashcardType Type = FlashcardType.Basic) : IRequest<Result<FlashcardDto>>;

public sealed class CreateFlashcardValidator : AbstractValidator<CreateFlashcardCommand>
{
    public CreateFlashcardValidator()
    {
        RuleFor(x => x.DeckId).NotEmpty();
        RuleFor(x => x.Front).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Back).NotEmpty().MaximumLength(2000);
    }
}

public sealed class CreateFlashcardHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext) : IRequestHandler<CreateFlashcardCommand, Result<FlashcardDto>>
{
    public async Task<Result<FlashcardDto>> Handle(CreateFlashcardCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<FlashcardDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<FlashcardDto>.Failure("User not found.");

        bool deckExists = await dbContext.FlashcardDecks
            .AnyAsync(d => d.Id == request.DeckId && d.UserId == user.Id, cancellationToken);

        if (!deckExists)
            return Result<FlashcardDto>.Failure("Deck not found.");

        var card = new Flashcard
        {
            DeckId = request.DeckId,
            Front = request.Front,
            Back = request.Back,
            Type = request.Type,
            NextReviewAt = DateTime.UtcNow,
        };

        dbContext.Flashcards.Add(card);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<FlashcardDto>.Success(new FlashcardDto(
            card.Id, card.Front, card.Back, card.Type,
            card.Repetitions, card.EaseFactor, card.IntervalDays,
            card.NextReviewAt, card.LastReviewedAt));
    }
}
