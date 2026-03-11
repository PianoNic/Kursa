using System.Text.Json;
using FluentValidation;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Common.Models;
using Kursa.Application.Features.Moodle.Models;
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
    IMoodleService moodleService,
    ILogger<SendChatMessageHandler> logger) : IRequestHandler<SendChatMessageCommand, Result<ChatResponseDto>>
{
    private const string CollectionName = "content_chunks";
    private const int MaxAgentIterations = 5;

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

    // ── Tool schemas ───────────────────────────────────────────────────────

    private static readonly IReadOnlyList<LlmTool> AgentTools =
    [
        new LlmTool(
            "search_course_materials",
            "Semantically searches the user's pinned and indexed course documents. Returns the most relevant chunks of text with source titles and relevance scores.",
            JsonDocument.Parse("""
                {
                  "type": "object",
                  "properties": {
                    "query": {
                      "type": "string",
                      "description": "The search query optimized for semantic retrieval, e.g. 'learning objectives for marketing' or 'Break-even point formula'"
                    },
                    "limit": {
                      "type": "integer",
                      "description": "Maximum number of results to return (1-10). Default 5.",
                      "default": 5
                    }
                  },
                  "required": ["query"]
                }
                """).RootElement),

        new LlmTool(
            "list_enrolled_courses",
            "Returns a list of all Moodle courses the user is enrolled in, including course IDs, names, and progress.",
            JsonDocument.Parse("""
                {
                  "type": "object",
                  "properties": {}
                }
                """).RootElement),

        new LlmTool(
            "get_course_content",
            "Returns the sections and modules of a specific Moodle course by its numeric ID. Includes module names, types, and descriptions.",
            JsonDocument.Parse("""
                {
                  "type": "object",
                  "properties": {
                    "courseId": {
                      "type": "integer",
                      "description": "The numeric Moodle course ID (obtain from list_enrolled_courses if unknown)"
                    }
                  },
                  "required": ["courseId"]
                }
                """).RootElement),
    ];

    // ── Handler ────────────────────────────────────────────────────────────

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
        var userMessage = new ChatMessage
        {
            ThreadId = thread.Id,
            Role = "user",
            Content = request.Message,
        };
        dbContext.ChatMessages.Add(userMessage);

        // Build message history for the agent
        var messages = new List<LlmMessage>();
        foreach (ChatMessage msg in thread.Messages.TakeLast(10))
        {
            messages.Add(new LlmMessage(msg.Role, msg.Content));
        }
        messages.Add(LlmMessage.User(request.Message));

        // Run the agentic loop
        var allCitations = new List<CitationDto>();
        string? finalAnswer = null;

        for (int iteration = 0; iteration < MaxAgentIterations; iteration++)
        {
            LlmChatResponse response = await llmProvider.ChatWithToolsAsync(new LlmChatRequestWithTools
            {
                SystemPrompt = SystemPrompt,
                Messages = messages,
                Tools = AgentTools,
                Temperature = 0.3f,
                MaxTokens = 2048,
            }, cancellationToken);

            // Final answer — no more tool calls
            if (!response.HasToolCalls)
            {
                finalAnswer = response.Content ?? string.Empty;
                break;
            }

            // Execute tool calls, add results to message history
            messages.Add(LlmMessage.AssistantWithToolCalls(response.ToolCalls!));

            foreach (LlmToolCall toolCall in response.ToolCalls!)
            {
                logger.LogInformation("Agent calling tool {Tool} (iteration {Iteration}): {Args}",
                    toolCall.Name, iteration + 1, toolCall.ArgumentsJson);

                (string toolResultContent, List<CitationDto> citations) =
                    await ExecuteToolAsync(toolCall, user, cancellationToken);

                allCitations.AddRange(citations);
                messages.Add(LlmMessage.ToolResult(toolCall.Id, toolResultContent));
            }
        }

        // Fallback if max iterations hit without a final answer
        finalAnswer ??= "I wasn't able to find a complete answer. Please try rephrasing your question.";

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

    // ── Tool execution ─────────────────────────────────────────────────────

    private async Task<(string Content, List<CitationDto> Citations)> ExecuteToolAsync(
        LlmToolCall toolCall,
        Kursa.Domain.Entities.User user,
        CancellationToken cancellationToken)
    {
        try
        {
            return toolCall.Name switch
            {
                "search_course_materials" => await SearchCourseMaterialsAsync(toolCall.ArgumentsJson, user.Id, cancellationToken),
                "list_enrolled_courses" => await ListEnrolledCoursesAsync(user, cancellationToken),
                "get_course_content" => await GetCourseContentAsync(toolCall.ArgumentsJson, user, cancellationToken),
                _ => ($"Unknown tool: {toolCall.Name}", [])
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tool {Tool} failed", toolCall.Name);
            return ($"Tool {toolCall.Name} failed: {ex.Message}", []);
        }
    }

    private async Task<(string Content, List<CitationDto> Citations)> SearchCourseMaterialsAsync(
        string argsJson, Guid userId, CancellationToken cancellationToken)
    {
        using JsonDocument args = JsonDocument.Parse(argsJson);
        string query = args.RootElement.GetProperty("query").GetString() ?? string.Empty;
        int limit = args.RootElement.TryGetProperty("limit", out JsonElement limitEl)
            ? Math.Clamp(limitEl.GetInt32(), 1, 10)
            : 5;

        IReadOnlyList<float> embedding = await llmProvider.GenerateEmbeddingAsync(query, cancellationToken);
        IReadOnlyList<VectorSearchResult> results = await vectorStore.SearchAsync(
            CollectionName, embedding, limit: limit, filterByUserId: userId, cancellationToken: cancellationToken);

        if (results.Count == 0)
            return ("No relevant course materials found for this query.", []);

        var citations = new List<CitationDto>();
        var parts = new List<string>();
        for (int i = 0; i < results.Count; i++)
        {
            VectorSearchResult r = results[i];
            parts.Add($"[Source {i + 1}] {r.ContentTitle} (relevance: {r.Score:F2})\n{r.ChunkText}");
            citations.Add(new CitationDto(r.ContentId, r.ContentTitle ?? "Unknown", r.ChunkText, r.Score, r.SourceUrl));
        }

        return (string.Join("\n\n---\n\n", parts), citations);
    }

    private async Task<(string Content, List<CitationDto> Citations)> ListEnrolledCoursesAsync(
        Kursa.Domain.Entities.User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.MoodleToken))
            return ("User has not linked their Moodle account yet.", []);

        IReadOnlyList<MoodleCourseDto> courses = await moodleService.GetEnrolledCoursesAsync(
            user.MoodleToken, cancellationToken);

        if (courses.Count == 0)
            return ("No enrolled courses found.", []);

        var lines = courses.Select(c =>
            $"- [{c.Id}] {c.FullName} ({c.ShortName})" +
            (c.Progress.HasValue ? $" — {c.Progress:F0}% complete" : string.Empty));

        return ($"Enrolled courses ({courses.Count} total):\n{string.Join('\n', lines)}", []);
    }

    private async Task<(string Content, List<CitationDto> Citations)> GetCourseContentAsync(
        string argsJson, Kursa.Domain.Entities.User user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(user.MoodleToken))
            return ("User has not linked their Moodle account yet.", []);

        using JsonDocument args = JsonDocument.Parse(argsJson);
        int courseId = args.RootElement.GetProperty("courseId").GetInt32();

        IReadOnlyList<MoodleCourseSectionDto> sections = await moodleService.GetCourseContentAsync(
            user.MoodleToken, courseId, cancellationToken);

        if (sections.Count == 0)
            return ("No content found for this course.", []);

        var lines = new List<string>();
        foreach (MoodleCourseSectionDto section in sections.Where(s => s.Visible != 0 && s.Modules.Count > 0))
        {
            lines.Add($"## {section.Name}");
            foreach (MoodleModuleDto mod in section.Modules.Where(m => m.Visible != 0))
            {
                string fileInfo = mod.Contents?.Count > 0 ? $" ({mod.Contents.Count} file(s))" : string.Empty;
                lines.Add($"  - [{mod.ModName}] {mod.Name}{fileInfo}");
                if (!string.IsNullOrWhiteSpace(mod.Description))
                {
                    // Strip HTML tags for a readable plain-text summary
                    string desc = System.Text.RegularExpressions.Regex.Replace(mod.Description, "<[^>]+>", " ")
                        .Replace("&nbsp;", " ").Trim();
                    if (desc.Length > 200) desc = string.Concat(desc.AsSpan(0, 197), "...");
                    if (!string.IsNullOrWhiteSpace(desc))
                        lines.Add($"    {desc}");
                }
            }
        }

        return (string.Join('\n', lines), []);
    }
}
