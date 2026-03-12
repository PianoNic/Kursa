using Kursa.Application.Common.Interfaces;
using Kursa.Application.Features.Graph.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kursa.API.Controllers;

/// <summary>
/// Microsoft Graph integration for OneNote and SharePoint.
/// Expects a Microsoft Graph access token in the X-Graph-Token header.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GraphController(IMicrosoftGraphService graphService) : ControllerBase
{
    // -- OneNote --

    [HttpGet("onenote/notebooks")]
    [ProducesResponseType(typeof(IReadOnlyList<OneNoteNotebookDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotebooksAsync(CancellationToken cancellationToken)
    {
        string? token = GetGraphToken();
        if (token is null)
            return BadRequest("Microsoft Graph token is required. Set the X-Graph-Token header.");

        IReadOnlyList<OneNoteNotebookDto> notebooks = await graphService.GetNotebooksAsync(token, cancellationToken);
        return Ok(notebooks);
    }

    [HttpGet("onenote/notebooks/{notebookId}/sections")]
    [ProducesResponseType(typeof(IReadOnlyList<OneNoteSectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSectionsAsync(string notebookId, CancellationToken cancellationToken)
    {
        string? token = GetGraphToken();
        if (token is null)
            return BadRequest("Microsoft Graph token is required. Set the X-Graph-Token header.");

        IReadOnlyList<OneNoteSectionDto> sections = await graphService.GetSectionsAsync(token, notebookId, cancellationToken);
        return Ok(sections);
    }

    [HttpGet("onenote/sections/{sectionId}/pages")]
    [ProducesResponseType(typeof(IReadOnlyList<OneNotePageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagesAsync(string sectionId, CancellationToken cancellationToken)
    {
        string? token = GetGraphToken();
        if (token is null)
            return BadRequest("Microsoft Graph token is required. Set the X-Graph-Token header.");

        IReadOnlyList<OneNotePageDto> pages = await graphService.GetPagesAsync(token, sectionId, cancellationToken);
        return Ok(pages);
    }

    [HttpGet("onenote/pages/{pageId}/content")]
    public async Task<IActionResult> GetPageContentAsync(string pageId, CancellationToken cancellationToken)
    {
        string? token = GetGraphToken();
        if (token is null)
            return BadRequest("Microsoft Graph token is required. Set the X-Graph-Token header.");

        string content = await graphService.GetPageContentAsync(token, pageId, cancellationToken);
        return Content(content, "text/html");
    }

    // -- SharePoint --

    [HttpGet("sharepoint/sites")]
    [ProducesResponseType(typeof(IReadOnlyList<SharePointSiteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSitesAsync(
        [FromQuery] string? search, CancellationToken cancellationToken)
    {
        string? token = GetGraphToken();
        if (token is null)
            return BadRequest("Microsoft Graph token is required. Set the X-Graph-Token header.");

        IReadOnlyList<SharePointSiteDto> sites = await graphService.GetSitesAsync(token, search, cancellationToken);
        return Ok(sites);
    }

    [HttpGet("sharepoint/sites/{siteId}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<SharePointDriveItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDriveItemsAsync(
        string siteId, [FromQuery] string? folderId, CancellationToken cancellationToken)
    {
        string? token = GetGraphToken();
        if (token is null)
            return BadRequest("Microsoft Graph token is required. Set the X-Graph-Token header.");

        IReadOnlyList<SharePointDriveItemDto> items = await graphService.GetDriveItemsAsync(
            token, siteId, folderId, cancellationToken);
        return Ok(items);
    }

    private string? GetGraphToken()
    {
        return Request.Headers["X-Graph-Token"].FirstOrDefault();
    }
}
