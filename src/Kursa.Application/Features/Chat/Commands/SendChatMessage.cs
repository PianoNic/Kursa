using System.Text.Json;
using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kursa.Application.Features.Chat.Commands;

public sealed record SendChatMessageCommand(Guid? ThreadId, string Message) : IRequest<Result<ChatResponseDto>>;

public sealed class SendChatMessageValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4096);
    }
}

public sealed class SendChatMessageHandler(
    ICurrentUserService currentUserService,
    IAppDbContext dbContext,
    ILlmProvider llmProvider,
    IVectorStore vectorStore,
    ILogger<SendChatMessageHandler> logger) : IRequestHandler<SendChatMessageCommand, Result<ChatResponseDto>>
{
    private const string CollectionName = "content_chunks";

    private const string SystemPrompt =
        """
        You are Kursa, an AI study assistant. Answer the user's question based on the provided context from their course materials.

        Rules:
        - Use ONLY the provided context to answer. If the context doesn't contain relevant information, say so.
        - When referencing information from the context, cite the source using [Source N] format where N corresponds to the source number.
        - Be concise but thorough. Use bullet points and structure where helpful.
        - Write in the same language as the user's question.
        - If the user asks something unrelated to study materials, politely redirect them.
        """;

    public async Task<Result<ChatResponseDto>> Handle(SendChatMessageCommand request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || currentUserService.ExternalId is null)
            return Result<ChatResponseDto>.Failure("User is not authenticated.");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.ExternalId == currentUserService.ExternalId, cancellationToken);

        if (user is null)
            return Result<ChatResponseDto>.Failure("User not found.");

        // Get or create thread
        ChatThread thread;
        if (request.ThreadId.HasValue)
        {
            ChatThread? existing = await dbContext.ChatThreads
                .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(t => t.Id == request.ThreadId.Value && t.UserId == user.Id, cancellationToken);

            if (existing is null)
                return Result<ChatResponseDto>.Failure("Chat thread not found.");

            thread = existing;
        }
        else
        {
            string title = request.Message.Length > 80
                ? string.Concat(request.Message.AsSpan(0, 77), "...")
                : request.Message;

            thread = new ChatThread
            {
                UserId = user.Id,
                Title = title,
            };
            dbContext.ChatThreads.Add(thread);
        }

        // Save user message
        var userMessage = new ChatMessage
        {
            ThreadId = thread.Id,
            Role = "user",
            Content = request.Message,
        };
        dbContext.ChatMessages.Add(userMessage);

        // RAG: embed the question and search for relevant chunks
        IReadOnlyList<float> questionEmbedding = await llmProvider.GenerateEmbeddingAsync(request.Message, cancellationToken);

        IReadOnlyList<VectorSearchResult> searchResults = await vectorStore.SearchAsync(
            CollectionName,
            questionEmbedding,
            limit: 5,
            filterByUserId: user.Id,
            cancellationToken: cancellationToken);

        // Build context with sources
        var citations = new List<CitationDto>();
        string contextBlock = string.Empty;

        if (searchResults.Count > 0)
        {
            var contextParts = new List<string>();
            for (int i = 0; i < searchResults.Count; i++)
            {
                VectorSearchResult result = searchResults[i];
                contextParts.Add($"[Source {i + 1}] (from: {result.ContentTitle})\n{result.ChunkText}");
                citations.Add(new CitationDto(
                    result.ContentId,
                    result.ContentTitle ?? "Unknown",
                    result.ChunkText,
                    result.Score,
                    result.SourceUrl));
            }
            contextBlock = "## Relevant Context\n\n" + string.Join("\n\n---\n\n", contextParts);
        }

        // Build messages for LLM
        var llmMessages = new List<LlmMessage>();

        // Include recent conversation history (last 10 messages)
        IEnumerable<ChatMessage> recentMessages = thread.Messages.TakeLast(10);
        foreach (ChatMessage msg in recentMessages)
        {
            llmMessages.Add(new LlmMessage(msg.Role, msg.Content));
        }

        // Add current user message with context
        string userPrompt = string.IsNullOrEmpty(contextBlock)
            ? request.Message
            : $"{contextBlock}\n\n## User Question\n{request.Message}";
        llmMessages.Add(LlmMessage.User(userPrompt));

        try
        {
            LlmChatResponse llmResponse = await llmProvider.ChatAsync(new LlmChatRequest
            {
                SystemPrompt = SystemPrompt,
                Messages = llmMessages,
                Temperature = 0.4f,
                MaxTokens = 2048,
            }, cancellationToken);

            // Save assistant message
            var assistantMessage = new ChatMessage
            {
                ThreadId = thread.Id,
                Role = "assistant",
                Content = llmResponse.Content,
                Citations = citations.Count > 0 ? JsonSerializer.Serialize(citations) : null,
                TokensUsed = llmResponse.TotalTokens,
            };
            dbContext.ChatMessages.Add(assistantMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            var responseDto = new ChatResponseDto(
                new ChatMessageDto(
                    assistantMessage.Id,
                    assistantMessage.Role,
                    assistantMessage.Content,
                    assistantMessage.Citations,
                    assistantMessage.TokensUsed,
                    assistantMessage.CreatedAt),
                citations);

            return Result<ChatResponseDto>.Success(responseDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get LLM response for thread {ThreadId}", thread.Id);
            return Result<ChatResponseDto>.Failure("Failed to generate response. Please try again.");
        }
    }
}
