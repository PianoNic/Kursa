using Kursa.Infrastructure.MoodlewareAPI.Client;
using Kursa.Infrastructure.Options;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kursa.Infrastructure.MoodlewareAPI;

/// <summary>
/// Creates <see cref="MoodlewareApiClient"/> instances pre-configured with a specific Moodle user token.
/// The token is injected via a custom <see cref="IAuthenticationProvider"/> so callers
/// don't need to manage auth headers manually.
/// </summary>
public sealed class MoodlewareClientFactory(
    IHttpClientFactory httpClientFactory,
    IOptions<MoodleOptions> options)
{
    public MoodlewareApiClient CreateForToken(string moodleToken)
    {
        var httpClient = httpClientFactory.CreateClient("Moodle");
        var authProvider = new StaticBearerTokenAuthProvider(moodleToken);
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient)
        {
            BaseUrl = options.Value.BridgeUrl.TrimEnd('/')
        };
        return new MoodlewareApiClient(adapter);
    }

    /// <summary>Creates an unauthenticated client for calling open endpoints such as POST /get-token.</summary>
    public MoodlewareApiClient CreateAnonymous()
    {
        var httpClient = httpClientFactory.CreateClient("Moodle");
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
        {
            BaseUrl = options.Value.BridgeUrl.TrimEnd('/')
        };
        return new MoodlewareApiClient(adapter);
    }

    private sealed class StaticBearerTokenAuthProvider(string token) : IAuthenticationProvider
    {
        public Task AuthenticateRequestAsync(
            RequestInformation request,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            request.Headers.TryAdd("Authorization", $"Bearer {token}");
            return Task.CompletedTask;
        }
    }
}
