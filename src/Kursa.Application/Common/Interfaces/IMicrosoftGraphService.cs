using Kursa.Application.Features.Graph.Models;

namespace Kursa.Application.Common.Interfaces;

public interface IMicrosoftGraphService
{
    Task<IReadOnlyList<OneNoteNotebookDto>> GetNotebooksAsync(
        string accessToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OneNoteSectionDto>> GetSectionsAsync(
        string accessToken, string notebookId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OneNotePageDto>> GetPagesAsync(
        string accessToken, string sectionId, CancellationToken cancellationToken = default);

    Task<string> GetPageContentAsync(
        string accessToken, string pageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SharePointSiteDto>> GetSitesAsync(
        string accessToken, string? searchQuery = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SharePointDriveItemDto>> GetDriveItemsAsync(
        string accessToken, string siteId, string? folderId = null,
        CancellationToken cancellationToken = default);
}
