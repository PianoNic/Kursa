using System.ClientModel;
using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Kursa.Infrastructure.Services;

public sealed class OllamaLlmProvider : ILlmProvider
{
    private readonly ChatClient _chatClient;
    private readonly EmbeddingClient _embeddingClient;
    private readonly ILogger<OllamaLlmProvider> _logger;

    public OllamaLlmProvider(IOptions<LlmOptions> options, ILogger<OllamaLlmProvider> logger)
    {
        _logger = logger;
        LlmOptions llmOptions = options.Value;

        // Ollama exposes an OpenAI-compatible API
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(llmOptions.OllamaHost)
        };

        // Ollama doesn't require a real API key
        var credential = new ApiKeyCredential("ollama");
        var openAiClient = new OpenAIClient(credential, clientOptions);

        _chatClient = openAiClient.GetChatClient(llmOptions.Model);
        _embeddingClient = openAiClient.GetEmbeddingClient(llmOptions.EmbeddingModel);
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

        _logger.LogDebug("Sending Ollama chat request with {MessageCount} messages", messages.Count);

        ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

        return new LlmChatResponse
        {
            Content = completion.Content[0].Text,
            PromptTokens = completion.Usage?.InputTokenCount ?? 0,
            CompletionTokens = completion.Usage?.OutputTokenCount ?? 0,
        };
    }

    public Task<LlmChatResponse> ChatWithToolsAsync(
        LlmChatRequestWithTools request,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Ollama provider does not support tool calling. Use OpenRouter provider.");

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating Ollama embedding for text of length {Length}", text.Length);

        OpenAIEmbedding embedding = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        ReadOnlyMemory<float> vector = embedding.ToFloats();

        return vector.ToArray();
    }

    public async Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating Ollama embeddings for {Count} texts", texts.Count);

        OpenAIEmbeddingCollection embeddings = await _embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

        return embeddings
            .Select(e => (IReadOnlyList<float>)e.ToFloats().ToArray())
            .ToList();
    }
}
