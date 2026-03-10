using System.Text.Json;
using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Features.Quizzes.Commands;

public sealed record GenerateQuizCommand(
    Guid ContentId,
    int QuestionCount = 10,
    string? Topic = null,
    int DurationSeconds = 600) : IRequest<Result<QuizDetailDto>>;

public sealed class GenerateQuizValidator : AbstractValidator<GenerateQuizCommand>
{
    public GenerateQuizValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.QuestionCount).InclusiveBetween(1, 50);
        RuleFor(x => x.DurationSeconds).InclusiveBetween(60, 7200);
    }
}

public sealed class GenerateQuizHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    ILlmProvider llmProvider,
    IVectorStore vectorStore,
    ILogger<GenerateQuizHandler> logger) : IRequestHandler<GenerateQuizCommand, Result<QuizDetailDto>>
{
    private const string CollectionName = "content_chunks";

    private const string SystemPrompt =
        """
        You are a quiz generator for educational materials. Generate questions based on the provided content.

        Rules:
        - Generate exactly the requested number of questions.
        - Mix question types: multiple_choice, true_false, fill_in_the_blank.
        - For multiple_choice: provide exactly 4 options, one must be the correct answer.
        - For true_false: the correct answer must be exactly "True" or "False".
        - For fill_in_the_blank: the question should contain "___" where the answer goes.
        - Each question must have an explanation for the correct answer.
        - Questions should test understanding, not just memorization.
        - Write questions in the same language as the source material.

        Respond ONLY with valid JSON in this exact format:
        {
          "questions": [
            {
              "question": "What is...?",
              "type": "multiple_choice",
              "options": ["A", "B", "C", "D"],
              "correct_answer": "A",
              "explanation": "Because..."
            }
          ]
        }
        """;

    public async Task<Result<QuizDetailDto>> Handle(GenerateQuizCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<QuizDetailDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<QuizDetailDto>.Failure("User not found.");

        // Verify content is pinned and indexed
        var pinnedContent = await dbContext.PinnedContents
            .Include(p => p.Content)
            .FirstOrDefaultAsync(p => p.ContentId == request.ContentId && p.UserId == user.Id, cancellationToken);

        if (pinnedContent is null)
            return Result<QuizDetailDto>.Failure("Content must be pinned before generating a quiz.");

        if (!pinnedContent.IsIndexed)
            return Result<QuizDetailDto>.Failure("Content must be indexed before generating a quiz. Please wait for indexing to complete.");

        // Retrieve content chunks from vector store
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
            return Result<QuizDetailDto>.Failure("No indexed content found to generate questions from.");

        // Build context from chunks
        string context = string.Join("\n\n---\n\n",
            searchResults.Select(r => r.ChunkText));

        string userPrompt = $"""
            Generate {request.QuestionCount} questions from the following material:

            {context}

            {(request.Topic is not null ? $"Focus on the topic: {request.Topic}" : "Cover the main concepts.")}
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

            // Parse the LLM response
            var generated = ParseQuizResponse(llmResponse.Content);

            if (generated.Count == 0)
                return Result<QuizDetailDto>.Failure("Failed to generate quiz questions. Please try again.");

            // Create quiz entity
            var quiz = new Quiz
            {
                UserId = user.Id,
                Title = request.Topic is not null
                    ? $"Quiz: {request.Topic}"
                    : $"Quiz: {pinnedContent.Content.Title}",
                Topic = request.Topic,
                ContentId = request.ContentId,
                QuestionCount = generated.Count,
                DurationSeconds = request.DurationSeconds,
            };

            for (int i = 0; i < generated.Count; i++)
            {
                var q = generated[i];
                quiz.Questions.Add(new QuizQuestion
                {
                    QuizId = quiz.Id,
                    QuestionText = q.Question,
                    Type = q.Type,
                    Options = q.Options is not null ? JsonSerializer.Serialize(q.Options) : null,
                    CorrectAnswer = q.CorrectAnswer,
                    Explanation = q.Explanation,
                    OrderIndex = i,
                });
            }

            dbContext.Quizzes.Add(quiz);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Build response DTO
            var questionDtos = quiz.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(q => new QuizQuestionDto(
                    q.Id,
                    q.QuestionText,
                    q.Type,
                    q.Options is not null ? JsonSerializer.Deserialize<List<string>>(q.Options) : null,
                    q.OrderIndex))
                .ToList();

            return Result<QuizDetailDto>.Success(new QuizDetailDto(
                quiz.Id,
                quiz.Title,
                quiz.Topic,
                quiz.DurationSeconds,
                questionDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate quiz for content {ContentId}", request.ContentId);
            return Result<QuizDetailDto>.Failure("Failed to generate quiz. Please try again.");
        }
    }

    private static List<GeneratedQuestion> ParseQuizResponse(string llmContent)
    {
        var result = new List<GeneratedQuestion>();

        try
        {
            // Extract JSON from response (may be wrapped in markdown code block)
            string json = llmContent;
            int jsonStart = json.IndexOf('{');
            int jsonEnd = json.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                json = json[jsonStart..(jsonEnd + 1)];
            }

            using var doc = JsonDocument.Parse(json);
            JsonElement questions = doc.RootElement.GetProperty("questions");

            foreach (JsonElement q in questions.EnumerateArray())
            {
                string typeStr = q.GetProperty("type").GetString() ?? "multiple_choice";
                QuestionType type = typeStr switch
                {
                    "true_false" => QuestionType.TrueFalse,
                    "fill_in_the_blank" => QuestionType.FillInTheBlank,
                    "open_ended" => QuestionType.OpenEnded,
                    _ => QuestionType.MultipleChoice,
                };

                List<string>? options = null;
                if (q.TryGetProperty("options", out JsonElement optionsEl) && optionsEl.ValueKind == JsonValueKind.Array)
                {
                    options = optionsEl.EnumerateArray()
                        .Select(o => o.GetString() ?? string.Empty)
                        .ToList();
                }

                result.Add(new GeneratedQuestion(
                    q.GetProperty("question").GetString() ?? string.Empty,
                    type,
                    options,
                    q.GetProperty("correct_answer").GetString() ?? string.Empty,
                    q.TryGetProperty("explanation", out JsonElement explEl) ? explEl.GetString() : null));
            }
        }
        catch (JsonException)
        {
            // Return empty if parsing fails
        }

        return result;
    }

    private sealed record GeneratedQuestion(
        string Question,
        QuestionType Type,
        List<string>? Options,
        string CorrectAnswer,
        string? Explanation);
}
