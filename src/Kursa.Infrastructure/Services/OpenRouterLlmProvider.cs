using System.ClientModel;
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
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(request.SystemPrompt)
        };

        foreach (LlmMessage msg in request.Messages)
        {
            messages.Add(msg.Role switch
            {
                "user" => new UserChatMessage(msg.Content),
                "assistant" => new AssistantChatMessage(msg.Content),
                _ => new UserChatMessage(msg.Content)
            });
        }

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = request.Temperature,
        };

        if (request.MaxTokens.HasValue)
        {
            chatOptions.MaxOutputTokenCount = request.MaxTokens.Value;
        }

        _logger.LogDebug("Sending OpenRouter chat request with {MessageCount} messages", messages.Count);

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

        return new LlmChatResponse
        {
            Content = completion.Content[0].Text,
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
        ReadOnlyMemory<float> vector = embedding.ToFloats();

        return vector.ToArray();
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embeddings via Ollama for {Count} texts", texts.Count);

        OpenAIEmbeddingCollection embeddings = await _embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

        return embeddings
            .Select(e => (IReadOnlyList<float>)e.ToFloats().ToArray())
            .ToList();
    }
}
