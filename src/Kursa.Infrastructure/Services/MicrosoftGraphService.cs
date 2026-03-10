using System.Net.Http.Headers;
using System.Text.Json;
using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Graph.Models;
using Microsoft.Extensions.Logging;

namespace Kursa.Infrastructure.Services;

public sealed class MicrosoftGraphService(
    IHttpClientFactory httpClientFactory,
    ILogger<MicrosoftGraphService> logger) : IMicrosoftGraphService
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<OneNoteNotebookDto>> GetNotebooksAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        string json = await SendGraphRequestAsync(accessToken, "/me/onenote/notebooks", cancellationToken);
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<OneNoteNotebookDto>>(json, JsonOptions);
        return response?.Value ?? [];
    }

    public async Task<IReadOnlyList<OneNoteSectionDto>> GetSectionsAsync(
        string accessToken, string notebookId, CancellationToken cancellationToken = default)
    {
        string json = await SendGraphRequestAsync(
            accessToken, $"/me/onenote/notebooks/{notebookId}/sections", cancellationToken);
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<OneNoteSectionDto>>(json, JsonOptions);
        return response?.Value ?? [];
    }

    public async Task<IReadOnlyList<OneNotePageDto>> GetPagesAsync(
        string accessToken, string sectionId, CancellationToken cancellationToken = default)
    {
        string json = await SendGraphRequestAsync(
            accessToken, $"/me/onenote/sections/{sectionId}/pages", cancellationToken);
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<OneNotePageDto>>(json, JsonOptions);
        return response?.Value ?? [];
    }

    public async Task<string> GetPageContentAsync(
        string accessToken, string pageId, CancellationToken cancellationToken = default)
    {
        return await SendGraphRequestAsync(
            accessToken, $"/me/onenote/pages/{pageId}/content", cancellationToken);
    }

    public async Task<IReadOnlyList<SharePointSiteDto>> GetSitesAsync(
        string accessToken, string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        string endpoint = string.IsNullOrWhiteSpace(searchQuery)
            ? "/sites?search=*"
            : $"/sites?search={Uri.EscapeDataString(searchQuery)}";

        string json = await SendGraphRequestAsync(accessToken, endpoint, cancellationToken);
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<SharePointSiteDto>>(json, JsonOptions);
        return response?.Value ?? [];
    }

    public async Task<IReadOnlyList<SharePointDriveItemDto>> GetDriveItemsAsync(
        string accessToken, string siteId, string? folderId = null,
        CancellationToken cancellationToken = default)
    {
        string endpoint = string.IsNullOrEmpty(folderId)
            ? $"/sites/{siteId}/drive/root/children"
            : $"/sites/{siteId}/drive/items/{folderId}/children";

        string json = await SendGraphRequestAsync(accessToken, endpoint, cancellationToken);
        var response = JsonSerializer.Deserialize<GraphCollectionResponse<GraphDriveItemRaw>>(json, JsonOptions);

        return response?.Value.Select(item => new SharePointDriveItemDto
        {
            Id = item.Id,
            Name = item.Name,
            WebUrl = item.WebUrl,
            Size = item.Size,
            LastModifiedAt = item.LastModifiedDateTime,
            IsFolder = item.Folder is not null,
            MimeType = item.File?.MimeType,
        }).ToList() ?? [];
    }

    private async Task<string> SendGraphRequestAsync(
        string accessToken, string endpoint, CancellationToken cancellationToken)
    {
        using HttpClient client = httpClientFactory.CreateClient("MicrosoftGraph");
        client.BaseAddress = new Uri(GraphBaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        logger.LogDebug("Calling Microsoft Graph: {Endpoint}", endpoint);

        using HttpResponseMessage response = await client.GetAsync(endpoint, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Graph API error {StatusCode} for {Endpoint}: {Body}",
                (int)response.StatusCode, endpoint, body);
            throw new HttpRequestException(
                $"Microsoft Graph API returned {(int)response.StatusCode}: {response.ReasonPhrase}");
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
