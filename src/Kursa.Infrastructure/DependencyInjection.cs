using Kursa.Application.Common.Interfaces;
using Kursa.Infrastructure.Options;
using Kursa.Infrastructure.Persistence;
using Kursa.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        // HTTP client for Moodle proxy
        services.AddHttpClient("Moodle");
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

        // Whisper transcription
        services.AddHttpClient("Whisper");
        services.AddScoped<ITranscriptionService, WhisperTranscriptionService>();
        services.AddSingleton<TranscriptionQueue>();
        services.AddSingleton<ITranscriptionQueue>(sp => sp.GetRequiredService<TranscriptionQueue>());
        services.AddHostedService<TranscriptionBackgroundService>();

        // LLM provider — configuration-driven selection
        services.AddSingleton<ILlmProvider>(sp =>
        {
            LlmOptions llmOptions = sp.GetRequiredService<IOptions<LlmOptions>>().Value;

            return llmOptions.Provider.ToLowerInvariant() switch
            {
                "openai" or "anthropic" => ActivatorUtilities.CreateInstance<OpenAiLlmProvider>(sp),
                "ollama" => ActivatorUtilities.CreateInstance<OllamaLlmProvider>(sp),
                _ => throw new InvalidOperationException(
                    $"Unknown LLM provider '{llmOptions.Provider}'. Supported: openai, anthropic, ollama.")
            };
        });

        return services;
    }
}
