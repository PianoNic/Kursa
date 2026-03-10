using Kursa.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kursa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
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

        return services;
    }
}
