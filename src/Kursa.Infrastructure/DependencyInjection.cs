using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.MoodlewareAPI;
using Kursa.Infrastructure.Options;
using Kursa.Infrastructure.Persistence;
using Kursa.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Kursa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IUserSyncService, UserSyncService>();

        // Redis distributed cache
        string redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "Kursa:";
        });

        // Moodle bridge — Kiota-generated client via MoodlewareAPI
        services.AddHttpClient("Moodle");
        // Direct Moodle file downloads (pluginfile.php proxy)
        services.AddHttpClient("MoodleFile");
        services.AddSingleton<MoodlewareClientFactory>();
        services.AddScoped<IMoodleService, MoodleService>();

        services.AddOptions<QdrantOptions>()
            .Bind(configuration.GetSection(QdrantOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MinIOOptions>()
            .Bind(configuration.GetSection(MinIOOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MoodleOptions>()
            .Bind(configuration.GetSection(MoodleOptions.SectionName));

        services.AddOptions<LlmOptions>()
            .Bind(configuration.GetSection(LlmOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<MicrosoftGraphOptions>()
            .Bind(configuration.GetSection(MicrosoftGraphOptions.SectionName));

        services.AddOptions<AudioProcessingOptions>()
            .Bind(configuration.GetSection(AudioProcessingOptions.SectionName));

        services.AddOptions<OidcOptions>()
            .Bind(configuration.GetSection(OidcOptions.SectionName));

        // Qdrant vector store
        services.AddSingleton<IVectorStore, QdrantVectorStore>();

        // MinIO file storage
        services.AddSingleton<IFileStorageService, MinioFileStorageService>();

        // Content embedding pipeline
        services.AddScoped<IContentPipeline, ContentPipeline>();

        // Summary service
        services.AddScoped<ISummaryService, SummaryService>();

        // Recording indexing (transcript → embeddings + summary)
        services.AddScoped<IRecordingIndexingService, RecordingIndexingService>();

        // AI study suggestions (agentic behavior)
        services.AddScoped<IStudySuggestionService, StudySuggestionService>();

        // Microsoft Graph (OneNote + SharePoint)
        services.AddHttpClient("MicrosoftGraph");
        services.AddScoped<IMicrosoftGraphService, MicrosoftGraphService>();

        // Whisper transcription
        services.AddHttpClient("Whisper");
        services.AddScoped<ITranscriptionService, WhisperTranscriptionService>();
        services.AddSingleton<TranscriptionQueue>();
        services.AddSingleton<ITranscriptionQueue>(sp => sp.GetRequiredService<TranscriptionQueue>());
        services.AddHostedService<TranscriptionBackgroundService>();

        // Semantic Kernel — chat via OpenRouter, embeddings via Ollama
        LlmOptions llmOpts = configuration.GetSection(LlmOptions.SectionName).Get<LlmOptions>()
            ?? new LlmOptions { Provider = "openrouter" };

        IKernelBuilder kernelBuilder = services.AddKernel();

#pragma warning disable SKEXP0010, SKEXP0070
        // Chat completion (OpenRouter = OpenAI-compatible API)
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: llmOpts.Model,
            apiKey: llmOpts.ApiKey,
            endpoint: new Uri(llmOpts.OpenRouterBaseUrl));

        // Text embeddings (Ollama native API)
        kernelBuilder.AddOllamaTextEmbeddingGeneration(
            modelId: llmOpts.EmbeddingModel,
            endpoint: new Uri(llmOpts.OllamaHost));
#pragma warning restore SKEXP0010, SKEXP0070

        return services;
    }
}
