using System.ClientModel;
using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Kursa.Infrastructure.Services;

/// <summary>
/// LLM provider that routes chat completions through OpenRouter (OpenAI-compatible API).
/// Embeddings are handled via Ollama since OpenRouter does not natively support them.
/// </summary>
public sealed class OpenRouterLlmProvider : ILlmProvider
{
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<OpenRouterLlmProvider> _logger;

    public OpenRouterLlmProvider(IOptions<LlmOptions> options, ILogger<OpenRouterLlmProvider> logger)
    {
        _logger = logger;
        LlmOptions llmOptions = options.Value;

        // OpenRouter exposes an OpenAI-compatible chat API
        var openRouterOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(llmOptions.OpenRouterBaseUrl)
        };
        var openRouterCredential = new ApiKeyCredential(llmOptions.ApiKey);
        var openRouterClient = new OpenAIClient(openRouterCredential, openRouterOptions);
        _chatClient = openRouterClient.GetChatClient(llmOptions.Model);

        // OpenRouter does not support embeddings — use Ollama for embeddings
        var ollamaOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(llmOptions.OllamaHost)
        };
        var ollamaCredential = new ApiKeyCredential("ollama");
        var ollamaClient = new OpenAIClient(ollamaCredential, ollamaOptions);
        _embeddingClient = ollamaClient.GetEmbeddingClient(llmOptions.EmbeddingModel);
    }

    public async Task<LlmChatResponse> ChatAsync(
        LlmChatRequest request,
        CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = BuildMessages(request.SystemPrompt, request.Messages);

        var chatOptions = new ChatCompletionOptions { Temperature = request.Temperature };
        if (request.MaxTokens.HasValue)
            chatOptions.MaxOutputTokenCount = request.MaxTokens.Value;

        _logger.LogDebug("Sending OpenRouter chat request with {MessageCount} messages", messages.Count);

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

        return new LlmChatResponse
        {
            Content = completion.Content[0].Text,
            PromptTokens = completion.Usage?.InputTokenCount ?? 0,
            CompletionTokens = completion.Usage?.OutputTokenCount ?? 0,
        };
    }

    public async Task<LlmChatResponse> ChatWithToolsAsync(
        LlmChatRequestWithTools request,
        CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = BuildMessages(request.SystemPrompt, request.Messages);

        var chatOptions = new ChatCompletionOptions { Temperature = request.Temperature };
        if (request.MaxTokens.HasValue)
            chatOptions.MaxOutputTokenCount = request.MaxTokens.Value;

        // Map LlmTool → OpenAI ChatTool
        foreach (LlmTool tool in request.Tools)
        {
            chatOptions.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: tool.Name,
                functionDescription: tool.Description,
                functionParameters: BinaryData.FromString(tool.InputSchema.GetRawText())));
        }

        _logger.LogDebug("Sending OpenRouter tool-calling request with {MessageCount} messages, {ToolCount} tools",
            messages.Count, request.Tools.Count);

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

        // Tool call response
        if (completion.FinishReason == ChatFinishReason.ToolCalls)
        {
            var toolCalls = completion.ToolCalls
                .Select(tc => new LlmToolCall(tc.Id, tc.FunctionName, tc.FunctionArguments.ToString()))
                .ToList();

            return new LlmChatResponse
            {
                Content = null,
                ToolCalls = toolCalls,
                PromptTokens = completion.Usage?.InputTokenCount ?? 0,
                CompletionTokens = completion.Usage?.OutputTokenCount ?? 0,
            };
        }

        // Final text answer
        return new LlmChatResponse
        {
            Content = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty,
            ToolCalls = null,
            PromptTokens = completion.Usage?.InputTokenCount ?? 0,
            CompletionTokens = completion.Usage?.OutputTokenCount ?? 0,
        };
    }

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding via Ollama for text of length {Length}", text.Length);
        OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return embedding.ToFloats().ToArray();
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embeddings via Ollama for {Count} texts", texts.Count);
        OpenAIEmbeddingCollection embeddings = await _embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);
        return embeddings.Select(e => (IReadOnlyList<float>)e.ToFloats().ToArray()).ToList();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static List<ChatMessage> BuildMessages(string systemPrompt, IReadOnlyList<LlmMessage> messages)
    {
        var result = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };

        foreach (LlmMessage msg in messages)
        {
            ChatMessage chatMessage = msg.Role switch
            {
                "user" => new UserChatMessage(msg.Content ?? string.Empty),

                "assistant" when msg.HasToolCalls =>
                    new AssistantChatMessage(
                        msg.ToolCalls!.Select(tc =>
                            ChatToolCall.CreateFunctionToolCall(tc.Id, tc.Name,
                                BinaryData.FromString(tc.ArgumentsJson)))),

                "assistant" => new AssistantChatMessage(msg.Content ?? string.Empty),

                "tool" => new ToolChatMessage(msg.ToolCallId!, msg.Content ?? string.Empty),

                _ => new UserChatMessage(msg.Content ?? string.Empty)
            };

            result.Add(chatMessage);
        }

        return result;
    }
}
