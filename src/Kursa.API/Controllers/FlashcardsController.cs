using Kursa.Application.Features.Flashcards;
using Kursa.Application.Features.Flashcards.Commands;
using Kursa.Application.Features.Flashcards.Queries;
using Kursa.Domain.Entities;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

[ApiController]
[Route("api/flashcards")]
[Authorize]
public class FlashcardsController(ISender sender) : ControllerBase
{
    [HttpGet("decks")]
    [ProducesResponseType(typeof(IReadOnlyList<FlashcardDeckDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDecksAsync(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDecksQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet("decks/{deckId:guid}")]
    [ProducesResponseType(typeof(FlashcardDeckDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDeckDetailAsync(Guid deckId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDeckDetailQuery(deckId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpGet("decks/{deckId:guid}/due")]
    [ProducesResponseType(typeof(IReadOnlyList<FlashcardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDueCardsAsync(Guid deckId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDueFlashcardsQuery(deckId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(result.Error);
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(FlashcardDeckDetailDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateFlashcardsAsync([FromBody] GenerateFlashcardsRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GenerateFlashcardsCommand(
            request.ContentId,
            request.CardCount,
            request.Topic), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("decks/{deckId:guid}/cards")]
    [ProducesResponseType(typeof(FlashcardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCardAsync(Guid deckId, [FromBody] CreateCardRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateFlashcardCommand(
            deckId,
            request.Front,
            request.Back,
            request.Type), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("cards/{cardId:guid}/review")]
    [ProducesResponseType(typeof(ReviewResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewCardAsync(Guid cardId, [FromBody] ReviewCardRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReviewFlashcardCommand(cardId, request.Quality), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }
}

public sealed record GenerateFlashcardsRequest(Guid ContentId, int CardCount = 20, string? Topic = null);

public sealed record CreateCardRequest(string Front, string Back, FlashcardType Type = FlashcardType.Basic);

public sealed record ReviewCardRequest(int Quality);
