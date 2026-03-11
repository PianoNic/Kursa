using System.Text.Json;
using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;

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
    IChatCompletionService chatCompletionService,
    ITextEmbeddingGenerationService embeddingService,
    IVectorStore vectorStore,
    IMoodleService moodleService,
    ILogger<SendChatMessageHandler> logger) : IRequestHandler<SendChatMessageCommand, Result<ChatResponseDto>>
{
    private const string SystemPrompt =
        """
        You are Kursa, an AI study assistant integrated with Moodle and indexed course materials.

        You have the following tools available:
        - search_course_materials: Searches the user's pinned and indexed course documents using semantic search. Use this when the user asks about specific course content, topics, learning objectives, or study material.
        - list_enrolled_courses: Lists all Moodle courses the user is enrolled in. Use this when the user asks what courses they have, wants to browse their courses, or needs a course ID.
        - get_course_content: Gets the full list of modules and resources in a specific Moodle course. Use this when the user asks about the structure or contents of a course.

        Decision rules:
        - For greetings, general knowledge, or questions that don't need course materials → answer directly without calling any tools.
        - For questions about course content, topics, or specific material → call search_course_materials first. If results are poor (low relevance), try with a more specific query.
        - For questions about "what courses do I have" or browsing courses → call list_enrolled_courses.
        - For questions about what's in a specific course → call get_course_content.
        - You may call tools multiple times with refined queries if the first result doesn't answer the question well.

        When answering based on tool results:
        - Cite sources using [Source N] format when referencing specific content.
        - Write in the same language as the user's question.
        - Be concise but thorough.
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

            thread = new ChatThread { UserId = user.Id, Title = title };
            dbContext.ChatThreads.Add(thread);
        }

        // Save user message
        dbContext.ChatMessages.Add(new ChatMessage
        {
            ThreadId = thread.Id,
            Role = "user",
            Content = request.Message,
        });

        // Build chat history from stored messages
        var chatHistory = new ChatHistory(SystemPrompt);
        foreach (ChatMessage msg in thread.Messages.TakeLast(10))
        {
            if (msg.Role == "user")
                chatHistory.AddUserMessage(msg.Content ?? string.Empty);
            else if (msg.Role == "assistant")
                chatHistory.AddAssistantMessage(msg.Content ?? string.Empty);
        }
        chatHistory.AddUserMessage(request.Message);

        // Create a per-request kernel with the plugin (plugin holds user context + citation accumulator)
        var allCitations = new List<CitationDto>();
        var plugin = new KursaAgentPlugin(vectorStore, embeddingService, moodleService, user, allCitations);
        var requestKernel = new Kernel();
        requestKernel.Plugins.AddFromObject(plugin);

        // SK automatically handles the full agentic loop (tool calls → results → final answer)
#pragma warning disable SKEXP0001
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.3f,
            MaxTokens = 2048,
        };
#pragma warning restore SKEXP0001

        logger.LogInformation("Running SK agentic chat for user {UserId}", user.Id);

        ChatMessageContent result = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory, executionSettings, requestKernel, cancellationToken);

        string finalAnswer = result.Content ?? "I wasn't able to find a complete answer. Please try rephrasing your question.";

        // Save assistant message
        var assistantMessage = new ChatMessage
        {
            ThreadId = thread.Id,
            Role = "assistant",
            Content = finalAnswer,
            Citations = allCitations.Count > 0 ? JsonSerializer.Serialize(allCitations) : null,
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
            allCitations);

        return Result<ChatResponseDto>.Success(responseDto);
    }
}
