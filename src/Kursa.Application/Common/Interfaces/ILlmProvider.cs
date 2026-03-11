using System.Text.Json;

namespace Kursa.Application.Common.Interfaces;

public interface ILlmProvider
{
    Task<LlmChatResponse> ChatAsync(
        LlmChatRequest request,
        CancellationToken cancellationToken = default);

    Task<LlmChatResponse> ChatWithToolsAsync(
        LlmChatRequestWithTools request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}

// ── Basic chat ──────────────────────────────────────────────────────────────

public sealed record LlmChatRequest
{
    public required string SystemPrompt { get; init; }
    public required IReadOnlyList<LlmMessage> Messages { get; init; }
    public float Temperature { get; init; } = 0.7f;
    public int? MaxTokens { get; init; }
}

// ── Tool-calling chat ───────────────────────────────────────────────────────

public sealed record LlmChatRequestWithTools
{
    public required string SystemPrompt { get; init; }
    public required IReadOnlyList<LlmMessage> Messages { get; init; }
    public required IReadOnlyList<LlmTool> Tools { get; init; }
    public float Temperature { get; init; } = 0.3f;
    public int? MaxTokens { get; init; }
}

/// <summary>A tool the agent can call, described by a JSON Schema for its input.</summary>
public sealed record LlmTool(string Name, string Description, JsonElement InputSchema);

/// <summary>A tool call requested by the LLM in a response.</summary>
public sealed record LlmToolCall(string Id, string Name, string ArgumentsJson);

// ── Messages (supports plain, tool-call request, and tool-result roles) ────

public sealed record LlmMessage
{
    public string Role { get; init; } = string.Empty;
    public string? Content { get; init; }

    /// <summary>Set when the assistant is requesting tool calls (role = "assistant").</summary>
    public IReadOnlyList<LlmToolCall>? ToolCalls { get; init; }
    public bool HasToolCalls => ToolCalls?.Count > 0;

    /// <summary>Set for role = "tool" to link the result back to the tool call.</summary>
    public string? ToolCallId { get; init; }

    // Convenience constructors kept for backwards compat
    public LlmMessage() { }
    public LlmMessage(string role, string content) { Role = role; Content = content; }

    public static LlmMessage User(string content) => new() { Role = "user", Content = content };
    public static LlmMessage Assistant(string content) => new() { Role = "assistant", Content = content };

    public static LlmMessage AssistantWithToolCalls(IReadOnlyList<LlmToolCall> toolCalls) =>
        new() { Role = "assistant", Content = null, ToolCalls = toolCalls };

    public static LlmMessage ToolResult(string toolCallId, string content) =>
        new() { Role = "tool", Content = content, ToolCallId = toolCallId };
}

// ── Response ────────────────────────────────────────────────────────────────

public sealed record LlmChatResponse
{
    /// <summary>The final text answer. Null if the response only contains tool calls.</summary>
    public string? Content { get; init; }

    /// <summary>Tool calls requested by the LLM. Null or empty when the LLM returned a final answer.</summary>
    public IReadOnlyList<LlmToolCall>? ToolCalls { get; init; }

    public bool HasToolCalls => ToolCalls?.Count > 0;

    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}
