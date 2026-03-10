using System.Text.Json;
using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Features.Flashcards.Commands;

public sealed record GenerateFlashcardsCommand(
    Guid ContentId,
    int CardCount = 20,
    string? Topic = null) : IRequest<Result<FlashcardDeckDetailDto>>;

public sealed class GenerateFlashcardsValidator : AbstractValidator<GenerateFlashcardsCommand>
{
    public GenerateFlashcardsValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.CardCount).InclusiveBetween(1, 100);
    }
}

public sealed class GenerateFlashcardsHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    ILlmProvider llmProvider,
    IVectorStore vectorStore,
    ILogger<GenerateFlashcardsHandler> logger) : IRequestHandler<GenerateFlashcardsCommand, Result<FlashcardDeckDetailDto>>
{
    private const string CollectionName = "content_chunks";

    private const string SystemPrompt =
        """
        You are a flashcard generator for educational materials. Generate flashcards based on the provided content.

        Rules:
        - Generate exactly the requested number of flashcards.
        - Mix types: "basic" (question/answer), "cloze" (fill in blank with "{{c1::answer}}" syntax on front), "reversible" (both sides are meaningful).
        - Focus on key concepts, definitions, and important facts.
        - Keep fronts concise and specific.
        - Backs should be clear and complete but not overly long.
        - Write in the same language as the source material.

        Respond ONLY with valid JSON in this exact format:
        {
          "cards": [
            {
              "front": "What is...?",
              "back": "The answer is...",
              "type": "basic"
            }
          ]
        }
        """;

    public async Task<Result<FlashcardDeckDetailDto>> Handle(GenerateFlashcardsCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<FlashcardDeckDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<FlashcardDeckDetailDto>.Failure("User not found.");

        var pinnedContent = await dbContext.PinnedContents
            .Include(p => p.Content)
            .FirstOrDefaultAsync(p => p.ContentId == request.ContentId && p.UserId == user.Id, cancellationToken);

        if (pinnedContent is null)
            return Result<FlashcardDeckDetailDto>.Failure("Content must be pinned before generating flashcards.");

        if (!pinnedContent.IsIndexed)
            return Result<FlashcardDeckDetailDto>.Failure("Content must be indexed first.");

        string searchQuery = request.Topic ?? pinnedContent.Content.Title;
        IReadOnlyList<float> queryEmbedding = await llmProvider.GenerateEmbeddingAsync(searchQuery, cancellationToken);

        IReadOnlyList<VectorSearchResult> searchResults = await vectorStore.SearchAsync(
            CollectionName,
            queryEmbedding,
            limit: 10,
            filterByUserId: user.Id,
            filterByContentId: request.ContentId,
            cancellationToken: cancellationToken);

        if (searchResults.Count == 0)
            return Result<FlashcardDeckDetailDto>.Failure("No indexed content found.");

        string context = string.Join("\n\n---\n\n", searchResults.Select(r => r.ChunkText));

        string userPrompt = $"""
            Generate {request.CardCount} flashcards from the following material:

            {context}

            {(request.Topic is not null ? $"Focus on: {request.Topic}" : "Cover the main concepts.")}
            """;

        try
        {
            LlmChatResponse llmResponse = await llmProvider.ChatAsync(new LlmChatRequest
            {
                SystemPrompt = SystemPrompt,
                Messages = [LlmMessage.User(userPrompt)],
                Temperature = 0.5f,
                MaxTokens = 4096,
            }, cancellationToken);

            var cards = ParseFlashcardResponse(llmResponse.Content);

            if (cards.Count == 0)
                return Result<FlashcardDeckDetailDto>.Failure("Failed to generate flashcards. Please try again.");

            var deck = new FlashcardDeck
            {
                UserId = user.Id,
                Title = request.Topic is not null
                    ? $"Flashcards: {request.Topic}"
                    : $"Flashcards: {pinnedContent.Content.Title}",
                Description = request.Topic,
                ContentId = request.ContentId,
            };

            foreach (var card in cards)
            {
                deck.Cards.Add(new Flashcard
                {
                    DeckId = deck.Id,
                    Front = card.Front,
                    Back = card.Back,
                    Type = card.Type,
                    NextReviewAt = DateTime.UtcNow,
                });
            }

            dbContext.FlashcardDecks.Add(deck);
            await dbContext.SaveChangesAsync(cancellationToken);

            var cardDtos = deck.Cards.Select(c => new FlashcardDto(
                c.Id, c.Front, c.Back, c.Type,
                c.Repetitions, c.EaseFactor, c.IntervalDays,
                c.NextReviewAt, c.LastReviewedAt)).ToList();

            return Result<FlashcardDeckDetailDto>.Success(new FlashcardDeckDetailDto(
                deck.Id, deck.Title, deck.Description, cardDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate flashcards for content {ContentId}", request.ContentId);
            return Result<FlashcardDeckDetailDto>.Failure("Failed to generate flashcards. Please try again.");
        }
    }

    private static List<GeneratedCard> ParseFlashcardResponse(string content)
    {
        var result = new List<GeneratedCard>();

        try
        {
            string json = content;
            int jsonStart = json.IndexOf('{');
            int jsonEnd = json.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
                json = json[jsonStart..(jsonEnd + 1)];

            using var doc = JsonDocument.Parse(json);
            JsonElement cards = doc.RootElement.GetProperty("cards");

            foreach (JsonElement c in cards.EnumerateArray())
            {
                string typeStr = c.TryGetProperty("type", out JsonElement typeEl) ? typeEl.GetString() ?? "basic" : "basic";
                FlashcardType type = typeStr switch
                {
                    "cloze" => FlashcardType.Cloze,
                    "reversible" => FlashcardType.Reversible,
                    _ => FlashcardType.Basic,
                };

                result.Add(new GeneratedCard(
                    c.GetProperty("front").GetString() ?? string.Empty,
                    c.GetProperty("back").GetString() ?? string.Empty,
                    type));
            }
        }
        catch (JsonException)
        {
            // Return empty on parse failure
        }

        return result;
    }

    private sealed record GeneratedCard(string Front, string Back, FlashcardType Type);
}
